# Укажите базовый образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["RankCalculator/RankCalculator.csproj", "RankCalculator/"]
RUN dotnet restore "RankCalculator/RankCalculator.csproj"
COPY . .
WORKDIR "/src/RankCalculator"
RUN dotnet build "RankCalculator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RankCalculator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RankCalculator.dll"]