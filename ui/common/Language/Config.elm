module Language.Config exposing (..)

import Language exposing (Language)
import Language.Info exposing (LanguageInfo, ThemeKey, ThemeRawKey, toThemeKey)
import Dict exposing (Dict)
import Network exposing (NetworkRequest(..))

type LangConfig = LangConfig (() -> LangConfigRaw)

type alias LangConfigRaw =
    { lang: String
    , info: LanguageInfo
    , root: Dict String Language
    , themes: Dict ThemeKey Language
    }

unwrap : LangConfig -> LangConfigRaw
unwrap (LangConfig config) = config ()

wrap : LangConfigRaw -> LangConfig
wrap config = LangConfig <| \() -> config

init : String -> LangConfig
init selLang = wrap
        { lang = selLang
        , info =
            { languages = Dict.empty
            , icons = Dict.empty
            , modes = Dict.empty
            }
        , root = Dict.empty
        , themes = Dict.empty
        }

getRootLang : LangConfig -> Language
getRootLang lang =
    let
        config = unwrap lang
    in
        Dict.get config.lang config.root
        |> Maybe.withDefault Language.LanguageUnknown

getLang : LangConfig -> Maybe ThemeRawKey -> Language
getLang langConfig key =
    let
        config = unwrap langConfig
    in Language.alternate
        (Language.getLanguage config.themes
            <| Maybe.map
                (\x -> toThemeKey x config.lang)
            <| key
        )
        <| getRootLang langConfig

setCurrent : String -> Maybe ThemeRawKey -> LangConfig -> (LangConfig, List NetworkRequest)
setCurrent lang theme langConfig =
    let
        config = unwrap langConfig
    in
        Tuple.mapFirst wrap <|
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
                        (\x -> toThemeKey x lang)
                    <| theme
                ]

verifyHasTheme : ThemeRawKey -> LangConfig -> List NetworkRequest
verifyHasTheme theme langConfig =
    let
        config = unwrap langConfig
    in
        toThemeKey theme config.lang
        |> \key ->
            if Dict.member key config.themes
            then []
            else [ GetLang key ]

setInfo : LanguageInfo -> LangConfig -> LangConfig
setInfo info langConfig =
    let
        config = unwrap langConfig
    in wrap { config | info = info }

setRoot : String -> Language -> LangConfig -> LangConfig
setRoot key lang langConfig =
    let
        config = unwrap langConfig
    in wrap { config | root = Dict.insert key lang config.root }

setTheme : ThemeKey -> Language -> LangConfig -> LangConfig
setTheme key lang langConfig =
    let
        config = unwrap langConfig
    in wrap { config | themes = Dict.insert key lang config.themes }
