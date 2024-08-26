#!/bin/bash
suffix="$(sed -E "s/^[0-9\.]+\.([0-9]+)$/\1/g" version.txt)"
prefix="$(sed -E "s/^([0-9\.]+\.)[0-9]+$/\1/g" version.txt)"
let "suffix=suffix+1"
echo -n "$prefix$suffix" > version.txt
echo "New version is $(cat version.txt)"

git add version.txt
git commit -m "Move version to $(cat version.txt)"
git push
git tag -a "$(cat version.txt)" -m "Version $(cat version.txt)"
git push origin "$(cat version.txt)"
