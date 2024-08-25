#!/bin/bash

# first check if we are in the right directory

if [ ! -f docker-compose.yml ]; then
    echo "No docker compose file found" > /dev/fd/2
    exit 1
fi

# check if the keyfile exists

if [ ! -f "maintenance.key" ]; then
    echo "maintenance key file missing" > /dev/fd/2
    exit 1
fi

# build the new docker compose setup

docker compose build || exit $?

# check if docker is running

if $(docker ps | grep "werewolf_game" > /dev/null); then

    # docker is running, request maintenance
    curl -d "key=$(cat maintenance.key)&reason=install+updates" \
        http://localhost:8100/api/maintenance
    echo
    echo "Maintenance requested"
    id="$(docker ps -aqf "ancestor=werewolf_game")"
    echo "Wait for exist of container $id"
    docker container wait "$id"
fi

# restart docker compose

if [ ! $(docker compose restart) ]; then
    docker compose up -d
fi
