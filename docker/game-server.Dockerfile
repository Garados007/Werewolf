FROM bitnami/git:latest as version
WORKDIR /src
COPY ./.git ./.git
RUN git rev-parse --short HEAD > version-suffix

FROM mcr.microsoft.com/dotnet/sdk:8.0 as builder
WORKDIR /src
COPY ./Werewolf.sln ./Werewolf.sln
COPY ./version.txt ./version-prefix
COPY --from=version /src/version-suffix ./version-suffix
RUN verpre="$(cat "version-prefix")" && \
    versuf="$(cat "version-suffix")"
# build tools
COPY ./tools ./tools
RUN mkdir -p /tools && \
    dotnet build --nologo -c RELEASE \
        /p:version="$verpre-$versuf" \
        tools/LogicCompiler/LogicCompiler.csproj && \
    dotnet publish --nologo -c RELEASE -o /tools \
        /p:version="$verpre-$versuf" \
        tools/LogicCompiler/LogicCompiler.csproj
# build logic files
COPY ./Themes ./Themes
COPY ./logic ./logic
RUN mkdir -p /src/server/Theme && \
    cd /src/server/Theme && \
    dotnet new classlib && \
    dotnet add reference /src/Themes/Werewolf.Theme.Base/Werewolf.Theme.Base.csproj && \
        find /src/logic/ -mindepth 1 -maxdepth 1 -type d | \
        xargs -I {} basename {} | \
        while read -r name; do dotnet /tools/LogicCompiler.dll \
            --source /src/logic/$name \
            --target /src/server/Theme/$name \
            --namespace Theme.$name || exit $?; \
        done && \
    cd /src && \
    dotnet sln add server/Theme/Theme.csproj
COPY ./Werewolf ./Werewolf
RUN cd /src/Werewolf && \
    dotnet add reference /src/server/Theme/Theme.csproj && \
    cd /src
# build server
RUN mkdir -p /app && \
    dotnet build --nologo -c RELEASE \
        /p:version="$verpre-$versuf" \
        Werewolf/Werewolf.csproj && \
    dotnet publish --nologo -c RELEASE -o /app \
        /p:version="$verpre-$versuf" \
        Werewolf/Werewolf.csproj && \
    rm ./version-* && \
    echo "Theme.dll" >> /app/plugins.txt

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=builder /app /app
CMD [ "dotnet", "/app/Werewolf.dll" ]
