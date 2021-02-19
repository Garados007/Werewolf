module Views.ViewRoomEditor exposing (..)

import Data
import Network exposing (NetworkRequest(..), EditGameConfig, editGameConfig)

import Html exposing (Html, div, text)
import Html.Attributes as HA exposing (class)
import Html.Events as HE
import Dict exposing (Dict)
import Maybe.Extra
import Language exposing (Language)

type Msg
    = SetBuffer (Dict String Int) EditGameConfig
    | SendConf EditGameConfig
    | StartGame
    | Noop

view : Language -> Data.RoleTemplates -> Maybe Language.ThemeKey -> Data.Game -> Bool 
    -> Dict String Int -> Html Msg
view lang roles theme game editable buffer =
    let

        handleInput : String -> String -> Msg
        handleInput id value =
            case String.toInt value of
                Nothing -> Noop
                Just new ->
                    SetBuffer
                        (Dict.insert id new buffer)
                        { editGameConfig
                        | newConfig = Just
                            <| Dict.insert id new buffer
                        }
        
        viewSingleRole : String -> Html Msg
        viewSingleRole id =
            div [ class "editor-role-box" ]
                [ div [ class "editor-role-name" ]
                    <| List.singleton
                    <| text
                    <| Language.getTextOrPath
                        lang
                        [ "theme", "roles", id ]
                , if editable
                    then Html.input
                        [ HA.type_ "number"
                        , HA.min "0"
                        , HA.step "1"
                        , HA.max "500"
                        , HA.value
                            <| String.fromInt
                            <| Maybe.withDefault 0
                            <| Maybe.Extra.orLazy
                                (Dict.get id buffer)
                                (\() -> Dict.get id game.config)
                        , HE.onInput <| handleInput id
                        ] []
                    else text ""
                , div 
                    [ class "editor-role-count"
                    , HA.title <| Language.getTextOrPath lang
                        [ "settings", "game-room", "editor-role-count" ]
                    ]
                    <| List.singleton
                    <| text
                    <| String.concat
                        [ "("
                        , String.fromInt
                            <| Maybe.withDefault 0
                            <| Dict.get id game.config
                        , ")"
                        ]
                ]
    
        maxPlayer = (+) 1 <| Dict.size game.participants
        maxRoles = (+) 1 <| List.sum <| Dict.values 
            <| Dict.union buffer game.config

        viewRoleBar : Html msg
        viewRoleBar = 
            div [ HA.classList
                    [ ("editor-bar-box", True)
                    , ("overflow", maxRoles > maxPlayer)
                    ]
                ]
                [ div [ class "editor-bar-fill-outer" ]
                    <| List.singleton
                    <| div
                        [ class "editor-bar-fill-inner" 
                        , HA.style "width" <|
                            (String.fromFloat
                                <| 100 *
                                    (toFloat <| min maxPlayer maxRoles) /
                                    (toFloat <| max maxPlayer maxRoles)
                            ) ++ "%"
                        ] []
                , div
                    [ class "editor-bar-player-box" 
                    , HA.style "left" <|
                        (String.fromFloat
                            <| min 100
                            <| 100 * (toFloat maxPlayer) / (toFloat maxRoles)
                        ) ++ "%"
                    ]
                    [ div [ class "line" ] []
                    , div [ class "number" ] [ text <| String.fromInt <| maxPlayer - 1 ]
                    ]
                , div
                    [ class "editor-bar-roles-box" 
                    , HA.style "left" <|
                        (String.fromFloat
                            <| min 100
                            <| 100 * (toFloat maxRoles) / (toFloat maxPlayer)
                        ) ++ "%"
                    ]
                    [ div [ class "line" ] []
                    , div [ class "number" ] [ text <| String.fromInt <| maxRoles - 1 ]
                    ]
                ]

        viewCheckbox : String -> Bool -> Bool -> (Bool -> Msg) -> Html Msg
        viewCheckbox title enabled checked onChange =
            Html.label 
                [ HA.classList
                    [ ("disabled", not enabled)
                    ]
                ]
                [ Html.input
                    [ HA.type_ "checkbox" 
                    , HA.checked checked
                    , HE.onCheck
                        <| if editable
                            then onChange
                            else always Noop
                    , HA.disabled <| not <| editable && enabled
                    ] []
                , Html.span [] [ text title ]
                ]

    in div [ class "editor" ]
        [ div [ class "editor-roles" ]
            <| List.map viewSingleRole
            <| Maybe.withDefault []
            <| Maybe.andThen
                (\(k, _, _) -> Dict.get k roles)
            <| theme
        , viewRoleBar
        , div [ class "editor-checks" ]
            [ viewCheckbox 
                (Language.getTextOrPath lang 
                    [ "settings", "game-room", "leader-is-player" ]
                )
                True
                game.leaderIsPlayer
                <| \new -> SendConf
                    { editGameConfig
                    | leaderIsPlayer = Just new
                    }
            , viewCheckbox 
                (Language.getTextOrPath lang 
                    [ "settings", "game-room", "dead-can-see-all-roles" ]
                )
                True
                game.deadCanSeeAllRoles
                <| \new -> SendConf
                    { editGameConfig
                    | newDeadCanSeeAllRoles = Just new
                    }
            , viewCheckbox 
                (Language.getTextOrPath lang 
                    [ "settings", "game-room", "auto-start-votings" ]
                )
                (not game.leaderIsPlayer)
                game.autostartVotings
                <| \new -> SendConf
                    { editGameConfig
                    | autostartVotings = Just new
                    }
            , viewCheckbox 
                (Language.getTextOrPath lang 
                    [ "settings", "game-room", "auto-finish-votings" ]
                )
                (not game.votingTimeout && not game.leaderIsPlayer)
                game.autofinishVotings
                <| \new -> SendConf
                    { editGameConfig
                    | autofinishVotings = Just new
                    }
            , viewCheckbox
                (Language.getTextOrPath lang 
                    [ "settings", "game-room", "votings-timeout" ]
                )
                (not game.autofinishVotings && not game.leaderIsPlayer)
                game.votingTimeout
                <| \new -> SendConf
                    { editGameConfig
                    | votingTimeout = Just new
                    }
            , viewCheckbox
                (Language.getTextOrPath lang 
                    [ "settings", "game-room", "auto-finish-rounds" ]
                )
                (not game.leaderIsPlayer)
                game.autofinishRound
                <| \new -> SendConf
                    { editGameConfig
                    | autofinishRound = Just new
                    }
            ]
        , if editable
            then div 
                [ HA.classList
                    [ ("start-button", True)
                    , Tuple.pair "disabled"
                        <| maxRoles /= maxPlayer
                    ]
                , HE.onClick <| if maxRoles /= maxPlayer 
                    then Noop 
                    else StartGame
                ]
                [ text
                    <| Language.getTextOrPath lang
                    [ "settings", "game-room", "start" ]
                ]
            else text ""
        ]

