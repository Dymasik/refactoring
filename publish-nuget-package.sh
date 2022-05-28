dotnet build ".\DataManagmentSystem.Common\DataManagmentSystem.Common.csproj" --configuration Release
cd \DataManagmentSystem.Common
cd \bin
cd \Release
dotnet nuget push DataManagmentSystem.Common.$1.nupkg -k $2 -s https://api.nuget.org/v3/index.json
dotnet nuget locals all --clear