module Views.ViewPlayerNotification exposing (..)

import Data
import Html exposing (Html, div, text)
import Html.Attributes as HA exposing (class)
import Dict
import Language exposing (Language)

view : Language -> Maybe Data.Game -> String -> List String -> Html Never
view lang game notificationId player =
    div [ class "notification-box" ]
        [ div [ class "player-list" ]
            <| List.filterMap
                (\id -> Maybe.map
                    (\user ->
                        div [ class "player" ]
                            [ div [ class "player-image" ]
                                <| List.singleton
                                <| Html.img
                                    [ HA.src user.img ]
                                    []
                            , div [ class "player-name" ]
                                [ text <| user.name ]
                            ]
                    )
                    <| Dict.get id
                    <| Maybe.withDefault Dict.empty
                    <| Maybe.map .user game
                )
            <| player
        , div [ class "notification-text" ]
            <| List.singleton
            <| text
            <| Language.getTextOrPath lang
                [ "theme", "event", "player-notification", notificationId ]
        ]
