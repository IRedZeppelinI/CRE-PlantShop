FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

EXPOSE 8080 

ENV ASPNETCORE_URLS=http://+:8080 

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["PlantShop.sln", "."]
COPY ["src/PlantShop.Application/PlantShop.Application.csproj", "src/PlantShop.Application/"]
COPY ["src/PlantShop.Domain/PlantShop.Domain.csproj", "src/PlantShop.Domain/"]
COPY ["src/PlantShop.Infrastructure/PlantShop.Infrastructure.csproj", "src/PlantShop.Infrastructure/"]
COPY ["src/PlantShop.WebUI/PlantShop.WebUI.csproj", "src/PlantShop.WebUI/"]

RUN dotnet restore "PlantShop.sln"

COPY . .

RUN dotnet publish "src/PlantShop.WebUI/PlantShop.WebUI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "PlantShop.WebUI.dll"]