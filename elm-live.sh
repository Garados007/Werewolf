#!/bin/bash
elm-live \
    --port=8000 \
    --start-page=content/index.html \
    ui/game/Main.elm \
    -x /api \
    -y http://localhost:8015 \
    -u \
    -- --output=content/index.js
