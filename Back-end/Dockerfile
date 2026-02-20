# Build and run the Minerva.GestaoPedidos.WebApi.
# Para resolver os nomes dos containers (gestcontas_postgres, gestcontas_mongo, etc.),
# execute a API na mesma rede Docker que os bancos/serviços (ex.: docker run --network=gestcontas_net ... ou use docker-compose com a mesma rede).

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
# curl para HEALTHCHECK e diagnósticos; --no-install-recommends e limpeza para reduzir tamanho da imagem
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && apt-get clean
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copia apenas os arquivos de projeto para otimizar cache de restore
COPY ["src/Presentation/Minerva.GestaoPedidos.WebApi/Minerva.GestaoPedidos.WebApi.csproj", "src/Presentation/Minerva.GestaoPedidos.WebApi/"]
COPY ["src/Core/Minerva.GestaoPedidos.Application/Minerva.GestaoPedidos.Application.csproj", "src/Core/Minerva.GestaoPedidos.Application/"]
COPY ["src/Core/Minerva.GestaoPedidos.Domain/Minerva.GestaoPedidos.Domain.csproj", "src/Core/Minerva.GestaoPedidos.Domain/"]
COPY ["src/Infrastructure/Minerva.GestaoPedidos.Infrastructure/Minerva.GestaoPedidos.Infrastructure.csproj", "src/Infrastructure/Minerva.GestaoPedidos.Infrastructure/"]

RUN dotnet restore "src/Presentation/Minerva.GestaoPedidos.WebApi/Minerva.GestaoPedidos.WebApi.csproj"

# Copia o restante do código-fonte
COPY . .

WORKDIR "/src/src/Presentation/Minerva.GestaoPedidos.WebApi"
RUN dotnet build "Minerva.GestaoPedidos.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Minerva.GestaoPedidos.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
# Garante que curl está disponível para o HEALTHCHECK (instalado no stage base)
RUN which curl
COPY --from=publish /app/publish .

# Health check: consulta o endpoint /health da API (Liveness/Readiness)
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Minerva.GestaoPedidos.WebApi.dll"]
