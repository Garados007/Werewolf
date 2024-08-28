module Views.ViewGamePhase exposing (..)

import Data
import Network exposing (Request(..), SocketRequest(..), NetworkRequest(..))

import Html exposing (Html, div, text)
import Html.Attributes as HA exposing (class)
import Html.Events as HE
import Dict
import Html exposing (option)
import Time exposing (Posix)
import Language exposing (Language)
import Maybe.Extra

type Msg
    = Noop
    | Send Request

view : Language -> Posix -> Data.Game -> Data.GamePhase -> Bool -> String -> Html Msg
view lang now game phase isLeader myId =
    let
        viewVoting : Data.GameVoting -> Html Msg
        viewVoting voting =
            div [ class "voting-box" ]
                [ div [ class "voting-header" ]
                    [ div [ class "title" ]
                        <| List.singleton
                        <| text
                        <|  (\value ->
                                case value of
                                    Just x -> x
                                    Nothing ->
                                        "theme.voting." ++ voting.langId ++ ".title"
                            )
                        <| Maybe.Extra.orElseLazy
                            (\() ->
                                Language.getText lang
                                    [ "theme", "voting", "default", "title" ]
                            )
                        <| Language.getText lang
                            [ "theme", "voting", voting.langId, "title" ]
                    , div [ class "status" ]
                        [ div
                            [ HA.classList
                                [ ("started-state", True)
                                , ("started", voting.started)
                                ]
                            ]
                            <| List.singleton
                            <| text
                            <| Language.getTextOrPath lang
                                [ "game"
                                , "voting"
                                , if voting.started
                                    then "started"
                                    else "not-started"
                                ]
                        , div
                            [ HA.classList
                                [ ("can-vote-state", True)
                                , ("can-vote", voting.canVote)
                                ]
                            ]
                            <| List.singleton
                            <| text
                            <| Language.getTextOrPath lang
                                [ "game"
                                , "voting"
                                , if voting.canVote
                                    then "can-vote"
                                    else "cannot-vote"
                                ]
                        ]
                    ]
                , div [ class "voting-options" ]
                    <| List.map
                        (\(oid, option) ->
                            div
                                [ HA.classList
                                    [ ("voting-option", True)
                                    , ("button", True)
                                    , Tuple.pair "voted"
                                        <| List.member myId option.user
                                    ]
                                , HA.title <|
                                    if List.isEmpty option.user
                                    then Language.getTextOrPath lang
                                        [ "game", "voting", "nobody-has-voted" ]
                                    else Language.getTextFormatOrPath lang
                                        [ "game", "voting", "has-voted-list" ]
                                        <| Dict.fromList
                                        <| List.singleton
                                        <| Tuple.pair "list"
                                        <| String.concat
                                        <| List.intersperse ", "
                                        <| List.map
                                            (\uid ->
                                                case Dict.get uid game.user of
                                                    Just { user } -> user.name
                                                    Nothing -> uid
                                            )
                                        <| option.user
                                , HE.onClick
                                    <| if voting.started && voting.canVote
                                        then Send <| SockReq <| Vote voting.id oid
                                        else Noop
                                ]
                                [ div
                                    [ class "bar"
                                    , HA.style "width"
                                        <|  ( String.fromFloat
                                                <| 100 *
                                                    (toFloat <| List.length option.user) /
                                                    (toFloat voting.maxVoter)
                                            )
                                        ++ "%"
                                    ]
                                    []
                                , Html.span []
                                    <| List.singleton
                                    <| text
                                    <| Maybe.withDefault option.langId
                                    <| Maybe.Extra.orElseLazy
                                        (\() ->
                                            Just <| Language.getTextFormatOrPath lang
                                                [ "theme", "voting", "default", "options", option.langId ]
                                                option.vars
                                        )
                                    <| Language.getTextFormat lang
                                        [ "theme", "voting", voting.langId, "options", option.langId ]
                                        option.vars
                                ]
                        )
                    <| Dict.toList voting.options
                ,   (\list ->
                        if List.isEmpty list
                        then text ""
                        else div [ class "voting-controls" ] list
                    )
                    <| List.filterMap identity
                    [ if isLeader && not game.leaderIsPlayer
                        then Just <|
                            if voting.started
                            then div
                                [ class "button"
                                , HE.onClick
                                    <| Send
                                    <| SockReq
                                    <| VotingFinish voting.id
                                ]
                                [ text
                                    <| Language.getTextOrPath lang
                                        [ "game", "voting", "button", "end" ]
                                ]
                            else div
                                [ class "button"
                                , HE.onClick
                                    <| Send
                                    <| SockReq
                                    <| VotingStart voting.id
                                ]
                                [ text
                                    <| Language.getTextOrPath lang
                                        [ "game", "voting", "button", "start" ]
                                ]
                        else Nothing
                    , if voting.started && (isLeader || voting.canVote)
                        then Maybe.andThen
                            (\time ->
                                let
                                    seconds : Int
                                    seconds = max 0
                                        <| (\t -> t // 1000)
                                        <| (Time.posixToMillis time) - (Time.posixToMillis now)

                                    missingPlayer : Int
                                    missingPlayer = (-) voting.maxVoter
                                        <| List.sum
                                        <| List.map (.user >> List.length)
                                        <| Dict.values voting.options

                                in  if seconds <= 30
                                    then Just <| div
                                        [ class "button"
                                        , HE.onClick
                                            <| Send
                                            <| SockReq
                                            <| VotingWait voting.id
                                        , HA.title <| Language.getTextFormatOrPath lang
                                            [ "game", "voting", "button", "add-time-hint" ]
                                            <| Dict.fromList
                                            [ Tuple.pair "time-left"
                                                <| String.fromInt seconds
                                            , Tuple.pair "max-time"
                                                <| String.fromInt
                                                <| missingPlayer * 45
                                            ]
                                        ]
                                        [ text <| Language.getTextFormatOrPath lang
                                            [ "game", "voting", "button", "add-time" ]
                                            <| Dict.fromList
                                            <| List.singleton
                                            <| Tuple.pair "time-left"
                                            <| String.fromInt seconds
                                        ]
                                    else Nothing
                            )
                            voting.timeout
                        else Nothing
                    ]
                ]

        viewSequence : Data.SequenceInfo -> Html Msg
        viewSequence sequence =
            div [ class "sequence-box" ]
                [ div [ class "marker" ] []
                , div [ class "sequence-step" ]
                    [ text <| String.concat
                        [ "("
                        , String.fromInt sequence.stepIndex
                        , " / "
                        , String.fromInt sequence.stepMax
                        , ")"
                        ]
                    ]
                , div [ class "sequence-name-box" ]
                    [ div [ class "sequence-name" ]
                        <| List.singleton
                        <| text
                        <| Language.getTextOrPath lang
                            [ "theme"
                            , "sequence"
                            , sequence.name
                            , "name"
                            ]
                    , div [ class "sequence-stepname" ]
                        <| List.singleton
                        <| text
                        <| Language.getTextOrPath lang
                        <| case sequence.stepName of
                            Just name -> [ "theme", "sequence", sequence.name, "step", name ]
                            Nothing -> [ "theme", "sequence", sequence.name, "init" ]
                    ]
                ]

    in div [ class "phase-container" ]
        [ div
            [ HA.classList
                [ ("phase-sequences", True)
                , ("auto-skip", game.autoSkip)
                ]
            ]
            <| List.map viewSequence game.sequences
        , div [ class "phase-votings" ]
            <| List.map viewVoting phase.voting
        ]
