module Network exposing
    ( EditGameConfig
    , EditUserConfig
    , NetworkRequest (..)
    , NetworkResponse (..)
    , SocketRequest (..)
    , SocketClose
    , Request (..)
    , editGameConfig
    , editUserConfig
    , executeRequest
    , getLangInfo
    , getRootLang
    , wsSend
    , execute
    , versionUrl
    )

import Http
import Dict exposing (Dict)
import Language exposing (Language, LanguageInfo)
import Json.Encode as JE
import WebSocket
import EventData exposing (EventData(..))
import Ports exposing (..)
import Config
import Url

wsSend : SocketRequest -> Cmd msg
wsSend request =
    WebSocket.send sendSocketCommand
        <| WebSocket.Send
            { name = "wss"
            , content =
                JE.encode 0
                <| JE.object
                <| case request of
                    FetchRoles ->
                        [ ("$type", JE.string "fetch-roles")
                        ]
                    SetGameConfig conf ->
                        List.filterMap identity
                        [ Just ("$type", JE.string "SetGameConfig")
                        , Maybe.map
                            (\x -> ("leader", JE.string x))
                            conf.newLeader
                        , Maybe.map
                            (Tuple.pair "config"
                                << JE.object
                                << List.map
                                    (Tuple.mapSecond JE.int)
                                << Dict.toList
                            )
                            conf.newConfig
                        , Maybe.map
                            (\x -> ("leader-is-player", JE.bool x))
                            conf.leaderIsPlayer
                        , Maybe.map
                            (\x -> ("dead-can-see-all-roles", JE.bool x))
                            conf.newDeadCanSeeAllRoles
                        , Maybe.map
                            (\x -> ("all-can-see-role-of-dead", JE.bool x))
                            conf.newAllCanSeeRoleOfDead
                        , Maybe.map
                            (\x -> ("autostart-votings", JE.bool x))
                            conf.autostartVotings
                        , Maybe.map
                            (\x -> ("autofinish-votings", JE.bool x))
                            conf.autofinishVotings
                        , Maybe.map
                            (\x -> ("voting-timeout", JE.bool x))
                            conf.votingTimeout
                        , Maybe.map
                            (\x -> ("autofinish-rounds", JE.bool x))
                            conf.autofinishRound
                        , Maybe.map
                            (\(x, _) -> ("theme-impl", JE.string x))
                            conf.theme
                        , Maybe.map
                            (\(_, x) -> ("theme-lang", JE.string x))
                            conf.theme
                        ]
                    SetUserConfig conf ->
                        List.filterMap identity
                        [ Just ("$type", JE.string "SetUserConfig")
                        , Maybe.map
                            (\x -> ("theme", JE.string x))
                            conf.newTheme
                        , Maybe.map
                            (\x -> ("background", JE.string x))
                            conf.newBackground
                        ]
                    GameStart ->
                        [ ("$type", JE.string "GameStart") ]
                    GameNext ->
                        [ ("$type", JE.string "GameNext") ]
                    GameStop ->
                        [ ("$type", JE.string "GameStop") ]
                    VotingStart vid ->
                        [ ("$type", JE.string "VotingStart")
                        , ("vid", JE.string vid)
                        ]
                    Vote vid id ->
                        [ ("$type", JE.string "Vote")
                        , ("vid", JE.string vid)
                        , ("id", JE.string id)
                        ]
                    VotingWait vid ->
                        [ ("$type", JE.string "VotingWait")
                        , ("vid", JE.string vid)
                        ]
                    VotingFinish vid ->
                        [ ("$type", JE.string "VotingFinish")
                        , ("vid", JE.string vid)
                        ]
                    KickUser user ->
                        [ ("$type", JE.string "KickUser")
                        , ("user", JE.string user)
                        ]
                    Message phase content ->
                        List.filterMap identity
                        [ Just ("$type", JE.string "Message")
                        , Maybe.map
                            (\x -> ("phase", JE.string x))
                            phase
                        , Just ("message", JE.string content)
                        ]
                    RefetchJoinToken ->
                        [ ("$type", JE.string "RefetchJoinToken") ]
            }

execute : (NetworkResponse -> msg) -> Request -> Cmd msg
execute tagger request =
    case request of
        SockReq req -> wsSend req
        NetReq req -> executeRequest req
            |> Cmd.map tagger

type alias SocketClose =
    { code: Int
    , reason: String
    }

type Request
    = SockReq SocketRequest
    | NetReq NetworkRequest

type SocketRequest
    = FetchRoles
    | SetGameConfig EditGameConfig
    | SetUserConfig EditUserConfig
    | GameStart
    | GameNext
    | GameStop
    | VotingStart String
    | Vote String String
    | VotingWait String
    | VotingFinish String
    | KickUser String
    | Message (Maybe String) String
    | RefetchJoinToken

type NetworkRequest
    = GetRootLang String
    | GetLang Language.ThemeKey

type NetworkResponse
    = RespError String
    | RespRootLang String Language
    | RespLang Language.ThemeKey Language

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
        GetRootLang lang -> getRootLang lang
            |> Cmd.map (Result.map <| RespRootLang lang)
        GetLang key -> getLang key
            |> Cmd.map (Result.map <| RespLang key)

type alias Response a = Result Http.Error a

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

editUserConfig : EditUserConfig
editUserConfig =
    { newTheme = Nothing
    , newBackground = Nothing
    }

type alias EditUserConfig =
    { newTheme: Maybe String
    , newBackground: Maybe String
    }

versionUrl : String -> String
versionUrl url =
    if Config.version == "debug"
    then url
    else url ++ "?_v=" ++ Url.percentEncode Config.version

getLangInfo : Cmd (Response LanguageInfo)
getLangInfo =
    Http.get
        { url = versionUrl "/content/lang/index.json"
        , expect = Http.expectJson identity Language.decodeLanguageInfo
        }

getRootLang : String -> Cmd (Response Language)
getRootLang lang =
    Http.get
        { url = versionUrl <| "/content/lang/root/" ++ lang ++ ".json"
        , expect = Http.expectJson identity Language.decodeLanguage
        }

getLang : Language.ThemeKey -> Cmd (Response Language)
getLang (k1, k2, k3) =
    Http.get
        { url = versionUrl <| "/content/lang/" ++ k1 ++ "/" ++ k2 ++
            "/" ++ k3 ++ ".json"
        , expect = Http.expectJson identity Language.decodeLanguage
        }
