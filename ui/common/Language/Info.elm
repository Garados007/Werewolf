module Language.Info exposing (..)

import Dict exposing (Dict)
import Json.Decode as JD exposing (Decoder)
import Json.Decode.Pipeline exposing (required, optional)

type alias ThemeKey = (String, String, String)
type alias ThemeRawKey = (String, String)

toThemeKey : ThemeRawKey -> String -> ThemeKey
toThemeKey (k1, k2) k3 = (k1, k2, k3)

toThemeRawKey : ThemeKey -> ThemeRawKey
toThemeRawKey (k1, k2, _) = (k1, k2)

type alias LanguageInfo =
    { languages: Dict String String
    , icons: Dict String String
    , modes: Dict String LangModeInfo
    }

firstTheme : LanguageInfo -> Maybe ThemeKey
firstTheme info =
    Dict.foldl
        (\k1 { themes } r1 ->
            case r1 of
                Just r1_ -> Just r1_
                Nothing ->
                    Dict.foldl
                        (\k2 { title } r2 ->
                            case r2 of
                                Just r2_ -> Just r2_
                                Nothing ->
                                    Dict.foldl
                                        (\k3 _ ->
                                            Just << Maybe.withDefault
                                                (k1, k2, k3)
                                        )
                                        Nothing
                                        title
                        )
                        Nothing
                        themes
        )
        Nothing
        info.modes

getThemeName : LanguageInfo -> ThemeKey -> Maybe String
getThemeName info (impl, ui, lang) =
    info.modes
        |> Dict.get impl
        |> Maybe.andThen (.themes >> Dict.get ui)
        |> Maybe.andThen (.title >> Dict.get lang)

decodeLanguageInfo : Decoder LanguageInfo
decodeLanguageInfo =
    JD.succeed LanguageInfo
        |> required "languages" (JD.dict JD.string)
        |> required "icons" (JD.dict JD.string)
        |> required "modes" (JD.dict decodeLangModeInfo)

type alias LangModeInfo =
    { title: Dict String String
    , themes: Dict String LangThemeInfo
    }

decodeLangModeInfo : JD.Decoder LangModeInfo
decodeLangModeInfo =
    JD.succeed LangModeInfo
    |> required "title" (JD.dict JD.string)
    |> required "themes" (JD.dict decodeLangThemeInfo)

type alias LangThemeInfo =
    { title: Dict String String
    , default: Maybe String
    , enabled: Bool
    , ignoreCharacter: List String
    }

decodeLangThemeInfo : JD.Decoder LangThemeInfo
decodeLangThemeInfo =
    JD.succeed LangThemeInfo
    |> required "title" (JD.dict JD.string)
    |> optional "default" (JD.nullable JD.string) Nothing
    |> optional "enabled" JD.bool True
    |> optional "ignore_character" (JD.list JD.string) []
