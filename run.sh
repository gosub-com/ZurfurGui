dotnet publish ./samples/TestApp/TestApp.Browser --configuration Release
scp -r ./samples/TestApp/TestApp.Browser/bin/Release/net10.0-browser/publish/wwwroot/* root@gosub.com:~/v5/www/zurfurgui/.

