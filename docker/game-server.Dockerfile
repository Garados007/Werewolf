FROM bitnami/git:latest as version
WORKDIR /src
COPY ./.git ./.git
COPY ./version.txt ./
RUN echo "$(cat version.txt)-$(git rev-parse --short HEAD)" > version

FROM bitnami/java:latest as grammar-builder
WORKDIR /src
COPY ./tools/LogicCompiler/grammar ./
COPY ./tools/LogicCompiler/antlr-4.13.1-complete.jar ./
RUN java -jar antlr-4.13.1-complete.jar -message-format vs2005 -long-messages -Werror -Dlanguage=CSharp -no-listener -package LogicCompiler.Grammar W5LogicLexer.g4 W5LogicParser.g4

FROM mcr.microsoft.com/dotnet/sdk:8.0 as builder
WORKDIR /src
COPY ./Werewolf.sln ./Werewolf.sln
COPY --from=version /src/version ./version
# build tools
COPY ./tools ./tools
COPY --from=grammar-builder /src ./tools/LogicCompiler/grammar
RUN mkdir -p /tools && \
    dotnet build --nologo -c RELEASE \
        /p:version="$(cat "version")" \
        tools/LogicCompiler/LogicCompiler.csproj && \
    dotnet publish --nologo -c RELEASE -o /tools \
        /p:version="$(cat "version")" \
        tools/LogicCompiler/LogicCompiler.csproj
# build logic files
COPY ./server/Werewolf.Theme.Base ./server/Werewolf.Theme.Base
COPY ./logic ./logic
RUN mkdir -p /src/server/Theme && \
    cd /src/server/Theme && \
    dotnet new classlib && \
    dotnet add reference /src/server/Werewolf.Theme.Base/Werewolf.Theme.Base.csproj && \
        find /src/logic/ -mindepth 1 -maxdepth 1 -type d | \
        xargs -I {} basename {} | \
        while read -r name; do dotnet /tools/LogicCompiler.dll \
            --source /src/logic/${name} \
            --target /src/server/Theme/${name} \
            --namespace Theme.${name} || exit $?; \
        done && \
    cd /src && \
    dotnet sln add server/Theme/Theme.csproj
COPY ./server/Werewolf ./server/Werewolf
RUN cd /src/server/Werewolf && \
    dotnet add reference /src/server/Theme/Theme.csproj && \
    cd /src
# build server
RUN mkdir -p /app && \
    dotnet build --nologo -c RELEASE \
        /p:version="$(cat "version")" \
        server/Werewolf/Werewolf.csproj && \
    dotnet publish --nologo -c RELEASE -o /app \
        /p:version="$(cat "version")" \
        server/Werewolf/Werewolf.csproj && \
    rm ./version && \
    echo "Theme.dll" >> /app/plugins.txt

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=builder /app /app
CMD [ "dotnet", "/app/Werewolf.dll" ]
