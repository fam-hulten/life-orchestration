FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/LifeOrchestration.Core/LifeOrchestration.Core.csproj src/LifeOrchestration.Core/
COPY src/LifeOrchestration.Infrastructure/LifeOrchestration.Infrastructure.csproj src/LifeOrchestration.Infrastructure/
COPY src/LifeOrchestration.Api/LifeOrchestration.Api.csproj src/LifeOrchestration.Api/

RUN dotnet restore src/LifeOrchestration.Api/LifeOrchestration.Api.csproj

COPY src/ src/
RUN dotnet publish src/LifeOrchestration.Api/LifeOrchestration.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
VOLUME [ "/app" ]

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT [ "dotnet", "LifeOrchestration.Api.dll" ]
