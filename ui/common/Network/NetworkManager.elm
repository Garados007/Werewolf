module Network.NetworkManager exposing (..)

import Network
import Time exposing (Posix)
import WebSocket
import Ports
import Json.Decode as JD
import Task
import Process
import Set exposing (Set)

type NetworkManager
    = Unconnected
    | Connected ConnectionTarget
    | DisruptedConnected ConnectionTarget DisruptionInfo

type alias ConnectionTarget =
    { api: String
    , token: String
    }

type alias DisruptionInfo =
    { close: Network.SocketClose
    , time: Maybe Posix
    , latest: Maybe Posix
    }

type Msg
    = WsClosed (Result JD.Error Network.SocketClose)
    | GotTime Posix
    | PerformReconnect

hasError : NetworkManager -> Bool
hasError network =
    case network of
        DisruptedConnected _ _ -> True
        _ -> False

-- set of codes for which it is unlikely that we ever can reconnect
cannotReconnect : Set Int
cannotReconnect = Set.fromList
    [ 1012 -- forced restart of the server -> all lobbies are destroyed
    , 4404 -- lobby not found on the server
    ]

new : NetworkManager
new = Unconnected

connect : String -> String -> (NetworkManager, Cmd msg)
connect api token =
    ( Connected { api = api, token = token }
    , WebSocket.send Ports.sendSocketCommand
        <| WebSocket.Connect
            { name = "wss"
            , address =
                if String.startsWith "http" api
                then "ws" ++ String.dropLeft 4 api
                    ++ "/ws/" ++ token
                else "wss://" ++ api ++ "/ws/" ++ token
            , protocol = ""
            }
    )

receive : (Result JD.Error WebSocket.WebSocketMsg -> msg) -> Sub msg
receive tagger =
    Ports.receiveSocketMsg
        <| WebSocket.receive tagger

exit : (NetworkManager, Cmd msg)
exit =
    ( Unconnected
    , WebSocket.send Ports.sendSocketCommand
        <| WebSocket.Close
            { name = "wss" }
    )

reconnected : NetworkManager -> NetworkManager
reconnected manager =
    case manager of
        DisruptedConnected info _ -> Connected info
        _ -> manager

update : Msg -> NetworkManager -> (NetworkManager, Cmd Msg)
update msg manager =
    case (msg, manager) of
        (_, Unconnected) -> (manager, Cmd.none)
        (WsClosed (Err _), _) -> (manager, Cmd.none)
        (WsClosed (Ok reason), Connected info) ->
            ( DisruptedConnected info
                { close = reason
                , time = Nothing
                , latest = Nothing
                }
            , Task.perform GotTime Time.now
            )
        (WsClosed (Ok reason), DisruptedConnected info close) ->
            (DisruptedConnected info { close | close = reason }, Task.perform GotTime Time.now)
        (GotTime time, DisruptedConnected info close) ->
            let
                newClose : DisruptionInfo
                newClose =
                    { close
                    | latest = Just time
                    , time = Just <| Maybe.withDefault time close.time
                    }
            in
                ( DisruptedConnected info newClose
                , reconnectCmd newClose
                )
        (GotTime _, _) -> (manager, Cmd.none)
        (PerformReconnect, DisruptedConnected info _) ->
            connect info.api info.token
            |> Tuple.mapFirst
                (always manager)
        (PerformReconnect, _) -> (manager, Cmd.none)

canReconnect : DisruptionInfo -> Bool
canReconnect close =
    not (Set.member close.close.code cannotReconnect)
    &&  (case (close.latest, close.time) of
            (Just latest, Just time) -> Time.posixToMillis latest - Time.posixToMillis time < 60000
            _ -> True
        )

reconnectCmd : DisruptionInfo -> Cmd Msg
reconnectCmd close =
    if not <| canReconnect close
    then Cmd.none
    else Process.sleep 5000
        |> Task.perform (always PerformReconnect)

subscriptions : NetworkManager -> Sub Msg
subscriptions model =
    if model == Unconnected
    then Sub.none
    else Ports.receiveSocketClose
        <| WsClosed
        << JD.decodeValue
            (JD.map2 Network.SocketClose
                (JD.field "code" JD.int)
                (JD.field "reason" JD.string)
            )
