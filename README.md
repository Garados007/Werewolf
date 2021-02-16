# Werewolf
An online implementation of the popular game werewolfes of millers hill. This uses a C# web server 
as a backend and elm as frontend.

## Design

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
> Internal Api is in `Werewolf.Game.Api`.

You can create as much game server you want. Each game server has to be accessible from the
internet. You can put a proxy such as NginX in front and give each one its own subdomain.

This game server communicates with the player with a HTTP web server. They are Rest and WebSocket
Apis provided.

The game server communicates with the user db for the user data.

The game server doesn't create and manage game sessons on its own. It provides an Api 
`Werewolf.Game.Api` for it.

### Game Server Multiplexer

> Implementation is in `Werewolf.Game.Multiplex`.
> Internal Api is in `Werewolf.Game.Api`.

This multiplexer wraps all instances of game servers (and possibly other multiplexer) to create
a load balancer.

If this multiplexer needs to create a room it searches for a available game server first and
directs to them.

You can also use this multiplexer (this is also availble in the game server) to provide static
content. If you have a proxy like NginX in front it is recommended to let them handle the static
content. All static content is provided on `/content/` on every server.

### Game Link

> This is just a concept.

The game server and multiplexer provides an Api to create an manage game rooms. This Api can be
used from other systems to manage new rooms. In future is planed to add this feature directly in
the core.

Right now is planed to have a discord bot which do this.

### User UI

> Elm implementation is in `src`.
> Styles in `/content/css/`.

The UI is written in Elm which is later compiled into JavaScript. This UI is provided from the
game server, the multiplexer and/or the proxy server.
