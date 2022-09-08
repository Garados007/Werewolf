# Werewolf

This is yet another implementation of the popular game werewolves of millers hill which can be
played online with your friends together. This is pretty similar to Mafia or Among Us. In this game
you have two parties in a small village: the normal villager and some werewolves. The goal of the
game is that the werewolves eliminate all normal villager and the goal for the villager is to
survive this. This game has multiple rounds and different phases with special actions.

The game is a lot of fun for me and my friends. So much fun that I decided to release it as a web
application so that we can play anytime, even if we can't physically meet. That was especially the
case at the time of Corona.

The project is a big construction site and I have ideas all over the place about how to expand on
that so that I can achieve my goal of including all the characters and features from the official
game, including expansions. From there, things can still change here in the future.

If you want to try this game you can check out here:
[werewolf.2complex.de](https://werewolf.2complex.de/).

If you want to help me providing translations, images or the code base you can create an issue. I am
happy to include your submissions. I'll put some more guidance in the wiki that can help you with
this.

If you want to host your own server, please contact me first. I want to have all instances to run
the latest version of the game all the time and for this we need some preparation (e.g. Github
webhooks, keys). Also for this some services needs to be prepared and the documentation is missing
yet (mostly because this is still in work).

---

> Rest of the old Readme. I will move this to the wiki later.

An online implementation of the popular game werewolfes of millers hill. This uses a C# web server 
as a backend and elm as frontend.

## Design


The backend is split into several distinct services with their own purpose. The servers can be on
different computes and the game server can be startet multiple times to scale this setup.

Some of these services have their implementation here.

The backend is split into several distinct server which is specialised for their own purpose. The
server can be on different computer and some servers can be added to scale this setup.

### User DB Server

> Implementation is in `Werewolf.Users`. 
> Api is in `Werewolf.Users.Api`.

It is recommended to have only one instance of a user server. The purpose of this server is:

- Store all the user information and their rankings
- Provide the data if requested
- Update new rankings
- Notify other instances of new rankings

### Game Server

> Implementation is in `Werewolf.Game`.

You can create as much game server you want. Each game server has to be accessible from the
internet. You can put a proxy such as NginX in front and give each one its own subdomain.

This game server communicates with the player with a HTTP web server. They are Rest and WebSocket
Apis provided.

The game server communicates with the user db for the user data.

### Game Server Multiplexer

> This use the [Pronto](https://github.com/Garados007/pronto/) project.

This server lists all the active instances and allows the ui to select automaticly which game
server to use.

### User UI

> Elm implementation is in `src`.
> Styles in `/content/css/`.

The UI is written in Elm which is later compiled into JavaScript. This UI is provided from the
game server, the multiplexer and/or the proxy server.

It is recommended that you provide the build on a static web server or you just use a single Game
Server for it.

### OAuth Authentification

> Any OAuth2 Provider

This project will be developeded and testet to run with [Keycloak](https://www.keycloak.org/).

If the user intents to have an permanent account to get high scores and levels, (s)he needs to
create an account on an OAuth2 Provider and login.

The player can use this app in guest mode but this will not create any rankings and the user
informationen will be deleted after usage.
