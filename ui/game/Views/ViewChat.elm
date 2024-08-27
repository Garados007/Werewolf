module Views.ViewChat exposing (..)

import Data
import Language exposing (Language)
import Language.ServiceRenderer

import Html exposing (Html, div, text)
import Html.Attributes as HA exposing (class)
import Html.Events as HE
import Html.Tools exposing (onNotShiftEnter)
import Dict
import Dict exposing (Dict)

type Msg
    = SetInput String
    | Send String
    | Close

view : Language -> Data.Game -> Dict String Data.GameUser -> List Data.ChatLog
    -> String -> Html Msg
view lang game removedUser chats input =
    div [ class "chat-box" ]
        [ div [ class "chat-history", HA.id "chat-box-history" ]
            <| List.map
                (\{entry} ->
                    case entry of
                        Data.ChatEntryMessage message ->
                            viewChatEntry lang game removedUser message
                        Data.ChatEntryService message ->
                            viewChatService lang game removedUser message
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

viewChatEntry : Language -> Data.Game -> Dict String Data.GameUser -> Data.ChatMessage -> Html Msg
viewChatEntry lang game removedUser chat =
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
    <| Language.ServiceRenderer.renderTextTokens lang game removedUser
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

viewChatService : Language -> Data.Game -> Dict String Data.GameUser -> Data.ChatServiceMessage -> Html Msg
viewChatService lang game removedUser chat =
    div
        [ HA.classList
            [ ("chat", True)
            , ("service", True)
            ]
        ]
        [ Language.ServiceRenderer.render lang game removedUser chat ]
