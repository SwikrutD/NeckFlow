$ErrorActionPreference = 'Stop'
dotnet publish .\src\NeckFlow\NeckFlow.csproj -c Release -r win-x64 --self-contained false -o .\publish\win-x64
