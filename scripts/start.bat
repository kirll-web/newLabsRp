docker-compose up -d

docker start rabbitmq-pa3
docker start my-redis

timeout /t 5 /nobreak >nul 

cd ..\valuator
start "Valuator 5001" dotnet run --urls "http://0.0.0.0:5001"
timeout /t 2 /nobreak >nul 
start "Valuator 5002" dotnet run --urls "http://0.0.0.0:5002"
timeout /t 2 /nobreak >nul 
start "Valuator 5003" dotnet run --urls "http://0.0.0.0:5003"
timeout /t 2 /nobreak >nul 
start "Valuator 5004" dotnet run --urls "http://0.0.0.0:5004"
timeout /t 2 /nobreak >nul 
cd ../nginx
start nginx.exe

cd ../

dotnet restore

dotnet run

start "Consumer 1" dotnet run --project Consumer1
timeout /t 2 /nobreak >nul 
start "Consumer 2" dotnet run --project Consumer1
timeout /t 2 /nobreak >nul 
start "Consumer 3" dotnet run --project Consumer1


cd scripts