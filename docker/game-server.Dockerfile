FROM mcr.microsoft.com/dotnet/sdk:5.0 as builder
WORKDIR /src
COPY ./Themes ./Themes
COPY ./Werewolf ./Werewolf
COPY ./Werewolf.sln ./Werewolf.sln
RUN mkdir -p /app && \
    dotnet build --nologo -c RELEASE && \
    dotnet publish --nologo -c RELEASE -o /app

FROM mcr.microsoft.com/dotnet/runtime:5.0
WORKDIR /app
COPY --from=builder /app /app
CMD [ "dotnet", "/app/Werewolf.dll" ]
