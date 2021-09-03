module Views.ViewUserPreview exposing (..)

import Html exposing (Html, div, text)
import Html.Attributes as HA exposing (class)
import Html.Events as HE
import Language exposing (Language)
import Data exposing (UserInfo)

view : Language -> Bool -> UserInfo -> Html ()
view lang guest user =
    div [ class "user-container" ]
        [ div
            [ class "user-frame"
            , class "me"
            ]
            [ div [ class "user-image-box" ]
                [ div [ class "user-image" ]
                    [ Html.img
                        [ HA.src user.picture ]
                        []
                    , if guest
                        then div [ class "guest" ]
                            <| List.singleton
                            <| text
                            <| Language.getTextOrPath lang
                                [ "user-stats", "guest" ]
                        else text ""
                    ]
                ]
            , div [ class "user-info-box" ]
                [ div [ class "user-name" ]
                    [ Html.span [] [ text user.username ] ]
                ]
            ]
        , div [ class "reset-user" ]
            <| List.singleton
            <| Html.button 
                [ HE.onClick () ]
            <| List.singleton
            <| text
            <| Language.getTextOrPath lang
                [ "init", "reset-user" ]
        ]
