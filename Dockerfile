FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# De forma a poder usar bibliotecas de globalização e mostrar correctametne o símbolo €
RUN apt-get update && apt-get install -y libicu-dev locales
RUN sed -i '/pt_PT.UTF-8/s/^# //g' /etc/locale.gen && locale-gen
ENV LANG=pt_PT.UTF-8
ENV LC_ALL=pt_PT.UTF-8
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080 

ENV ASPNETCORE_URLS=http://+:8080 

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/PlantShop.Application/PlantShop.Application.csproj", "src/PlantShop.Application/"]
COPY ["src/PlantShop.Domain/PlantShop.Domain.csproj", "src/PlantShop.Domain/"]
COPY ["src/PlantShop.Infrastructure/PlantShop.Infrastructure.csproj", "src/PlantShop.Infrastructure/"]
COPY ["src/PlantShop.WebUI/PlantShop.WebUI.csproj", "src/PlantShop.WebUI/"]

RUN dotnet restore "src/PlantShop.WebUI/PlantShop.WebUI.csproj"

COPY . .

RUN dotnet publish "src/PlantShop.WebUI/PlantShop.WebUI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "PlantShop.WebUI.dll"]