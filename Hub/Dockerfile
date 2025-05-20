# Укажите базовый образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Hub/Hub.csproj", "Hub/"]
RUN dotnet restore "Hub/Hub.csproj"
COPY . .
WORKDIR "/src/Hub"
RUN dotnet build "Hub.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Hub.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Hub.dll"]
