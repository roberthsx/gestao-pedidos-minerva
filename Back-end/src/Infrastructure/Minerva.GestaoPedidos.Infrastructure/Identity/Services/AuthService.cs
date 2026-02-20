using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Minerva.GestaoPedidos.Application.Common;
using Minerva.GestaoPedidos.Application.Common.Exceptions;
using Minerva.GestaoPedidos.Application.Contracts;
using Minerva.GestaoPedidos.Application.DTOs;
using Minerva.GestaoPedidos.Infrastructure.Identity.Configurations;
using System.Data.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Minerva.GestaoPedidos.Infrastructure.Identity.Services;

/// <summary>
/// Autenticação com token em cache (IMemoryCache): retorna token do cache se ainda faltar mais de 5 min para vencer;
/// se estiver a 5 min ou menos de vencer (ou já vencido), gera novo JWT e armazena no cache.
/// </summary>
public sealed class AuthService : IAuthService
{
    private const string CacheKeyPrefix = "auth_token_";
    /// <summary>Se o token em cache vencer em menos que isso, geramos um novo em vez de reutilizar.</summary>
    private static readonly TimeSpan MinTimeToReuseCachedToken = TimeSpan.FromMinutes(5);
    private readonly IAuthUserStore _userStore;
    private readonly IMemoryCache _cache;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IAuthUserStore userStore,
        IMemoryCache cache,
        IOptions<JwtSettings> jwtSettings)
    {
        _userStore = userStore;
        _cache = cache;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<LoginResultDto>> LoginAsync(
        string registrationNumber,
        string password,
        CancellationToken cancellationToken = default)
    {
        AuthUserInfo? usuario;
        try
        {
            usuario = await _userStore.GetByRegistrationNumberAsync(registrationNumber.Trim(), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is DbException or Npgsql.NpgsqlException or TimeoutException or OperationCanceledException)
        {
            if (ex is OperationCanceledException)
                throw;
            throw new InfrastructureException(InfrastructureException.DefaultMessage, ex);
        }

        if (usuario is null)
            return Result<LoginResultDto>.Failure("Matrícula ou senha inválidos.");

        if (!BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
            return Result<LoginResultDto>.Failure("Matrícula ou senha inválidos.");

        var cacheKey = CacheKeyPrefix + registrationNumber.Trim();
        if (_cache.TryGetValue(cacheKey, out LoginResultDto? cached) && cached is not null && CachedTokenHasEnoughTimeLeft(cached.AccessToken))
            return Result<LoginResultDto>.Success(cached);

        var expirationMinutes = _jwtSettings.ExpirationMinutes > 0 ? _jwtSettings.ExpirationMinutes : 60;
        var (tokenString, expiresInSeconds) = GenerateToken(usuario, expirationMinutes);

        var dto = new LoginResultDto(
            tokenString,
            expiresInSeconds,
            new LoginUserDto(usuario.Name, usuario.Role));

        _cache.Set(cacheKey, dto, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expirationMinutes)
        });

        return Result<LoginResultDto>.Success(dto);
    }

    /// <summary>Retorna true se o token JWT em cache expira em mais de 5 minutos (pode reutilizar).</summary>
    private static bool CachedTokenHasEnoughTimeLeft(string tokenString)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);
            var expClaim = token.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
            if (string.IsNullOrEmpty(expClaim) || !long.TryParse(expClaim, out var expUnix))
                return false;
            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
            return DateTime.UtcNow.Add(MinTimeToReuseCachedToken) < expiresAt;
        }
        catch
        {
            return false;
        }
    }

    private (string Token, int ExpiresInSeconds) GenerateToken(AuthUserInfo usuario, int expirationMinutes)
    {
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? _jwtSettings.Secret;
        if (string.IsNullOrWhiteSpace(secret))
            secret = "super-secret-key-change-in-prod";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.RegistrationNumber),
            new(ClaimTypes.Name, usuario.Name),
            new(ClaimTypes.Role, usuario.Role)
        };

        var issuer = _jwtSettings.Issuer ?? "Minerva.GestaoPedidos";
        var audience = _jwtSettings.Audience ?? "Minerva.Frontend";
        var expires = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expires,
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        var expiresInSeconds = (int)TimeSpan.FromMinutes(expirationMinutes).TotalSeconds;
        return (tokenString, expiresInSeconds);
    }
}