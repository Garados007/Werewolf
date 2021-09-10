module Language.Config exposing (..)

import Language exposing (Language, LanguageInfo, ThemeKey,ThemeRawKey)
import Dict exposing (Dict)
import Network exposing (NetworkRequest(..))

type alias LangConfig =
    { lang: String
    , info: LanguageInfo
    , root: Dict String Language
    , themes: Dict ThemeKey Language
    }

init : String -> LangConfig
init selLang =
    { lang = selLang
    , info =
        { languages = Dict.empty
        , icons = Dict.empty
        , themes = Dict.empty
        }
    , root = Dict.empty
    , themes = Dict.empty
    }

getRootLang : LangConfig -> Language
getRootLang lang =
    Dict.get lang.lang lang.root
    |> Maybe.withDefault Language.LanguageUnknown

getLang : LangConfig -> Maybe ThemeRawKey -> Language
getLang lang key =
    Language.alternate
        (Language.getLanguage lang.themes
            <| Maybe.map
                (\x -> Language.toThemeKey x lang.lang)
            <| key
        ) 
        <| getRootLang lang

setCurrent : String -> Maybe ThemeRawKey -> LangConfig -> (LangConfig, List NetworkRequest)
setCurrent lang theme config =
    if lang == config.lang
    then (config, [])
    else Tuple.pair
        { config | lang = lang }
        <| List.filterMap identity
            [ if Dict.member lang config.root
                then Nothing
                else Just <| GetRootLang lang
            , Maybe.andThen
                (\key ->
                    if Dict.member key config.themes
                    then Nothing
                    else Just <| GetLang key
                )
                <| Maybe.map
                    (\x -> Language.toThemeKey x lang)
                <| theme
            ]

verifyHasTheme : ThemeRawKey -> LangConfig -> List NetworkRequest
verifyHasTheme theme config =
    Language.toThemeKey theme config.lang
    |> \key ->
        if Dict.member key config.themes
        then []
        else [ GetLang key ]

setInfo : LanguageInfo -> LangConfig -> LangConfig
setInfo info config =
    { config | info = info }

setRoot : String -> Language -> LangConfig -> LangConfig
setRoot key lang config =
    { config | root = Dict.insert key lang config.root }

setTheme : ThemeKey -> Language -> LangConfig -> LangConfig
setTheme key lang config =
    { config | themes = Dict.insert key lang config.themes }

