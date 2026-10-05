$ErrorActionPreference = 'Stop'
dotnet restore
dotnet build .\NeckFlow.sln -c Release
