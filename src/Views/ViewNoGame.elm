module Views.ViewNoGame exposing (..)

import Html exposing (Html, div, text)
import Html.Attributes as HA exposing (class)
import Language exposing (Language)

view : Language -> Bool -> Html msg
view lang isLoading =
    if isLoading
    then text ""
    else
    div [ class "frame-status-box" ]
        <| List.singleton
        <| div [ class "frame-status" ]
        <| List.singleton
        <| text
        <| Language.getTextOrPath lang
            [ "game"
            , "loading"
            , "not-found"
            ]
