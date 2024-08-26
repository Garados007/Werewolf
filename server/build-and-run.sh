#!/bin/bash

set -e

dotnet run --project ../../tools/LogicCompiler/LogicCompiler.csproj -- \
    -s ../../logic/werewolf/ \
    -t ../../tools/bin \
    -n "Theme.werewolf" \
    --write-ast --write-docs
if [ ! -f "../../tools/bin/bin.csproj" ]; then
    pushd ../../tools/bin
    dotnet new classlib
    rm Class1.cs
    dotnet add reference ../../server/Werewolf.Theme.Base/Werewolf.Theme.Base.csproj
    popd
fi
dotnet build ../../tools/bin -o bin/Debug/net8.0

set -a && source ../../.env && set +a
export GAME_SERVER_PLUGINS=bin.dll
export GAME_SERVER_WEBSERVER_PORT=8015
export GAME_SERVER_DOMAIN=http://localhost:8015
dotnet run
