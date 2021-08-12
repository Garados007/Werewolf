module Network exposing
    ( EditGameConfig
    , EditUserConfig
    , NetworkRequest (..)
    , NetworkResponse (..)
    , editGameConfig
    , editUserConfig
    , executeRequest
    )

import Data
import Http
import Dict exposing (Dict)
import Url
import Language exposing (Language, LanguageInfo)

type NetworkRequest
    = GetRoles
    | GetLangInfo
    | GetRootLang String
    | GetLang Language.ThemeKey
    | GetGame String
    | PostGameConfig String EditGameConfig
    | PostUserConfig String EditUserConfig
    | GetGameStart String
    | GetVotingStart String String
    | GetVote String String String
    | GetVotingWait String String
    | GetVotingFinish String String
    | GetGameNext String
    | GetGameStop String
    | GetUserKick String String
    | PostChat String (Maybe String) String

type NetworkResponse
    = RespRoles Data.RoleTemplates
    | RespGame Data.GameUserResult
    | RespError String
    | RespNoError
    | RespLangInfo LanguageInfo
    | RespRootLang String Language
    | RespLang Language.ThemeKey Language

mapRespError : Cmd (Response Data.Error) -> Cmd (Response NetworkResponse)
mapRespError =
    Cmd.map
        (Result.map
            ( Maybe.map RespError
                >> Maybe.withDefault RespNoError
            )
        )

executeRequest : NetworkRequest -> Cmd NetworkResponse
executeRequest request =
    Cmd.map
        (\result ->
            case result of
                Ok d -> d
                Err (Http.BadUrl url) -> 
                    RespError <| "Http Error: Bad Url " ++ url
                Err Http.Timeout ->
                    RespError <| "Http Error: Timeout"
                Err Http.NetworkError ->
                    RespError <| "Http Error: Network Error"
                Err (Http.BadStatus status) ->
                    RespError <| "Http Error: Bad Status " ++ String.fromInt status
                Err (Http.BadBody msg) ->
                    RespError <| "Http Error: Bad Body: " ++ msg
        )
    <| case request of
        GetRoles -> getRoles
            |> Cmd.map (Result.map RespRoles)
        GetLangInfo -> getLangInfo
            |> Cmd.map (Result.map RespLangInfo)
        GetRootLang lang -> getRootLang lang
            |> Cmd.map (Result.map <| RespRootLang lang)
        GetLang key -> getLang key
            |> Cmd.map (Result.map <| RespLang key)
        GetGame token -> getGame token
            |> Cmd.map (Result.map RespGame)
        PostGameConfig token config -> postGameConfig token config
            |> mapRespError
        PostUserConfig token config -> postUserConfig token config
            |> mapRespError
        GetGameStart token -> getGameStart token
            |> mapRespError
        GetVotingStart token vid -> getVotingStart token vid
            |> mapRespError
        GetVote token vid id -> getVote token vid id
            |> mapRespError
        GetVotingWait token vid -> getVotingWait token vid
            |> mapRespError
        GetVotingFinish token vid -> getVotingFinish token vid
            |> mapRespError
        GetGameNext token -> getGameNext token
            |> mapRespError
        GetGameStop token -> getGameStop token
            |> mapRespError
        GetUserKick token user -> getUserKick token user
            |> mapRespError
        PostChat token phase message -> postChat token phase message
            |> mapRespError

type alias Response a = Result Http.Error a

getRoles : Cmd (Response Data.RoleTemplates)
getRoles =
    Http.get
        { url = "/api/roles"
        , expect = Http.expectJson identity Data.decodeRoleTemplates
        }

getGame : String -> Cmd (Response Data.GameUserResult)
getGame token =
    Http.get
        { url = "/api/game/" ++ token
        , expect = Http.expectJson identity Data.decodeGameUserResult
        }

editGameConfig : EditGameConfig
editGameConfig =
    { newLeader = Nothing
    , newConfig = Nothing
    , leaderIsPlayer = Nothing
    , newDeadCanSeeAllRoles = Nothing
    , newAllCanSeeRoleOfDead = Nothing
    , autostartVotings = Nothing
    , autofinishVotings = Nothing
    , votingTimeout = Nothing
    , autofinishRound = Nothing
    , theme = Nothing
    }

type alias EditGameConfig =
    { newLeader: Maybe String
    , newConfig: Maybe (Dict String Int)
    , leaderIsPlayer: Maybe Bool
    , newDeadCanSeeAllRoles: Maybe Bool
    , newAllCanSeeRoleOfDead: Maybe Bool
    , autostartVotings: Maybe Bool
    , autofinishVotings: Maybe Bool
    , votingTimeout: Maybe Bool
    , autofinishRound: Maybe Bool
    , theme: Maybe (String, String)
    }

convertEditGameConfig : EditGameConfig -> String
convertEditGameConfig config =
    [ Maybe.map
        (\leader -> "leader=" ++ leader
        )
        config.newLeader
    , Maybe.map
        (\conf -> 
            Dict.toList conf
            |> List.map
                (\(key, value) ->
                    key ++ ":" ++ String.fromInt value
                )
            |> List.intersperse ","
            |> (::) "config="
            |> String.concat
        )
        config.newConfig
    , Maybe.map
        (\new -> "leader-is-player=" ++
            if new then "true" else "false"
        )
        config.leaderIsPlayer
    , Maybe.map
        (\new -> "dead-can-see-all-roles=" ++
            if new then "true" else "false"
        )
        config.newDeadCanSeeAllRoles
    , Maybe.map
        (\new -> "all-can-see-role-of-dead=" ++
            if new then "true" else "false"        
        )
        config.newAllCanSeeRoleOfDead
    , Maybe.map
        (\new -> "autostart-votings=" ++
            if new then "true" else "false"        
        )
        config.autostartVotings
    , Maybe.map
        (\new -> "autofinish-votings=" ++
            if new then "true" else "false"
        )
        config.autofinishVotings
    , Maybe.map
        (\new -> "voting-timeout=" ++
            if new then "true" else "false"
        )
        config.votingTimeout
    , Maybe.map
        (\new -> "autofinish-rounds=" ++
            if new then "true" else "false"
        )
        config.autofinishRound
    , Maybe.map
        (\(new, _) -> "theme-impl=" ++ Url.percentEncode new)
        config.theme
    , Maybe.map
        (\(_, new) -> "theme-lang=" ++ Url.percentEncode new)
        config.theme
    ]
    |> List.filterMap identity
    |> List.intersperse "&"
    |> String.concat

postGameConfig : String -> EditGameConfig -> Cmd (Response Data.Error)
postGameConfig token config =
    Http.post
        { url = "/api/game/" ++ token ++ "/config"
        , body = Http.stringBody "application/x-www-form-urlencoded"
            <| convertEditGameConfig config
        , expect = Http.expectJson identity Data.decodeError
        }

editUserConfig : EditUserConfig
editUserConfig =
    { newTheme = Nothing
    , newBackground = Nothing
    , newLanguage = Nothing
    }

type alias EditUserConfig =
    { newTheme: Maybe String
    , newBackground: Maybe String
    , newLanguage: Maybe String
    }

convertEditUserConfig : EditUserConfig -> String
convertEditUserConfig config =
    [ Maybe.map
        (\theme -> "theme=" ++ theme)
        config.newTheme
    , Maybe.map
        (\background -> "background=" ++ Url.percentEncode background)
        config.newBackground
    , Maybe.map
        (\language -> "language=" ++ language)
        config.newLanguage
    ]
    |> List.filterMap identity
    |> List.intersperse "&"
    |> String.concat

postUserConfig : String -> EditUserConfig -> Cmd (Response Data.Error)
postUserConfig token config =
    Http.post
        { url = "/api/game/" ++ token ++ "/user/config"
        , body = Http.stringBody "application/x-www-form-urlencoded"
            <| convertEditUserConfig config
        , expect = Http.expectJson identity Data.decodeError
        }

getErrorReq : String -> Cmd (Response Data.Error)
getErrorReq url = 
    Http.get
        { url = url
        , expect = Http.expectJson identity Data.decodeError
        }

getGameStart : String -> Cmd (Response Data.Error)
getGameStart token =
    getErrorReq <| "/api/game/" ++ token ++ "/start"

getVotingStart : String -> String -> Cmd (Response Data.Error)
getVotingStart token vid =
    getErrorReq 
        <| "/api/game/" ++ token ++ "/voting/" 
        ++ vid ++ "/start"

getVote : String -> String -> String -> Cmd (Response Data.Error)
getVote token vid id =
    getErrorReq
        <| "/api/game/" ++ token ++ "/voting/" 
        ++ vid ++ "/vote/" ++ id
        
getVotingWait : String -> String -> Cmd (Response Data.Error)
getVotingWait token vid =
    getErrorReq
        <| "/api/game/" ++ token ++ "/voting/" 
        ++ vid ++ "/wait"

getVotingFinish : String -> String -> Cmd (Response Data.Error)
getVotingFinish token vid =
    getErrorReq
        <| "/api/game/" ++ token ++ "/voting/" 
        ++ vid ++ "/finish"

getGameNext : String -> Cmd (Response Data.Error)
getGameNext token =
    getErrorReq <| "/api/game/" ++ token ++ "/next"

getGameStop : String -> Cmd (Response Data.Error)
getGameStop token =
    getErrorReq <| "/api/game/" ++ token ++ "/stop"

getUserKick : String -> String -> Cmd (Response Data.Error)
getUserKick token uid =
    getErrorReq <| "/api/game/" ++ token ++ "/kick/" ++ uid

getLangInfo : Cmd (Response LanguageInfo)
getLangInfo =
    Http.get
        { url = "/content/games/werwolf/lang/index.json"
        , expect = Http.expectJson identity Language.decodeLanguageInfo
        }

getRootLang : String -> Cmd (Response Language)
getRootLang lang =
    Http.get
        { url = "/content/games/werwolf/lang/root/" ++ lang ++ ".json"
        , expect = Http.expectJson identity Language.decodeLanguage
        }

getLang : Language.ThemeKey -> Cmd (Response Language)
getLang (k1, k2, k3) =
    Http.get
        { url = "/content/games/werwolf/lang/" ++ k1 ++ "/" ++ k2 ++
            "/" ++ k3 ++ ".json"
        , expect = Http.expectJson identity Language.decodeLanguage
        }

postChat : String -> Maybe String -> String -> Cmd (Response Data.Error)
postChat token phase message =
    Http.post
        { url = (++) ("/api/game/" ++ token ++ "/" ++ "/chat")
            <| Maybe.withDefault ""
            <| Maybe.map ((++) "?phase=" << Url.percentEncode) phase
        , body = Http.stringBody "application/x-www-form-urlencoded"
            <| (++) "message="
            <| Url.percentEncode message
        , expect = Http.expectJson identity Data.decodeError
        }
