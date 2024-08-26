module Views.ViewPlayerNotification exposing (..)

import Data
import Html exposing (Html, div, text)
import Html.Attributes as HA exposing (class)
import Dict exposing (Dict)
import Language exposing (Language)
import Maybe.Extra

view : Language -> Maybe Data.Game -> Dict String Data.GameUser -> String -> List String
    -> Html Never
view lang game removedUser notificationId player =
    div [ class "notification-box" ]
        [ div [ class "player-list" ]
            <| List.filterMap
                (\id -> Maybe.map
                    (\user ->
                        div [ class "player" ]
                            [ div [ class "player-image" ]
                                [ Html.img
                                    [ HA.src user.img ]
                                    []
                                , if user.isGuest
                                    then div [ class "guest" ]
                                        <| List.singleton
                                        <| text
                                        <| Language.getTextOrPath lang
                                            [ "user-stats", "guest" ]
                                    else text ""
                                ]
                            , div [ class "player-name" ]
                                [ text <| user.name ]
                            ]
                    )
                    <| Maybe.Extra.orElseLazy
                        (\() ->
                            Dict.get id removedUser
                        )
                    <| Maybe.map .user
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
