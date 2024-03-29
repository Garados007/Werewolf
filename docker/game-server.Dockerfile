FROM bitnami/git:latest as version
WORKDIR /src
COPY ./.git ./.git
RUN git rev-parse --short HEAD > version-suffix

FROM mcr.microsoft.com/dotnet/sdk:6.0 as builder
WORKDIR /src
COPY ./Themes ./Themes
COPY ./Werewolf ./Werewolf
COPY ./Test ./Test
COPY ./Translate ./Translate
COPY ./Werewolf.sln ./Werewolf.sln
COPY ./version.txt ./version-prefix
COPY --from=version /src/version-suffix ./version-suffix
RUN mkdir -p /app && \
    verpre="$(cat "version-prefix")" && \
    versuf="$(cat "version-suffix")" && \
    dotnet build --nologo -c RELEASE \
        /p:version="$verpre-$versuf" \
        Werewolf/Werewolf.csproj && \
    dotnet publish --nologo -c RELEASE -o /app \
        /p:version="$verpre-$versuf" \
        Werewolf/Werewolf.csproj && \
    rm ./version-*

FROM mcr.microsoft.com/dotnet/runtime:6.0
WORKDIR /app
COPY --from=builder /app /app
CMD [ "dotnet", "/app/Werewolf.dll" ]
