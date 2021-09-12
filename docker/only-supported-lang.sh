#!/bin/bash

supported="$(jq -r ".languages | keys | .[]" content/lang/index.json)"

for file in $(find content/lang/ -name "*.json"); do
    name="$(basename $file .json)"
    if [ "$name" == "index" ]; then
        continue;
    fi
    if $(echo "$supported" | grep -qx "$name" > /dev/null); then
        continue;
    fi
    rm "$file"
done