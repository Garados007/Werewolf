module Main.Tools exposing (..)
import Url exposing (Url)
import Dict
import Url.Parser.Query
import Http
import Browser.Navigation exposing (Key)
import Url.Parser

redirectUri : NavOpts -> Url
redirectUri nav =
    nav.url |> \url ->
    { url
    | path = "/"
    , query = Just <| navQuery nav
    }

queryBool : String -> Url.Parser.Query.Parser Bool
queryBool name =
    Url.Parser.Query.map
        (Maybe.withDefault False)
    <| Url.Parser.Query.enum name
    <| Dict.fromList
        [ ("true", True)
        , ("false", False)
        , ("1", True)
        , ("0", False)
        ]

formEncodedBody : List (String, String) -> Http.Body
formEncodedBody = 
    Http.stringBody "application/x-www-form-urlencoded"
        << String.concat
        << List.intersperse "&"
        << List.map
            (\(k, v) -> Url.percentEncode k ++ "=" ++ Url.percentEncode v
            )

type alias NavOpts =
    { url: Url
    , dev: Bool
    , fail: Bool
    , lang: String
    , langFallback: Maybe String
    , key: Key
    }

parseNavOpts : Url -> Key -> NavOpts
parseNavOpts url key =
    let
        getOpt : Url.Parser.Query.Parser (Maybe a) -> Maybe  a
        getOpt parser =
            Maybe.andThen identity
            <| Url.Parser.parse
                (Url.Parser.query parser)
                { url | path = "" }
        
        nav : NavOpts
        nav =
            { url = url
            , dev = queryBool "dev"
                |> Url.Parser.Query.map Just
                |> getOpt
                |> Maybe.withDefault False
            , fail = queryBool "fail"
                |> Url.Parser.Query.map Just
                |> getOpt
                |> Maybe.withDefault False
            , lang = Url.Parser.Query.string "lang"
                |> getOpt
                |> Maybe.withDefault "en"
            , langFallback = Url.Parser.Query.string "lang-fallback-for"
                |> getOpt
            , key = key
            }
    in  { nav
        | url = { url | query = Just <| navQuery nav }
        }
        

navQuery : NavOpts -> String
navQuery nav =
    String.concat
    <| List.intersperse "&"
    <| List.map
        (\(k, v) -> Url.percentEncode k ++ "=" ++ Url.percentEncode v)
    <| List.filterMap identity
        [ if nav.dev then Just ("dev", "true") else Nothing
        , if nav.fail then Just ("fail", "true") else Nothing
        , Just ("lang", nav.lang)
        , Maybe.map (Tuple.pair "lang-fallback-for") nav.langFallback
        ]

navigateTo : NavOpts -> String -> (NavOpts, Cmd msg)
navigateTo navOpts path =
    let
        url : Url
        url = navOpts.url |> \x ->
            { x 
            | path = path
            , query = Just <| navQuery navOpts
            }

        func : Key -> String -> Cmd msg
        func =
            if navOpts.url.path /= path
            then Browser.Navigation.pushUrl
            else Browser.Navigation.replaceUrl
    in Tuple.pair
            { navOpts | url = url }
        <| func navOpts.key
        <| Url.toString url

navFail : Bool -> NavOpts -> NavOpts
navFail fail nav = { nav | fail = fail }
