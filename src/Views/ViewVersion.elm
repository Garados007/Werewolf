module Views.ViewVersion exposing (..)

import Config
import Html exposing (Html, div, text, a)
import Html.Attributes exposing (class, href, target)

view : Html msg
view = div [ class "version-notice" ]
    [ div [ class "liner" ]
        [ text "This page is made with ♥ from Max Brauer." ]
    , div [ class "cookies" ]
        [ text "This page doesn't make use of any cookies or tracking techniques." ]
    , a [ class "sourcer"
        , href "https://github.com/Garados007/Werewolf"
        , target "_blank"
        ]
        [ text Config.version ]
    ]