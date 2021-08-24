#!/bin/bash
dotnet test --nologo --no-restore --logger:"html;LogFileName=$(pwd)/test-report.html"
sed -i \
    -e "s/display:none;//" \
    -e "s@$(pwd)@.@" \
    -e "s/width : 150px;/min-width : 150px;/" \
    test-report.html
