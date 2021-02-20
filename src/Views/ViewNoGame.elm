module Views.ViewNoGame exposing (..)

import Html exposing (Html, div, text)
import Html.Attributes exposing (class)
import Language exposing (Language)

view : Language -> Bool -> Html msg
view lang isLoading =
    div [ class "frame-status-box" ]
        <| List.singleton
        <| div [ class "frame-status" ]
        <| List.singleton
        <| text
        <| Language.getTextOrPath lang
            [ "game"
            , "loading"
            , if isLoading then "is-loading" else "not-found"
            ]
