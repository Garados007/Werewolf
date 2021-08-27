module Views.ViewCloseReason exposing
    ( view
    )

import Html exposing (Html, div, text)
import Html.Attributes exposing (class)
import Network exposing (SocketClose)
import Views.ViewModal
import Language exposing (Language)
import Maybe.Extra

view : Language -> SocketClose -> Html ()
view lang close =
    Html.map (always ())
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
        ]