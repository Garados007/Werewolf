module Language.ServiceRenderer exposing
    ( render
    , renderText
    , TextToken(..)
    , renderTextTokens
    , renderEpic
    )

import Parser exposing ((|.), (|=), Parser)
import Html exposing (Html, text, span)
import Html.Attributes as HA exposing (class)
import Data
import Language exposing (Language)
import Dict exposing (Dict)

type TextToken
    = TokenRaw String
    | TokenVariable String

parser : Parser (List TextToken)
parser =
    Parser.loop [] parserHelp

parserHelp : List TextToken -> Parser (Parser.Step (List TextToken) (List TextToken))
parserHelp revToken =
    Parser.oneOf
        [ Parser.succeed (\token -> Parser.Loop (token :: revToken))
            |= parserSingle
        , Parser.succeed ()
            |> Parser.map (\_ -> Parser.Done (List.reverse revToken))
        ]

parserSingle : Parser TextToken
parserSingle =
    Parser.oneOf
        [ Parser.map TokenRaw
            <| Parser.getChompedString
            <| Parser.succeed ()
            |. Parser.chompIf ((/=) '{')
            |. Parser.chompUntilEndOr "{"
        , Parser.succeed TokenVariable
            |. Parser.symbol "{"
            |= (Parser.getChompedString
                    <| Parser.succeed ()
                    |. Parser.chompUntil "}"
                )
            |. Parser.symbol "}"
        ]

rawTextKey : Data.ChatServiceMessage -> List String
rawTextKey message = [ "theme", "logs", message.key ]

render : Language -> Data.Game -> Dict String Data.GameUser -> Data.ChatServiceMessage -> Html msg
render language game removedUser message =
    case Language.getText language (rawTextKey message) of
        Nothing -> renderDefault message
        Just rawText -> renderFormat language game removedUser message rawText

renderEpic : Language -> Data.Game -> Dict String Data.GameUser -> Data.ChatServiceMessage -> Html msg
renderEpic language game removedUser message =
    case Language.getText language (rawTextKey message) of
        Nothing -> render language game removedUser message
        Just rawText -> renderFormat language game removedUser message rawText

renderFormat : Language -> Data.Game -> Dict String Data.GameUser -> Data.ChatServiceMessage -> String -> Html msg
renderFormat language game removedUser message formatString =
    case renderText language game removedUser message.args formatString of
        Ok html -> html
        Err err ->
            span [ class "parse-error" ]
            <| List.singleton
            <| text
            <| String.concat
                [ "Language Parse Error [ "
                , rawTextKey message
                    |> List.intersperse ", "
                    |> String.concat
                , " ]: "
                , err
                ]

renderText : Language -> Data.Game -> Dict String Data.GameUser -> Dict String Data.TextVariable -> String -> Result String (Html msg)
renderText language game removedUser messageArgs formatString =
    case Parser.run parser formatString of
        Err deadEnd -> Err <| Parser.deadEndsToString deadEnd
        Ok tokens -> Ok <| renderTextTokens language game removedUser messageArgs tokens

renderTextTokens : Language -> Data.Game -> Dict String Data.GameUser -> Dict String Data.TextVariable -> List TextToken -> Html msg
renderTextTokens language game removedUser messageArgs tokens =
    span [ class "special-content" ]
    <| List.map
        (\token ->
            case token of
                TokenRaw x ->
                    span [ class "raw" ] [ text x ]
                TokenVariable x ->
                    renderTokenVariable language game removedUser messageArgs x
        )
    <| tokens

renderTokenVariable : Language -> Data.Game -> Dict String Data.GameUser -> Dict String Data.TextVariable -> String -> Html msg
renderTokenVariable language game removedUser messageArgs token =
    case Dict.get token messageArgs of
        Nothing ->
            span [ class "variable", class "not-found" ]
                [ text <| "{" ++ token ++ "}" ]
        Just (Data.TextVarPlain x) ->
            span [ class "variable", class "plain" ]
                [ text x ]
        Just (Data.TextVarUser id as var) ->
            span
                [ class "variable", class "user", class "highlight"
                , HA.title <| "#" ++ id
                ]
            <| List.singleton
            <| text
            <| case Dict.get id game.user of
                Just entry -> "[" ++ entry.user.name ++ "]"
                Nothing -> case Dict.get id removedUser of
                    Just entry -> "[" ++ entry.name ++ "]"
                    Nothing -> renderDefaultVariable var
        Just (Data.TextVarVoting id as var) ->
            span [ class "variable", class "voting", class "highlight" ]
            <| List.singleton
            <| text
            <| case getVotingTitle language id of
                Just t -> "[" ++ t ++ "]"
                Nothing -> renderDefaultVariable var
        Just (Data.TextVarPhase id as var) ->
            span [ class "variable", class "phase", class "highlight" ]
            <| List.singleton
            <| text
            <| case Language.getText language [ "theme", "phases", id ] of
                Just t -> "[" ++ t ++ "]"
                Nothing -> renderDefaultVariable var
        Just (Data.TextVarVotingOption voting option args as var) ->
            span
                [ class "variable", class "voting", class "highlight"
                , HA.title
                    <| case Dict.get "player-id" args of
                        Just id -> "#" ++ id
                        Nothing -> ""
                ]
            <| List.singleton
            <| text
            <| case getVotingOptionText language voting option args of
                Just t -> "[" ++ t ++ "]"
                Nothing -> renderDefaultVariable var

getVotingTitle : Language -> String -> Maybe String
getVotingTitle language id =
    case Language.getText language [ "theme", "voting", id, "title" ] of
        Just x -> Just x
        Nothing -> Language.getText language [ "theme", "voting", "default", "title-logs" ]

getVotingOptionText : Language -> String -> String -> Dict String String -> Maybe String
getVotingOptionText language voting option args =
    case Language.getTextFormat language [ "theme", "voting", voting, "options", option ] args of
        Just x -> Just x
        Nothing -> Language.getTextFormat
            language
            [ "theme", "voting", "default", "options", option ]
            args

renderDefault : Data.ChatServiceMessage -> Html msg
renderDefault message =
    span [ class "default" ]
        <| List.singleton
        <| text
        <| String.concat
        [ "#"
        , message.key
        , ": {"
        , String.concat
            <| List.intersperse ", "
            <| List.map
                (\(key, var) -> key ++ "=" ++ renderDefaultVariable var
                )
            <| Dict.toList message.args
        , "}"
        ]

renderDefaultVariable : Data.TextVariable -> String
renderDefaultVariable var =
    case var of
        Data.TextVarPlain x -> x
        Data.TextVarUser x -> "<#" ++ x ++ ">"
        Data.TextVarVoting x -> "<" ++ x ++ ">"
        Data.TextVarPhase x -> "<" ++ x ++ ">"
        Data.TextVarVotingOption x1 x2 d -> "<" ++ x1 ++ ":" ++ x2 ++ "> {" ++
            ( Dict.toList d
                |> List.map (\(k, v) -> k ++ "=" ++ v)
                |> List.intersperse ", "
                |> String.concat
            ) ++ "}"
