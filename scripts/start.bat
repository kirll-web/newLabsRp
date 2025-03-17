
cd ..\valuator
start "Valuator 5001" dotnet run --urls "http://0.0.0.0:5001"
start "Valuator 5002" dotnet run --urls "http://0.0.0.0:5002"
start "Valuator 5003" dotnet run --urls "http://0.0.0.0:5003"
start "Valuator 5004" dotnet run --urls "http://0.0.0.0:5004"

cd ../nginx
start nginx.exe

cd ../scripts