module Views.ViewUserList exposing (Msg (..), view)

import Data
import Network exposing (Request(..), SocketRequest(..), NetworkRequest(..))
import Language exposing (Language)

import Html exposing (Html, div, text)
import Html.Attributes as HA exposing (class)
import Html.Events as HE
import Dict exposing (Dict)
import Svg
import Svg.Attributes as SA
import Level exposing (Level, LevelData)
import Time exposing (Posix)

type Msg
    = Send Request
    | CopyToClipboard String
    | SetStreamerMode Bool

view : Language -> Posix -> Dict String Level -> Data.Game -> String -> Maybe Data.LobbyJoinToken
    -> Maybe Posix -> Bool -> Html Msg
view lang now levels game myId joinToken codeCopyTimestamp streamerMode =
    let
        getLeaderSpecText : String -> (() -> String) -> String
        getLeaderSpecText id func =
            if id == game.leader
            then
                ( (++)
                    <| Language.getTextOrPath lang
                        [ "theme", "names", "game-leader" ]
                ) <|
                if game.leaderIsPlayer
                then ", " ++ func ()
                else ""
            else func ()

        getUserRole : String -> String
        getUserRole id = getLeaderSpecText id <| \() ->
            case Dict.get id game.user |> Maybe.map .role of
                Just Nothing ->
                    Language.getTextOrPath lang
                        [ "theme", "names", "member" ]
                Nothing ->
                    Language.getTextOrPath lang
                        [ "theme", "names", "player" ]
                Just (Just player) ->
                    String.concat
                        <| List.intersperse " "
                        <| (::)
                            ( player.role
                                |> Maybe.map
                                    (\rid ->
                                        Language.getTextOrPath lang
                                            [ "theme", "roles", rid ]
                                    )
                                |> Maybe.withDefault
                                    ( Language.getTextOrPath lang
                                        [ "theme", "roles", "unknown" ]
                                    )
                            )
                        <| List.map
                            (\tag ->
                                Language.getTextOrPath lang
                                    [ "theme", "tags", tag ]
                            )
                        <| player.tags

        viewGameUser : String -> Data.GameUser -> Html Msg
        viewGameUser id user =
            div [ HA.classList
                    [ ("user-frame", True)
                    , ("me", myId == id)
                    , Tuple.pair "dead"
                        <| not
                        <| case Dict.get id game.user |> Maybe.map .role of
                            Just (Just player) -> 
                                not <| List.member "not-alive" player.tags
                            _ -> True
                    ]
                ]
                [ div [ class "user-image-box" ]
                    <| List.singleton
                    <| div [ class "user-image" ]
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
                , div [ class "user-info-box" ]
                    [ div [ class "user-name" ]
                        [ Html.span [] [ text user.name ]
                        , if Dict.get id game.user
                                |> Maybe.map (.online >> .isOnline)
                                |> Maybe.withDefault True
                            then text ""
                            else Html.span [ class "offline" ]
                                <| List.singleton
                                <| text
                                <| Language.getTextOrPath lang
                                    [ "user-stats", "offline" ]
                        ]
                    , div [ class "user-role" ]
                        [ text <| getUserRole id ]
                    ]
                , div [ class "user-info-stats" ]
                    [ div 
                        [ HA.title
                            <| Language.getTextOrPath lang
                                [ "user-stats", "won-games" ]
                        ]
                        [ svgWinner
                        , Html.span [] [ text <| String.fromInt user.stats.winGames ]
                        ]
                    , div 
                        [ HA.title
                            <| Language.getTextOrPath lang
                                [ "user-stats", "died" ]
                        ]
                        [ svgKill
                        , Html.span [] [ text <| String.fromInt user.stats.killed ]
                        ]
                    , div 
                        [ HA.title
                            <| Language.getTextOrPath lang
                                [ "user-stats", "played-games" ]
                        ]
                        [ svgGames
                        , Html.span [] [ text <| String.fromInt <| user.stats.winGames + user.stats.looseGames ]
                        ]
                    , div 
                        [ HA.title
                            <| Language.getTextOrPath lang
                                [ "user-stats", "leader" ]
                        ]
                        [ svgLeader
                        , Html.span [] [ text <| String.fromInt user.stats.leader ]
                        ]
                    ]
                ,   let
                        level : LevelData
                        level = Dict.get id levels
                            |> Maybe.map (Level.getData now)
                            |> Maybe.withDefault
                                { level = 0
                                , xp = 0
                                , maxXp = 0
                                }

                    in div
                        [ class "user-info-level" ]
                        [ div [ class "text" ]
                            [ div [] 
                                <| List.singleton
                                <| text
                                <| Language.getTextOrPath lang
                                    [ "user-stats", "level" ]
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
                , if myId == game.leader && myId /= id
                    then div
                        [ class "kick"
                        , HA.title
                            <| Language.getTextOrPath lang
                                [ "user-stats", "player-kick" ]
                        , HE.onClick <| Send <| SockReq <| KickUser id
                        ]
                        [ text "X" ]
                    else text ""
                ]

        joinTokenValidTime : Maybe Int
        joinTokenValidTime =
            Maybe.andThen
                (\x -> if x < 0 then Nothing else Just x)
            <| Maybe.map
                (\{ aliveUntil } -> 
                    Time.posixToMillis aliveUntil
                    - Time.posixToMillis now
                )
                joinToken

        isCodeCopied : Bool
        isCodeCopied =
            case codeCopyTimestamp of
                Nothing -> False
                Just stamp ->
                    (Time.posixToMillis stamp) + 2000 >= Time.posixToMillis now

        viewJoinToken : () -> Html Msg
        viewJoinToken () =
            div [ class "join-token" 
                ]
                [ div [ class "title" ]
                    <| List.singleton
                    <| text
                    <| Language.getTextOrPath lang
                        [ "join-token", "title" ]
                , div [ class "description" ]
                    <| List.singleton
                    <| text
                    <| Language.getTextOrPath lang
                        [ "join-token", "description" ]
                , case (joinToken, joinTokenValidTime) of
                    (Just code, Just _) ->
                        div [ class "box" 
                            , HE.onClick <| CopyToClipboard code.token
                            , HA.title
                                <| Language.getTextOrPath lang
                                    [ "join-token", "copy-hint" ]
                            ]
                            [ div [ class "code" ]
                                [ text <|
                                    if streamerMode
                                    then Language.getTextOrPath lang
                                        [ "join-token", "stream-replacement" ]
                                    else code.token 
                                ]
                            , div [ class "hint" ]
                                <| List.singleton
                                <| text
                                <| Language.getTextOrPath lang
                                    [ "join-token", "hint" ]
                            , if isCodeCopied
                                then div [ class "copied" ]
                                    <| List.singleton
                                    <| text
                                    <| Language.getTextOrPath lang
                                        [ "join-token", "copied" ]
                                else text ""
                            ]
                    _ ->
                        div [ class "box" 
                            , HE.onClick
                                <| Send
                                <| SockReq
                                <| RefetchJoinToken
                            ]
                            [ div [ class "refetch" ]
                                <| List.singleton
                                <| text
                                <| Language.getTextOrPath lang
                                    [ "join-token", "refetch" ]
                            ]
                , Html.label []
                    [ Html.input
                        [ HA.type_ "checkbox"
                        , HA.checked streamerMode
                        , HE.onCheck SetStreamerMode
                        ] []
                    , Html.span []
                        <| List.singleton
                        <| text
                        <| Language.getTextOrPath lang
                            [ "join-token", "streamer-mode" ]
                    ]
                , case joinTokenValidTime of
                    Nothing -> text ""
                    Just time ->
                        div [ class "time" ]
                        <| List.singleton
                        <| text
                        <| Language.getTextFormatOrPath lang
                            [ "join-token", "time" ]
                        <| Dict.fromList
                            [ Tuple.pair "minute"
                                <| String.fromInt
                                <| time // 60000
                            , Tuple.pair "second-leading"
                                <|  if (modBy 60 <| time // 1000) < 10
                                    then "0"
                                    else ""
                            , Tuple.pair "second"
                                <| String.fromInt
                                <| modBy 60 
                                <| time // 1000
                            ]
                ]

    in Dict.toList game.user
        |> List.sortBy
            (\(id, _) ->
                ( case Dict.get id game.user |> Maybe.andThen .role of
                    Just { tags } ->
                        if List.member "not-alive" tags
                        then 1
                        else 0
                    _ -> 2
                , id
                )
            )
        |> List.map
            (\(id, user) -> viewGameUser id user.user)
        |>  (\list ->
                if game.leader == myId
                then list ++ [ viewJoinToken () ]
                else list
            )
        |> div [ class "user-container" ]

svgWinner : Html msg
svgWinner =
    Svg.svg
        [ SA.height "512"
        , SA.width "512"
        , SA.viewBox "0 0 512 512"
        ]
    <| List.singleton
    <| Svg.g []
        [ Svg.path
            [ SA.d """m437.974 181.556c0-100.11-81.633-181.556-181.974-181.556s-181.973
81.446-181.973 181.556c0 57.689 27.108 109.18 69.283 142.46v177.984c0 3.328 1.655 6.438
4.416 8.295 2.76 1.858 6.266 2.223 9.348.969l98.926-40.183 98.928 40.184c1.213.493 2.49.735
3.762.735 1.962 0 3.911-.577 5.585-1.705 2.76-1.858 4.416-4.968 4.416-8.295v-177.984c42.175-33.28
69.283-84.771 69.283-142.46zm-89.283 305.589-88.928-36.122c-2.413-.98-5.113-.98-7.526 0l-88.928
36.122v-149.396c27.164 16.107 58.864 25.364 92.691 25.364s65.527-9.257
92.691-25.364zm-92.691-144.032c-89.313 0-161.974-72.474-161.974-161.557s72.662-161.556
161.974-161.556 161.974 72.474 161.974 161.556-72.661 161.557-161.974 161.557z"""
            ] []
        , Svg.path
            [ SA.d """m256 45.837c-75.004 0-136.025 60.883-136.025 135.719s61.021 135.72
136.025 135.72 136.025-60.884 136.025-135.72-61.02-135.719-136.025-135.719zm0 251.439c-63.977
0-116.025-51.912-116.025-115.72s52.048-115.719 116.025-115.719 116.025 51.912 116.025
115.719c0 63.808-52.048 115.72-116.025 115.72z"""
            ] []
        , Svg.path
            [ SA.d """m309.495
154.8-26.191-4.197-12.107-23.574c-2.939-5.724-8.762-9.28-15.196-9.28s-12.257 3.556-15.196
9.279l-12.107 23.575-26.191 4.197c-6.366 1.021-11.557 5.464-13.546 11.596s-.396 12.776 4.158
17.339l18.691 18.728-4.089 26.125c-.996 6.362 1.62 12.667 6.827 16.456 2.988 2.174 6.501 3.281
10.04 3.281 2.628 0 5.269-.611 7.718-1.849l23.694-11.978 23.695 11.979c5.747 2.905 12.55 2.356
17.758-1.432 5.207-3.789 7.823-10.094 6.827-16.456l-4.089-26.126 18.69-18.728c4.555-4.563
6.148-11.208 4.159-17.34-1.989-6.131-7.179-10.575-13.545-11.595zm-27.046 37.129c-2.254 2.258-3.295
5.458-2.802 8.61l3.995 25.521-23.13-11.693c-1.418-.717-2.965-1.076-4.512-1.076-1.546
0-3.093.358-4.512 1.076l-23.13 11.693 3.995-25.521c.493-3.153-.548-6.352-2.802-8.61l-18.248-18.284
25.559-4.096c3.143-.504 5.859-2.474 7.313-5.306l11.825-23.026 11.825 23.026c1.454 2.832 4.17 4.802
7.313 5.306l25.559 4.096z"""
            ] []
        ]

svgKill : Html msg
svgKill =
    Svg.svg
        [ SA.height "512"
        , SA.width "512"
        , SA.viewBox "0 0 512.12 512.12"
        ]
    <| List.singleton
    <| Svg.g []
        [ Svg.path
            [ SA.d """m497.12
422.457h-14.874v-44.832c0-8.284-6.716-15-15-15h-37.041v-188.421c0-96.023-78.121-174.145-174.145-174.145s-174.145
78.121-174.145 174.145v188.421h-37.04c-8.284 0-15 6.716-15 15v44.832h-14.875c-8.284 0-15 6.716-15
15v59.603c0 8.284 6.716 15 15 15h482.12c8.284 0 15-6.716
15-15v-59.603c0-8.284-6.716-15-15-15zm-385.205-248.253c0-79.481 64.663-144.145
144.145-144.145s144.145 64.663 144.145 144.145v188.421h-288.29zm-52.04
218.421h392.371v29.832h-392.371zm422.245 89.435h-452.12v-29.603h452.12z"""
            ] []
        , Svg.path
            [ SA.d """m317.538 266.793h-122.956c-8.284 0-15 6.716-15 15s6.716 15 15
15h122.955c8.284 0 15-6.716 15-15s-6.715-15-14.999-15z"""
            ] []
        , Svg.path
            [ SA.d """m317.538 206.793h-122.956c-8.284 0-15 6.716-15 15s6.716 15 15
15h122.955c8.284 0 15-6.716 15-15 .001-8.284-6.715-15-14.999-15z"""
            ] []
        , Svg.path
            [ SA.d """m317.538 146.793h-122.956c-8.284 0-15 6.716-15 15s6.716 15 15
15h122.955c8.284 0 15-6.716 15-15 .001-8.284-6.715-15-14.999-15z"""
            ] []
        ]

svgGames : Html msg
svgGames =
    Svg.svg
        [ SA.viewBox "0 0 321.145 321.145"
        ]
    <| List.singleton
    <| Svg.g []
        [ Svg.path
            [ SA.d """M320.973,200.981c-0.8-18.4-4-38.8-8.8-58c-4.8-18.4-10.8-35.6-18-48.8c-28-49.2-58.4-41.6-94.8-32
c-11.6,2.8-24,6-36.8,7.2h-4c-12.8-1.2-25.2-4.4-36.8-7.2c-36.4-9.2-66.8-17.2-94.8,32.4c-7.2,13.2-13.6,30.4-18,48.8
c-4.8,19.2-8,39.6-8.8,58c-0.8,20.4,1.2,35.2,5.6,45.6c4.4,9.6,10.8,15.6,19.2,18c7.6,2,16.4,1.2,25.6-2.8
c15.6-6.4,33.6-20,51.2-36.4c12.4-12,35.6-18,58.8-18s46.4,6,58.8,18c17.6,16.4,35.6,30,51.2,36.4c9.2,3.6,18,4.8,25.6,2.8
c8-2.4,14.8-8,19.2-18.4C319.773,236.581,321.773,221.781,320.973,200.981z M301.773,240.981c-2.4,5.6-5.6,8.8-9.6,10
c-4.4,1.2-10,0.4-16.4-2c-14-5.6-30.4-18-46.4-33.2c-15.2-15.2-42-22.8-68.8-22.8s-53.6,7.6-69.2,22c-16.4,15.2-32.8,28-46.4,33.2
c-6.4,2.4-12,3.6-16.4,2c-4-1.2-7.2-4.4-9.6-10c-3.2-7.6-4.8-20-4-38.4c0.8-17.2,3.6-36.8,8.4-55.2c4.4-17.2,10-33.2,16.8-45.2
c22-39.6,47.6-33.2,78-25.2c12.4,3.2,25.2,6.4,39.2,7.6c0.4,0,0.4,0,0.8,0h4.4c0.4,0,0.4,0,0.8,0c14.4-1.2,27.2-4.4,39.6-7.6
c30.4-7.6,56-14.4,78,25.2c6.8,12,12.4,27.6,16.8,45.2c4.4,18.4,7.6,37.6,8.4,55.2
C306.973,220.181,305.373,232.581,301.773,240.981z"""
            ] []
        , Svg.path
            [ SA.d """M123.773,122.981c-4-3.6-8.8-6.4-14.4-6.8c-0.4-5.2-2.8-10.4-6.4-14l-0.4-0.4c-4.4-4.4-10-6.8-16.4-6.8
c-6.4,0-12.4,2.8-16.4,6.8c-3.6,3.6-6.4,8.8-6.8,14.4c-5.6,0.4-10.4,2.8-14.4,6.4l-0.4,0.4c-4.4,4.4-6.8,10-6.8,16.4
c0,6.4,2.8,12.4,6.8,16.4c4,4,8.8,6.4,14.8,6.8c0.4,5.6,2.8,10.8,6.8,14.4c4.4,4.4,10,6.8,16.4,6.8c6.4,0,12.4-2.8,16.4-6.8
c3.6-4,6.4-8.8,6.8-14.4c5.6-0.4,10.8-2.8,14.4-6.8c4.4-4.4,6.8-10,6.8-16.4C130.573,132.981,127.773,126.981,123.773,122.981z
    M113.773,145.381c-1.6,1.6-3.6,2.4-6,2.4h-5.6c-4,0-7.6,3.2-7.6,7.6v5.2c0,2.4-0.8,4.4-2.4,6c-1.6,1.6-3.6,2.4-6,2.4
c-2.4,0-4.4-0.8-6-2.4c-1.6-1.6-2.4-3.6-2.4-6v-5.6c0-4-3.2-7.6-7.6-7.6h-5.6c-2.4,0-4.4-0.8-6-2.4c-1.2-1.2-2.4-3.2-2.4-5.6
c0-2.4,0.8-4.4,2.4-6c0,0,0,0,0.4-0.4c1.6-1.2,3.6-2,5.6-2h5.6c4,0,7.6-3.2,7.6-7.6v-5.6c0-2.4,0.8-4.4,2.4-6
c1.6-1.6,3.6-2.4,6-2.4c2.4,0,4.4,0.8,6,2.4c0,0,0,0,0.4,0.4c1.2,1.6,2,3.6,2,5.6v5.6c0,4,3.2,7.6,7.6,7.6h5.6
c2.4,0,4.4,0.8,6,2.4c1.6,1.6,2.4,3.6,2.4,6C116.173,141.781,115.373,143.781,113.773,145.381z"""
            ] []
        , Svg.circle
            [ SA.cx "230.173"
            , SA.cy "110.981"
            , SA.r "14"
            ] []
        , Svg.circle
            [ SA.cx "230.173"
            , SA.cy "167.781"
            , SA.r "14"
            ] []
        , Svg.circle
            [ SA.cx "201.773"
            , SA.cy "139.381"
            , SA.r "14"
            ] []
        , Svg.circle
            [ SA.cx "258.573"
            , SA.cy "139.381"
            , SA.r "14"
            ] []
        ]

svgLeader : Html msg
svgLeader =
    Svg.svg
        [ SA.height "644"
        , SA.width "644"
        , SA.viewBox "-20 -23 644.00054 644"
        ]
    <| List.singleton
    <| Svg.g []
        [ Svg.path
            [ SA.d """m129.78125 159.578125c33.078125 0 59.898438-26.8125 59.898438-59.898437
0-33.078126-26.820313-59.898438-59.898438-59.898438s-59.898438 26.820312-59.898438
59.898438c.035157 33.070312 26.828126 59.871093 59.898438 59.898437zm0-99.828125c22.050781 0
39.933594 17.878906 39.933594 39.929688 0 22.058593-17.882813 39.933593-39.933594
39.933593-22.054688 0-39.933594-17.875-39.933594-39.933593 0-22.050782 17.878906-39.929688
39.933594-39.929688zm0 0"""
            ] []
        , Svg.path
            [ SA.d """m119.796875 409.15625h19.96875v189.679688h-19.96875zm0 0"""
            ] []
        , Svg.path
            [ SA.d """m603.75
69.730469h-114.578125v-39.933594h-149.746094v-4.988281c0-13.78125-11.171875-24.960938-24.957031-24.960938s-24.957031
11.179688-24.957031 24.960938v154.738281h-219.628907c-38.578124.042969-69.8398432
31.308594-69.882812 69.878906v99.832031c.0351562 27.558594 22.359375 49.886719 49.914062
49.917969h9.984376v199.660157h19.964843v-359.390626h-19.964843v139.761719h-9.984376c-16.539062
0-29.949218-13.402343-29.949218-29.949219v-99.832031c.035156-27.550781 22.359375-49.878906
49.917968-49.914062h269.542969v9.984375c0 16.542968-13.410156 29.949218-29.949219
29.949218h-129.78125v359.390626h19.96875v-339.425782h89.847657v339.425782h49.914062v-349.667969c12.523438-9.347657
19.921875-24.039063
19.96875-39.671875v-29.949219h-19.96875v-29.949219h99.832031v39.929688h164.492188l-23.957031-59.898438zm-284.289062
509.140625h-9.984376v-319.460938c3.355469.003906 6.699219-.324218
9.984376-.992187zm0-399.324219h-9.984376v-154.738281c0-2.753906 2.234376-4.992188
4.992188-4.992188s4.992188 2.238282 4.992188
4.992188zm19.964843-129.78125h129.78125v79.863281h-129.78125zm234.832031
119.796875h-115.035156v-19.964844h29.949219v-59.898437h85.085937l-15.972656 39.929687zm0 0"""
            ] []
        ]