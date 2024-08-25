module Views.ViewRoomEditor exposing
    ( Msg(..)
    , view
    )

import Data
import Network exposing (NetworkRequest(..), EditGameConfig, editGameConfig)
import Model exposing (EditorPage(..))

import Html exposing (Html, div, text)
import Html.Attributes as HA exposing (class)
import Html.Events as HE
import Html.Lazy
import Dict exposing (Dict)
import Language exposing (Language)
import Language.Config exposing (LangConfig)
import Json.Decode as JD
import Url
import Set exposing (Set)
import Views.Icons

type Msg
    = SetBuffer (Dict String Int) EditGameConfig
    | SetPage EditorPage
    | SendConf EditGameConfig
    | StartGame
    | ShowRoleInfo String
    | MissingImg String
    | Noop

viewPageSelector : Language -> EditorPage -> Html Msg
viewPageSelector lang page =
    div [ class "editor-page-selector" ]
        <| List.map
            (\(theme, key) ->
                Html.button
                    [ HA.disabled <| page == theme
                    , HE.onClick <| SetPage theme
                    ]
                    [ text <| Language.getTextOrPath lang
                        [ "settings", "page", key ]

                    ]
            )
            [ (PageTheme, "theme")
            , (PageRole, "roles")
            , (PageOptions, "options")
            ]

handleNewRoleCount : Dict String Int -> String -> Maybe Int -> Msg
handleNewRoleCount config id value =
    case value of
        Nothing -> Noop
        Just new ->
            (\dict ->
                SetBuffer dict { editGameConfig | newConfig = Just dict }
            )
            <| if new == 0 then Dict.remove id config else Dict.insert id new config

showImg : Set String -> String -> String -> List String -> Html Msg
showImg missingImg imgClass fallback paths =
    let
        path : String
        path = Maybe.withDefault fallback
            <| List.head
            <| List.filter (\x -> not <| Set.member x missingImg)
            <| paths

    in Html.node "picture"
        [ class imgClass ]
        [ Html.source
            [ HA.attribute "srcset" path
            ] []
        , Html.img
            [ HA.src path
            , HE.on "error" <| JD.succeed <| MissingImg path
            , class imgClass
            ] []
        ]

viewSingleRoleBox : Language -> Maybe Language.ThemeRawKey -> Dict String Int -> Set String -> Bool -> String -> Html Msg
viewSingleRoleBox = Html.Lazy.lazy6 <| \lang theme config missingImg editable id ->
    let
        current : Int
        current = Maybe.withDefault 0
            <| Dict.get id config
    in div [ class "editor-role-box" ]
        [ div
            [ class "editor-role-info"
            , HE.onClick <| ShowRoleInfo id
            ]
            [ text "i" ]
        , showImg missingImg "editor-role-image" "/content/img/assasin.svg"
            <| List.singleton
            <| "/content/img/roles/" ++
                (case theme of
                    Just (k, _) -> Url.percentEncode k ++ "/"
                    Nothing -> ""
                ) ++
                Url.percentEncode id ++ ".png"
        , div [ class "editor-role-name" ]
            <| List.singleton
            <| text
            <| Language.getTextOrPath lang
                [ "theme", "role", id, "name" ]
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
                    <| String.fromInt current
                , HA.disabled <| not editable
                , HE.onInput
                    <| handleNewRoleCount config id
                    << String.toInt
                ] []
            , div
                [ class "down"
                , HE.onClick
                    <| handleNewRoleCount config id
                    <| Just
                    <| max 0
                    <| current - 1
                ] []
            , div
                [ class "up"
                , HE.onClick
                    <| handleNewRoleCount config id
                    <| Just
                    <| min 500
                    <| current + 1
                ] []
            ]
        ]


view : Language -> LangConfig -> Data.RoleTemplates
    -> Maybe Language.ThemeRawKey -> Data.Game -> Bool
    -> EditorPage -> Set String -> Html Msg
view lang langConfig roles theme game editable page missingImg =
    let

        maxPlayer =
            if game.leaderIsPlayer
            then Dict.size game.user
            else Dict.size game.user - 1
        maxRoles = List.sum <| Dict.values game.config

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
                                <| (*) 100
                                <| clamp 0 1
                                <| if maxPlayer == 0
                                    then 0
                                    else toFloat maxRoles / toFloat maxPlayer
                            ) ++ "%"
                        ] []
                , div [ class "editor-bar-desc" ]
                    <| List.singleton
                    <| text
                    <| Language.getTextFormatOrPath lang
                        [ "settings", "bar", "stats" ]
                    <| Dict.fromList
                        [ ("roles", String.fromInt maxRoles)
                        , ("player", String.fromInt maxPlayer)
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

        viewThemeSelector2 : Bool -> Html Msg
        viewThemeSelector2 enabled =
            div [ class "theme-selector" ]
            <| List.map
                (\(system, info) ->
                    div [ class "theme-selector-system" ]
                        [ div [ class "theme-selector-system-title" ]
                            [ text
                                <| Maybe.withDefault system
                                <| Dict.get langConfig.lang info.title
                            ]
                        , div [ class "theme-selector-variants" ]
                            <| List.map
                                (\(key, names) ->
                                    div [ HA.classList
                                            [ ("theme-selector-variant", True)
                                            , ("enabled", enabled)
                                            ]
                                        , HE.onClick <|
                                            if enabled
                                            then SendConf
                                                { editGameConfig
                                                | theme = Just (system, key)
                                                }
                                            else Noop
                                        ]
                                        [ if game.theme == (system, key)
                                            then div [ class "theme-selector-variant-current" ]
                                                <| List.singleton
                                                <| Html.map (always Noop)
                                                <| Views.Icons.svgTick
                                            else text ""
                                        , showImg missingImg "theme-selector-variant-img"
                                            "/content/img/system/icon-fallback.png"
                                            [ "/content/img/system/icons/" ++ system ++ "/" ++ key ++ ".png"
                                            , "/content/img/system/icons/" ++ system ++ ".png"
                                            ]
                                        , div [ class "theme-selector-variant-title" ]
                                            <| List.singleton
                                            <| text
                                            <| Maybe.withDefault key
                                            <| Dict.get langConfig.lang names
                                        , div [ class "theme-selector-langs" ]
                                            <| List.map
                                                (\icon ->
                                                    Html.span
                                                        [ class "flag-icon"
                                                        , class <| "flag-icon-" ++ icon
                                                        ] []
                                                )
                                            <| List.map
                                                (\icon ->
                                                    Dict.get icon langConfig.info.icons
                                                    |> Maybe.withDefault icon
                                                )
                                            <| Dict.keys names
                                        ]
                                )
                            <| Dict.toList
                            <| Maybe.withDefault Dict.empty
                            <| Dict.get system langConfig.info.themes
                        ]
                )
            <| Dict.toList langConfig.info.system

    in div [ class "editor" ]
        [ viewPageSelector lang page
        , viewRoleBar
        , case page of
            PageTheme ->
                div [ class "editor-content-theme" ]
                    [ viewThemeSelector2 editable ]
            PageRole ->
                div [ class "editor-content-roles" ]
                    [ div [ class "editor-roles" ]
                        <| List.map
                            (viewSingleRoleBox lang theme game.config missingImg editable)
                        <| Maybe.withDefault []
                        <| Maybe.andThen
                            (\(k, _) -> Dict.get k roles)
                        <| theme
                    ]
            PageOptions ->
                div [ class "editor-content-options" ]
                    [ div [ class "editor-checks" ]
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

