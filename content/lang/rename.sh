#!/bin/bash

# use it like
#     ./rename.sh '.theme.phases.AmorPhase' '.theme.phases.AmorSelection'
# this will rename any path into the new one. Empty objects are not removed.

old=$1
new=$2

set -e

if [ ! "$LOG" == "0" ]; then
    if [ ! -f log.sh ]; then
        echo "#!/bin/bash" > log.sh
    fi
    echo "LOG=0 ./rename.sh '$old' '$new'" >> log.sh
fi

while read -r line; do
    value="$(jq -c "$old" "$line")"
    if [ "$value" == "null" ]; then
        continue
    fi
    echo "found in $line"
    jq -S --indent 4 "del($old) | ($new |= $value)" "$line" > "$line.tmp"
    mv "$line.tmp" "$line"
done < <(find . -name "*.json")
