#!/bin/bash

function generate_sub () {
    local name="$1"
    pushd LangSubConfigGenerator >> /dev/null
    dotnet run -- "../../Themes/$name/$name.csproj" "../../content/lang-config/sub/$name.json"
    popd >> /dev/null
}

while IFS= read -r line; do
    generate_sub "$line"
done << EOM
Werewolf.Theme.Default
EOM
