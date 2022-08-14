module Views.ViewChat exposing (..)

import Data
import Language exposing (Language)
import Language.ServiceRenderer

import Html exposing (Html, div, text)
import Html.Attributes as HA exposing (class)
import Html.Events as HE
import Json.Decode as JD
import Dict

type Msg
    = SetInput String
    | Send String
    | Close

view : Language -> Data.Game -> List Data.ChatLog 
    -> String -> Html Msg
view lang game chats input =
    div [ class "chat-box" ]
        [ div [ class "chat-history", HA.id "chat-box-history" ]
            <| List.map
                (\{entry} ->
                    case entry of
                        Data.ChatEntryMessage message ->
                            viewChatEntry lang game message
                        Data.ChatEntryService message ->
                            viewChatService lang game message
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

viewChatEntry : Language -> Data.Game -> Data.ChatMessage -> Html Msg
viewChatEntry lang game chat =
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
    <| List.singleton
    <| Language.ServiceRenderer.renderTextTokens lang game
        (Dict.fromList
            <| List.filterMap identity
            [ Maybe.map (Tuple.pair "phase" << Data.TextVarPhase) chat.phase
            , Just <| ("user", Data.TextVarUser chat.sender)
            ]
        )
    <| List.filterMap identity
        [ if chat.phase == Nothing
            then Nothing
            else Just <| Language.ServiceRenderer.TokenVariable "phase"
        , Just <| Language.ServiceRenderer.TokenRaw " "
        , Just <| Language.ServiceRenderer.TokenVariable "user"
        , Just <| Language.ServiceRenderer.TokenRaw " "
        , Just <| Language.ServiceRenderer.TokenRaw chat.message
        ]

viewChatService : Language -> Data.Game -> Data.ChatServiceMessage -> Html Msg
viewChatService lang game chat =
    div
        [ HA.classList
            [ ("chat", True)
            , ("service", True)
            ]
        ]
        [ Language.ServiceRenderer.render lang game chat ]

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
