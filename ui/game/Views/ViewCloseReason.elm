module Views.ViewCloseReason exposing
    ( view
    )

import Html exposing (Html, div, text)
import Html.Attributes exposing (class)
import Html.Events as HE
import Network exposing (SocketClose)
import Views.ViewModal
import Language exposing (Language)
import Maybe.Extra

view : Language -> SocketClose -> msg -> msg-> Html msg
view lang close noop return =
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
                [ "modals", "connection", "code", String.fromInt close.code, close.reason ]
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
