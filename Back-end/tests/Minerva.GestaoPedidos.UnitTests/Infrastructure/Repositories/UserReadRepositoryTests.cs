using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Minerva.GestaoPedidos.Application.Common.Mappings;
using Minerva.GestaoPedidos.Domain.Entities;
using Minerva.GestaoPedidos.Infrastructure.Data;
using Minerva.GestaoPedidos.Infrastructure.Repositories;
using Xunit;

namespace Minerva.GestaoPedidos.UnitTests.Infrastructure.Repositories;

/// <summary>
/// Testes do UserReadRepository com banco em memória: mapeamento via AutoMapper, GetByIdAsync e GetAllAsync.
/// </summary>
public class UserReadRepositoryTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        return config.CreateMapper();
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("UserReadRepo_" + Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserNotExists_ReturnsNull()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var sut = new UserReadRepository(ctx, CreateMapper());

        var result = await sut.GetByIdAsync(999, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_MapToReadModel_ReturnsCorrectProperties()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var profile = new Minerva.GestaoPedidos.Domain.Entities.Profile("ADM", "Admin");
        ctx.Profiles.Add(profile);
        await ctx.SaveChangesAsync();

        var user = new User("Maria", "Silva", "maria.silva@minerva.com", active: true);
        ctx.Users.Add(user);
        var entry = ctx.Entry(user);
        entry.Property("ProfileId").CurrentValue = profile.Id;
        entry.Property("RegistrationNumber").CurrentValue = "M001";
        entry.Property("PasswordHash").CurrentValue = "hash";
        await ctx.SaveChangesAsync();

        var sut = new UserReadRepository(ctx, CreateMapper());
        var result = await sut.GetByIdAsync(user.Id, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.FirstName.Should().Be("Maria");
        result.LastName.Should().Be("Silva");
        result.Email.Should().Be("maria.silva@minerva.com");
        result.Active.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_EmptyDb_ReturnsEmptyList()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var sut = new UserReadRepository(ctx, CreateMapper());

        var result = await sut.GetAllAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithUsers_ReturnsAllMapped()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();
        var profile = new Minerva.GestaoPedidos.Domain.Entities.Profile("ADM", "Admin");
        ctx.Profiles.Add(profile);
        await ctx.SaveChangesAsync();

        var user1 = new User("A", "B", "a@b.com", true);
        ctx.Users.Add(user1);
        var e1 = ctx.Entry(user1);
        e1.Property("ProfileId").CurrentValue = profile.Id;
        e1.Property("RegistrationNumber").CurrentValue = "M1";
        e1.Property("PasswordHash").CurrentValue = "h";
        await ctx.SaveChangesAsync();

        var sut = new UserReadRepository(ctx, CreateMapper());
        var result = await sut.GetAllAsync(CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].FirstName.Should().Be("A");
        result[0].Email.Should().Be("a@b.com");
    }
}
