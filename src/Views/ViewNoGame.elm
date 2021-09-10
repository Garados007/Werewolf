module Views.ViewNoGame exposing (..)

import Html exposing (Html, div, text)
import Html.Attributes exposing (class)
import Html.Events as HE
import Language exposing (Language)

view : Language -> Bool -> Html ()
view lang isLoading =
    if isLoading
    then text ""
    else
    div [ class "frame-status-box" ]
        <| List.singleton
        <| div [ class "frame-status" ]
        [ text
            <| Language.getTextOrPath lang
                [ "game"
                , "loading"
                , "not-found"
                ]
        , Html.button
            [ class "frame-go-back" 
            , HE.onClick ()
            ]
            [ text <| Language.getTextOrPath lang
                [ "game", "loading", "go-back" ]

            ]
        ]
