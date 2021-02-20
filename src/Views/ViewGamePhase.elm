module Views.ViewGamePhase exposing (..)

import Data
import Network exposing (NetworkRequest(..))

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
    | Send NetworkRequest

view : Language -> Posix -> String -> Data.Game -> Data.GamePhase -> Bool -> String -> Html Msg
view lang now token game phase isLeader myId =
    let
        viewPhaseHeader : Html Msg
        viewPhaseHeader =
            div [ class "phase-header" ]
                [ div [ class "title" ]
                    <| List.singleton
                    <| text
                    <| Language.getTextOrPath lang
                        [ "theme", "phases", phase.langId ]
                ]

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
                                        "theme.voting-title." ++ voting.langId
                            )
                        <| Maybe.Extra.orElseLazy
                            (\() ->
                                Language.getText lang
                                    [ "theme", "voting-title", "default" ]
                            )
                        <| Language.getText lang
                            [ "theme", "voting-title", voting.langId ]
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
                                                    Just user -> user.name
                                                    Nothing -> uid
                                            )
                                        <| option.user
                                , HE.onClick
                                    <| if voting.started && voting.canVote
                                        then Send <| GetVote token voting.id oid
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
                                , Html.span [] [ text option.name ]
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
                                    <| GetVotingFinish token voting.id
                                ]
                                [ text
                                    <| Language.getTextOrPath lang
                                        [ "game", "voting", "button", "end" ]
                                ]
                            else div 
                                [ class "button" 
                                , HE.onClick 
                                    <| Send
                                    <| GetVotingStart token voting.id
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
                                            <| GetVotingWait token voting.id
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

        viewPhaseControls : () -> Html Msg
        viewPhaseControls () =
            div [ class "phase-controls" ]
                [ div 
                    [ class "button" 
                    , HE.onClick <| Send <| GetGameStop token
                    ]
                    [ text <| Language.getTextOrPath lang
                        [ "game", "phase", "end" ]
                    ]
                , div
                    [ class "button"
                    , HE.onClick <| Send <| GetGameNext token
                    ]
                    [ text <| Language.getTextOrPath lang
                        [ "game", "phase", "next" ]
                    ]
                ]

    in div [ class "phase-container" ]
        [ viewPhaseHeader
        , div [ class "phase-votings" ]
            <| List.map viewVoting phase.voting
        , if isLeader
            then viewPhaseControls ()
            else text ""
        ]