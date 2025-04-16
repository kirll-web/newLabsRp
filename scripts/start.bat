
set DB_MAIN=localhost:6000
set DB_RU=localhost:6001
set DB_EU=localhost:6002
set DB_ASIA=localhost:6003

docker-compose up -d

docker start rabbitmq-pa3
# start docker run -p 6000:6379 redis
# start docker run -p 6001:6379 redis
# start docker run -p 6002:6379 redis
# start docker run -p 6003:6379 redis

timeout /t 5 /nobreak >nul 

cd ..\valuator
dotnet restore

dotnet build

start "Valuator 5001" dotnet run --urls "http://0.0.0.0:5001"
timeout /t 3 /nobreak >nul 
start "Valuator 5002" dotnet run --no-build --urls "http://0.0.0.0:5002"
start "Valuator 5003" dotnet run --no-build --urls "http://0.0.0.0:5003"
start "Valuator 5004" dotnet run --no-build --urls "http://0.0.0.0:5004"
cd ../nginx
start nginx.exe

cd ../RankCalculator

dotnet restore
dotnet build

cd ../

start "RankCalculator 1" dotnet run --project RankCalculator
timeout /t 3 /nobreak >nul 
start "RankCalculator 2" dotnet run --no-build --project RankCalculator
start "RankCalculator 3" dotnet run --no-build --project RankCalculator

start "EventsLogger 1" dotnet run --project EventsLogger
timeout /t 3 /nobreak >nul 
start "EventsLogger 2" dotnet run --no-build --project EventsLogger

cd scripts