FROM bitnami/git:latest as version
WORKDIR /src
COPY ./.git ./.git
RUN git rev-parse --short HEAD > version-suffix

FROM mcr.microsoft.com/dotnet/sdk:5.0 as builder
WORKDIR /src
COPY ./Themes ./Themes
COPY ./Werewolf ./Werewolf
COPY ./Test ./Test
COPY ./Werewolf.sln ./Werewolf.sln
COPY ./version.txt ./version-prefix
COPY --from=version /src/version-suffix ./version-suffix
RUN mkdir -p /app && \
    verpre="$(cat "version-prefix")" && \
    versuf="$(cat "version-suffix")" && \
    dotnet build --nologo -c RELEASE \
        --version "$verpre-$versuf" && \
        # --version-prefix "$verpre" \
        # --version-suffix "$versuf" && \
    dotnet publish --nologo -c RELEASE -o /app \
        --version "$verpre-$versuf" && \
        # --version-prefix "$verpre" \
        # --version-suffix "$versuf" && \
    rm ./version-*

FROM mcr.microsoft.com/dotnet/runtime:5.0
WORKDIR /app
COPY --from=builder /app /app
CMD [ "dotnet", "/app/Werewolf.dll" ]
