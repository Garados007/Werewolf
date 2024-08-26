module Translation.ViewProgress exposing (view)

import Html exposing (Html, div, text)
import Html.Attributes exposing (class, style)

view : String -> Int -> Int -> Html msg
view title min max =
    div [ class "loading-full" ]
    <| List.singleton
    <| div [ class "loading-box" ]
        [ div [ class "loading-progress" ]
            <| List.singleton
            <| div 
                [ style "width"
                    <| String.fromInt (100 * min // max) ++ "%"
                ] []
        , div [ class "loading-state" ]
            [ text title ]
        ]
