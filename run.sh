#!/bin/sh

dotnet build /p:Platform=x64
dotnet ./MapStudio/bin/x64/Debug/net5.0/MapStudio.dll
