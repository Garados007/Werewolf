module Views.ViewCloseReason exposing
    ( view
    )

import Html exposing (Html, div, text)
import Html.Attributes exposing (class)
import Html.Events as HE
import Network.NetworkManager exposing (DisruptionInfo)
import Views.ViewModal
import Language exposing (Language)
import Maybe.Extra
import Time exposing (Posix)
import Dict

view : Language -> Posix -> DisruptionInfo -> msg -> msg-> Html msg
view lang now info noop return =
    Html.map
        (\msg ->
            case msg of
                Views.ViewModal.Close -> noop
                Views.ViewModal.Wrap x -> x
        )
    <| Views.ViewModal.viewCondClose False
        ( Language.getTextOrPath lang
            [ "modals", "connection", "close", "title" ]
        )
        [ div [ class "connection-error" ]
            <| List.singleton
            <| text
            <| Maybe.withDefault ""
            <| Maybe.Extra.orElseLazy
                (\() ->
                    Just <| Language.getTextOrPath lang
                    [ "modals", "connection", "close", "desc" ]
                )
            <| Language.getText lang
                [ "modals", "connection", "code", String.fromInt info.close.code ]
        , info.latest
            |> Maybe.map
                (\latest ->
                    div [ class "connection-reconnect"]
                        [ text <|
                            if not <| Network.NetworkManager.canReconnect info
                            then Language.getTextOrPath lang
                                [ "modals", "connection", "timeout" ]
                            else if Time.posixToMillis now - Time.posixToMillis latest <= 5000
                            then Language.getTextFormatOrPath lang
                                [ "modals", "connection", "reconnect-in" ]
                                <| Dict.fromList
                                    [("sec", String.fromInt <| 5 - (Time.posixToMillis now - Time.posixToMillis latest) // 1000)]
                            else Language.getTextOrPath lang
                                [ "modals", "connection", "reconnect" ]
                        ]
                )
            |> Maybe.withDefault (text "")
        , div [ class "modal-button-container" ]
            <| List.singleton
            <| Html.button
            [ class "connection-go-back"
            , class "modal-button"
            , HE.onClick return
            ]
            [ text <| Language.getTextOrPath lang
                [ "modals", "connection", "go-back" ]
            ]
        ]
