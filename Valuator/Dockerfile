# Укажите ваш базовый образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Valuator/Valuator.csproj", "Valuator/"]
RUN dotnet restore "Valuator/Valuator.csproj"
COPY . .
WORKDIR "/src/Valuator"
RUN dotnet build "Valuator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Valuator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Valuator.dll"]