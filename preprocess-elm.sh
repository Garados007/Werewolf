#!/bin/bash

function copy () {
    [ -d bin ] && rm -r bin
    mkdir -p bin
    cp elm.json bin/

    for file in $(find ui -name '*.elm'); do
        echo "preprocess $file"
        dir="$(dirname "bin/$file")"
        [ ! -d "$dir" ] && mkdir -p "$dir"
        cat "$file" | \
            sed -z -e "s/--\!BEGIN\n\([^-]\|-[^-]\|--[^\!]\|--\![^E]\|--\!E[^N]\|--\!EN[^D]\)*--\!END//g" \
            > "bin/$file"
    done
    for file in $(find ui -not -name '*.elm' -and -type f); do
        echo "copy $file"
        dir="$(dirname "bin/$file")"
        [ ! -d "$dir" ] && mkdir -p "$dir"
        cp "$file" "bin/$file"
    done
}

if [[ "$1" == "test" ]]; then
    copy > /dev/null
    pushd bin > /dev/null
    elm make --optimize ui/game/Main.elm
    code=$?
    popd > /dev/null
    rm -r bin
    exit $?
else
    copy
fi
