module Views.ViewWinners exposing (..)

import Data
import Level exposing (Level, LevelData)

import Html exposing (Html, div, text)
import Html.Attributes as HA exposing (class)
import Dict exposing (Dict)
import Time exposing (Posix)
import Language exposing (Language)
import Avatar

view : Language -> Avatar.AvatarStorage -> Posix -> Dict String Level -> Data.Game -> List String
    -> Html Never
view lang avatar now levels game winners =
    div [ class "winner-box" ]
        <| List.map
            (\winner ->
                let
                    img : String
                    img = Dict.get winner game.user
                        |> Maybe.map (.user >> .img)
                        |> Maybe.withDefault ""

                    name : String
                    name = Dict.get winner game.user
                        |> Maybe.map (.user >> .name)
                        |> Maybe.withDefault winner

                    isGuest : Bool
                    isGuest = Dict.get winner game.user
                        |> Maybe.map (.user >> .isGuest)
                        |> Maybe.withDefault False

                    role : String
                    role = Language.getTextOrPath lang
                        [ "theme"
                        , "role"
                        , Dict.get winner game.user
                            |> Maybe.andThen .role
                            |> Maybe.andThen .role
                            |> Maybe.withDefault "unknown"
                        , "name"
                        ]

                    level : LevelData
                    level = Dict.get winner levels
                        |> Maybe.map (Level.getData now)
                        |> Maybe.withDefault
                            { level = 0
                            , xp = 0
                            , maxXp = 0
                            }

                in div [ class "winner" ]
                    [ div [ class "image" ]
                        [ Avatar.viewOrImg avatar img
                        , if isGuest
                            then div [ class "guest" ]
                                <| List.singleton
                                <| text
                                <| Language.getTextOrPath lang
                                    [ "user-stats", "guest" ]
                            else text ""
                        ]
                    , div [ class "name" ]
                        [ text name ]
                    , div [ class "role" ]
                        [ text role ]
                    , div
                        [ class "user-info-level" ]
                        [ div [ class "text" ]
                            [ div [] [ text "Level" ]
                            , div [] [ text <| String.fromInt level.level ]
                            ]
                        , div [ class "outer" ]
                            [ div
                                [ HA.style "width"
                                    <| (\x -> String.fromFloat x ++ "%")
                                    <|
                                        if level.xp == level.maxXp
                                        then 100
                                        else 100 * (toFloat level.xp) / (toFloat level.maxXp)
                                ]
                                []
                            ]
                        ]
                    ]
            )
        <| winners
