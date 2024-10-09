module Language exposing (..)

import Dict exposing (Dict)
import Json.Decode as JD exposing (Decoder)
import Maybe.Extra

type Language
    = LanguageText String
    | LanguageNode (Dict String Language)
    | LanguageUnknown
    | LanguageAlternate Language Language

alternate : Language -> Language -> Language
alternate = LanguageAlternate

empty : Language
empty = LanguageUnknown

getLanguage : Dict comparable Language -> Maybe comparable -> Language
getLanguage dict =
    Maybe.andThen
        (\k -> Dict.get k dict)
    >> Maybe.withDefault LanguageUnknown

getTextFormat : Language -> List String -> Dict String String -> Maybe String
getTextFormat language path vars =
    Maybe.map
        (\text ->
            Dict.foldl
                (\key ->
                    String.replace <| "{" ++ key ++ "}"
                )
                text
                vars
        )
    <| getText language path

getText : Language -> List String -> Maybe String
getText language path =
    case (language, path) of
        (LanguageText text, []) -> Just text
        (LanguageNode dict, first::other) ->
            Dict.get first dict
                |> Maybe.andThen
                    (\x -> getText x other)
        (LanguageAlternate l1 l2, _) ->
            Maybe.Extra.orLazy
                (getText l1 path)
                (\() -> getText l2 path)
        _ -> Nothing

getTextFormatOrPath : Language -> List String -> Dict String String -> String
getTextFormatOrPath language path vars =
    case getTextFormat language path vars of
        Just x -> x
        Nothing -> String.concat
            <| List.concat
            [ List.intersperse "." path
            , [ "(" ]
            , List.intersperse ", "
                <| List.map (\(key, value) -> key ++ ": " ++ value)
                <| Dict.toList vars
            , [ ")" ]
            ]

getTextOrPath : Language -> List String -> String
getTextOrPath language path =
    case getText language path of
        Just x -> x
        Nothing -> String.concat
            <| List.intersperse "." path

decodeLanguage : Decoder Language
decodeLanguage =
    JD.oneOf
        [ JD.map LanguageText JD.string
        , JD.map LanguageNode
            <| JD.dict
            <| JD.lazy
            <| \() -> decodeLanguage
        , JD.succeed LanguageUnknown
        ]
