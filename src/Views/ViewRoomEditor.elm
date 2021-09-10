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
import Dict exposing (Dict)
import Maybe.Extra
import Language exposing (Language)
import Language.Config exposing (LangConfig)
import Json.Decode as JD
import Json.Encode as JE
import Url
import Set exposing (Set)

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

view : Language -> LangConfig -> Data.RoleTemplates
    -> Maybe Language.ThemeRawKey -> Data.Game -> Bool
    -> EditorPage -> Dict String Int -> Set String -> Html Msg
view lang langConfig roles theme game editable page buffer missingImg =
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

        showImg : String -> String -> String -> Html Msg
        showImg imgClass fallback path =
            Html.node "picture" [ class imgClass ]
                [ Html.source
                    [ HA.attribute "srcset"
                        <| if Set.member path missingImg
                            then fallback
                            else path
                    ] []
                , Html.img
                    [ HA.src
                        <| if Set.member path missingImg
                            then fallback
                            else path
                    , HE.on "error" <| JD.succeed <| MissingImg path
                    , class imgClass
                    ] []
                ]

        viewSingleRoleBox : String -> Html Msg
        viewSingleRoleBox id =
            div [ class "editor-role-box" ]
                [ div
                    [ class "editor-role-info"
                    , HE.onClick <| ShowRoleInfo id
                    ]
                    [ text "i" ]
                , showImg "editor-role-image" "/content/img/assasin.svg"
                    <| "/content/img/roles/" ++
                        (case theme of
                            Just (k, _) -> Url.percentEncode k ++ "/"
                            Nothing -> ""
                        ) ++
                        Url.percentEncode id ++ ".png"
                -- , Html.object 
                --     [ class "editor-role-image"
                --     , HA.attribute "data" 
                --         <| "/content/img/roles/" ++
                --             (case theme of
                --                 Just (k, _) -> Url.percentEncode k ++ "/"
                --                 Nothing -> ""
                --             ) ++
                --             Url.percentEncode id ++ ".png"
                --     ]
                --     [ Html.img
                --         [ HA.src "/content/img/assasin.svg"
                --         , HA.type_ "image/svg+xml"
                --         , class "editor-role-image"
                --         ] []
                --     ]
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
        
        maxPlayer =
            if game.leaderIsPlayer
            then Dict.size game.user
            else Dict.size game.user - 1
        maxRoles = List.sum <| Dict.values 
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
                            Dict.get langConfig.lang d2
                            |> Maybe.Extra.orElseLazy
                                (\() -> Dict.get "de" d2)
                            |> Maybe.Extra.orElseLazy
                                (\() -> Dict.values d2 |> List.head)
                            |> Maybe.map
                                (Tuple.pair k2)
                        )
                    <| Dict.toList d1
                )
            <| Dict.toList langConfig.info.themes

    in div [ class "editor" ]
        [ viewPageSelector lang page
        , viewRoleBar
        , case page of
            PageTheme ->
                div [ class "editor-content-theme" ]
                    [ viewThemeSelector editable ]
            PageRole ->
                div [ class "editor-content-roles" ]
                    [ div [ class "editor-roles" ]
                        -- <| List.map viewSingleRole
                        <| List.map viewSingleRoleBox
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

