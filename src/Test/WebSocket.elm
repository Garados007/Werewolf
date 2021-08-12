port module Test.WebSocket exposing (..)

import Json.Decode as JD
import Json.Encode as JE
import WebSocket

import Html exposing (Html, div)
import Html.Attributes as HA
import Html.Events as HE
import Browser
import Set exposing (Set)

port receiveSocketMsg : (JD.Value -> msg) -> Sub msg
port sendSocketCommand : JE.Value -> Cmd msg

type alias Model =
    { input: String
    , output: List String
    }

type Msg
    = WsMsg (Result JD.Error WebSocket.WebSocketMsg)
    | SetInput String
    | Send

main : Platform.Program () Model Msg
main = Browser.element
    { init = \() ->
        Tuple.pair
            { input = ""
            , output = []
            }
        <| WebSocket.send sendSocketCommand
        <| WebSocket.Connect
            { name = "wss"
            , address = "ws://localhost:8000"
            , protocol = ""
            }
    , view = \model ->
        div []
            [ Html.input
                [ HA.type_ "text"
                , HA.value model.input
                , HE.onInput SetInput
                ] []
            , Html.button
                [ HE.onClick Send ]
                [ Html.text "Send" ]
            , Html.text <| Debug.toString model
            ] 
    , update = \msg model ->
        case msg of
            WsMsg (Ok (WebSocket.Data d)) ->
                Tuple.pair
                    { model
                    | output = model.output ++
                        [ d.data ]
                    }
                    Cmd.none
            WsMsg (Ok (WebSocket.Error d)) ->
                Tuple.pair
                    { model
                    | output = model.output ++
                        [ d.error ]
                    }
                    Cmd.none
            WsMsg (Err e) ->
                Tuple.pair
                    { model
                    | output = model.output ++
                        [ Debug.toString e ]
                    }
                    Cmd.none
            SetInput text ->
                ({ model | input = text }, Cmd.none)
            Send ->
                Tuple.pair model
                    <| WebSocket.send sendSocketCommand
                    <| WebSocket.Send
                        { name = "wss"
                        , content = model.input
                        }
    , subscriptions = \model ->
        receiveSocketMsg <| WebSocket.receive WsMsg
    }