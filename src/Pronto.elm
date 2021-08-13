module Pronto exposing
    ( ServerState
    , ServerInfo
    , ServerGame
    , ProntoConfig
    , GetTargetHostArgs
    , TargetHost
    , JoinTokenInfo
    , getServerInfo
    , getTargetHost
    , getTargetHostArgs
    , getJoinTokenInfo
    )

import Time exposing (Posix)
import Http
import Url
import Json.Decode as JD exposing (Decoder)
import Json.Decode.Pipeline exposing (required, optional)
import Json.Encode as JE
import Html exposing (b)
import Iso8601

{-|
    This is the client side implementation of the pronto protocoll.
-}

type alias ServerState =
    { id: String
    , lastSeen: Posix
    , lastSeenSec: Float
    , info: ServerInfo
    }

decodeServerState : Decoder ServerState
decodeServerState =
    JD.succeed ServerState
        |> required "id" JD.string
        |> required "last-seen" 
            (JD.andThen
                (\str -> 
                    case JD.decodeValue Iso8601.decoder 
                        <| JE.string 
                        <| String.replace " " "T" str 
                    of
                        Ok date -> JD.succeed date
                        Err e -> JD.fail <| JD.errorToString e
                )
                JD.string
            )
        |> required "last-seen-sec" JD.float
        |> required "info" decodeServerInfo

type alias ServerInfo =
    { name: String
    , uri: String
    , developer: Bool
    , fallback: Bool
    , full: Bool
    , maintenance: Bool
    , maxClients: Maybe Int
    , games: List ServerGame
    }

decodeOptBool : String -> Decoder (Bool -> b) -> Decoder b
decodeOptBool name =
    optional
        name
        (JD.maybe JD.bool |> JD.map (Maybe.withDefault False))
        False

decodeServerInfo : Decoder ServerInfo
decodeServerInfo =
    JD.succeed ServerInfo
        |> required "name" JD.string
        |> required "uri" JD.string
        |> decodeOptBool "developer"
        |> decodeOptBool "fallback"
        |> decodeOptBool "full"
        |> decodeOptBool "maintenance"
        |> optional "max-clients" (JD.maybe JD.int) Nothing
        |> required "games" (JD.list decodeServerGame)

type alias ServerGame =
    { name: String
    , uri: String
    , rooms: Int
    , maxRooms: Maybe Int
    , clients: Int
    }

decodeServerGame : Decoder ServerGame
decodeServerGame =
    JD.succeed ServerGame
        |> required "name" JD.string
        |> required "uri" JD.string
        |> required "rooms" JD.int
        |> optional "max-rooms" (JD.maybe JD.int) Nothing
        |> required "clients" JD.int

type alias ProntoConfig =
    { host: String
    }

getServerInfo : ProntoConfig -> String -> Cmd (Maybe ServerState)
getServerInfo config id =
    Http.get
        { url = config.host ++ "/v1/info/" ++ Url.percentEncode id
        , expect = Http.expectJson
            Result.toMaybe
            decodeServerState
        }

type alias TargetHost =
    { id: String
    , apiUri: String
    , gameUri: String
    }

decodeTargetHost : Decoder TargetHost
decodeTargetHost =
    JD.succeed TargetHost
        |> required "id" JD.string
        |> required "api-uri" JD.string
        |> required "game-uri" JD.string

type alias GetTargetHostArgs =
    { game: String
    , developer: Bool
    , fallback: Bool
    }

getTargetHostArgs : String -> GetTargetHostArgs
getTargetHostArgs game =
    { game = game
    , developer = False
    , fallback = True
    }

getTargetHost : ProntoConfig -> GetTargetHostArgs -> Cmd (Maybe TargetHost)
getTargetHost config args =
    Http.get
        { url = config.host 
            ++ "/v1/new?game=" ++ Url.percentEncode args.game
            ++ "&developer=" ++ (if args.developer then "true" else "false")
            ++ "&fallback=" ++ (if args.fallback then "true" else "false")
        , expect = Http.expectJson
            Result.toMaybe
            decodeTargetHost
        }

type alias JoinTokenInfo =
    { server: String
    , game: String
    , lobby: String
    , apiUri: String
    , gameUri: Maybe String
    }

decodeJoinTokenInfo : Decoder JoinTokenInfo
decodeJoinTokenInfo =
    JD.succeed JoinTokenInfo
        |> required "server" JD.string
        |> required "game" JD.string
        |> required "lobby" JD.string
        |> required "api-uri" JD.string
        |> required "game-uri" (JD.nullable JD.string)

getJoinTokenInfo : ProntoConfig -> String -> Cmd (Maybe JoinTokenInfo)
getJoinTokenInfo config joinToken =
    Http.get
        { url = config.host ++ "/v1/token/" ++ Url.percentEncode joinToken
        , expect = Http.expectJson
            Result.toMaybe
            decodeJoinTokenInfo
        }
