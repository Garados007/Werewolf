#!/bin/bash
suffix="$(sed -E "s/^[0-9\.]+\.([0-9]+)$/\1/g" version.txt)"
prefix="$(sed -E "s/^([0-9\.]+\.)[0-9]+$/\1/g" version.txt)"
let "suffix=suffix+1"
echo -n "$prefix$suffix" > version.txt
echo "New version is $(cat version.txt)"
