module Views.ViewRoomEditor exposing (..)

import Data
import Network exposing (NetworkRequest(..), EditGameConfig, editGameConfig)
import Model

import Html exposing (Html, div, text)
import Html.Attributes as HA exposing (class)
import Html.Events as HE
import Dict exposing (Dict)
import Maybe.Extra
import Language exposing (Language, LanguageInfo)
import Json.Decode as JD
import Json.Encode as JE

type Msg
    = SetBuffer (Dict String Int) EditGameConfig
    | SendConf EditGameConfig
    | StartGame
    | ShowRoleInfo String
    | Noop

view : Language -> LanguageInfo -> Data.RoleTemplates
    -> Data.GameUserResult
    -> Maybe Language.ThemeRawKey -> Data.Game -> Bool
    -> Dict String Int -> Html Msg
view lang langInfo roles gameResult theme game editable buffer =
    let

        handleNewRoleCount : String -> Maybe Int -> Msg
        handleNewRoleCount id value =
            case value of
                Nothing -> Noop
                Just new ->
                    (\dict -> 
                        SetBuffer dict 
                        { editGameConfig | newConfig = Just dict }
                    )
                    <| Dict.insert id new buffer

        viewSingleRoleBox : String -> Html Msg
        viewSingleRoleBox id =
            div [ class "editor-role-box" ]
                [ div
                    [ class "editor-role-info"
                    , HE.onClick <| ShowRoleInfo id
                    ]
                    [ text "i" ]
                , Html.img 
                    [ class "editor-role-image"
                    , HA.src "/content/games/werwolf/img/assasin.svg"
                    ] []
                , div [ class "editor-role-name" ]
                    <| List.singleton
                    <| text
                    <| Language.getTextOrPath lang
                        [ "theme", "roles", id ]
                , div 
                    [ HA.classList
                        [ ("editor-role-number", True)
                        , ("editable", editable)
                        ]
                    ]
                    [ Html.input
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
                        , HA.disabled <| not editable
                        , HE.onInput 
                            <| handleNewRoleCount id 
                            << String.toInt
                        ] []
                    , div
                        [ class "down"
                        , HE.onClick
                            <| handleNewRoleCount id
                            <| Just
                            <| max 0
                            <| (+) -1
                            <| Maybe.withDefault 0
                            <| Maybe.Extra.orLazy
                                (Dict.get id buffer)
                                (\() -> Dict.get id game.config)
                        ] []
                    , div
                        [ class "up"
                        , HE.onClick
                            <| handleNewRoleCount id
                            <| Just
                            <| min 500
                            <| (+) 1
                            <| Maybe.withDefault 0
                            <| Maybe.Extra.orLazy
                                (Dict.get id buffer)
                                (\() -> Dict.get id game.config)
                        ] []
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

        userLang : String
        userLang = Model.getSelectedLanguage gameResult

        viewThemeSelector : Bool -> Html Msg
        viewThemeSelector enabled =
            div [ class "theme-selector" ]
                <| List.singleton
                <| Html.select
                [ HA.disabled <| not enabled
                , HE.on "change"
                    <| JD.map
                        (\key -> SendConf { editGameConfig | theme = Just key })
                    <| JD.andThen
                        (\raw ->
                            case
                                JD.decodeString
                                    ( JD.map2 Tuple.pair
                                        (JD.index 0 JD.string)
                                        (JD.index 1 JD.string)
                                    )
                                    raw
                            of
                                Ok value -> JD.succeed value
                                Err _ -> JD.fail "error"
                        )
                    <| HE.targetValue
                ]
            <| List.map
                (\((tk1, tk2), value) ->
                    Html.option
                        [ HA.selected
                            <| (tk1, tk2) == game.theme
                        , HA.value 
                            <| JE.encode 0
                            <| JE.list JE.string
                                [ tk1, tk2 ]
                        ]
                        [ text value ]
                )
            <| List.concatMap
                (\(k1, d1) ->
                    List.map
                        (Tuple.mapFirst <| Tuple.pair k1)
                    <| List.filterMap
                        (\(k2, d2) ->
                            Dict.get userLang d2
                            |> Maybe.Extra.orElseLazy
                                (\() -> Dict.get "de" d2)
                            |> Maybe.Extra.orElseLazy
                                (\() -> Dict.values d2 |> List.head)
                            |> Maybe.map
                                (Tuple.pair k2)
                        )
                    <| Dict.toList d1
                )
            <| Dict.toList langInfo.themes

    in div [ class "editor" ]
        [ div [ class "editor-roles" ]
            -- <| List.map viewSingleRole
            <| List.map viewSingleRoleBox
            <| Maybe.withDefault []
            <| Maybe.andThen
                (\(k, _) -> Dict.get k roles)
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
                    [ "settings", "game-room", "all-can-see-role-of-dead" ]
                )
                True
                game.allCanSeeRoleOfDead
                <| \new -> SendConf
                    { editGameConfig
                    | newAllCanSeeRoleOfDead = Just new
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
        , viewThemeSelector editable
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

