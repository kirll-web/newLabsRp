dotnet restore
dotnet build

docker-compose up -d


dotnet run --no-build --project=Consumer/


dotnet run --no-build --project=Producer/