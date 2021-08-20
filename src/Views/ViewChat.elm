module Views.ViewChat exposing (..)

import Data
import Language exposing (Language)

import Html exposing (Html, div, text)
import Html.Attributes as HA exposing (class)
import Html.Events as HE
import Json.Decode as JD
import Dict

type Msg
    = SetInput String
    | Send String
    | Close

view : Language -> Data.Game -> List Data.ChatMessage 
    -> String -> Html Msg
view lang game chats input =
    div [ class "chat-box" ]
        [ div [ class "chat-history", HA.id "chat-box-history" ]
            <| List.map
                (\chat ->
                    div
                        [ HA.classList
                            [ ("chat", True)
                            , ("can-send", chat.canSend)
                            ]
                        , HA.title <|
                            if chat.canSend
                            then ""
                            else Language.getTextOrPath lang
                                [ "chat", "not-allowed" ]
                        ]
                    <| List.filterMap identity
                        [ Maybe.map
                                (Html.span [ class "chat-phase" ] << List.singleton)
                            <| Maybe.map
                                (\x -> text <| "[" ++ x ++ "]")
                            <| Maybe.map
                                (\name ->
                                    Language.getTextOrPath lang
                                        [ "theme", "phases", name ]
                                )
                            <| chat.phase
                        , Just
                            <| Html.span [ class "chat-user-name" ]
                            <| List.singleton
                            <| text
                            <| (\x -> "[" ++ x ++ "]")
                            <| Maybe.withDefault chat.sender
                            <| Maybe.map (.user >> .name)
                            <| Dict.get chat.sender game.user
                        , Just
                            <| Html.span [ class "chat-message" ]
                            <| List.singleton
                            <| text chat.message
                        ]
                )
            <| List.reverse
            <| chats
        , div [ class "chat-input-box" ]
            [ Html.textarea
                [ HA.value input
                , HE.onInput SetInput
                , onNotShiftEnter <| Send input
                ] []
            , div 
                [ class "chat-input-send"
                , HE.onClick <| Send input
                ] []
            ]
        , div 
            [ class "chat-box-close" 
            , HE.onClick Close ]
            [ text "X" ]
        ]

onNotShiftEnter : msg -> Html.Attribute msg
onNotShiftEnter event =
    HE.custom "keydown"
        <| JD.andThen
            (\(code, shift) ->
                if code == 13 && not shift
                then JD.succeed
                    { message = event
                    , stopPropagation = True
                    , preventDefault = True
                    }
                else JD.fail "shift+enter"
            )
        <| JD.map2
            Tuple.pair
            HE.keyCode
        <| JD.field "shiftKey" JD.bool
