module Model exposing
    ( Model
    , Modal (..)
    , applyResponse
    , applyEventData
    , getLanguage
    , getSelectedLanguage
    , init
    )

import Data
import EventData exposing (EventData)
import Dict exposing (Dict)
import Network exposing (NetworkResponse(..))
import Browser.Navigation exposing (Key)
import Time exposing (Posix)
import Level exposing (Level)
import Language exposing (Language, LanguageInfo)
import Styles exposing (Styles)

import Views.ViewThemeEditor

type alias Model =
    { game: Maybe Data.GameUserResult
    , roles: Maybe Data.RoleTemplates
    , errors: List String
    , token: String
    , key: Key
    , now: Posix
    , modal: Modal
    -- local editor
    , editor: Dict String Int
    -- buffer
    , oldBufferedConfig: (Posix, Data.UserConfig)
    , bufferedConfig: Data.UserConfig
    , levels: Dict String Level
    , langInfo: LanguageInfo
    , rootLang: Dict String Language
    , themeLangs: Dict Language.ThemeKey Language
    , events: List (Bool,String)
    , styles: Styles
    , chats: List Data.ChatMessage
    , chatView: Maybe String
    }

type Modal
    = NoModal
    | SettingsModal Views.ViewThemeEditor.Model
    | WinnerModal Data.Game (List String)
    | PlayerNotification (Dict String (List String))
    | RoleInfo String

init : String -> Key -> Model
init token key =
    { game = Nothing
    , roles = Nothing
    , errors = []
    , token = token
    , key = key
    , now = Time.millisToPosix 0
    , modal = NoModal
    , editor = Dict.empty
    , oldBufferedConfig = Tuple.pair
        (Time.millisToPosix 0)
        { theme = ""
        , background = ""
        , language = ""
        }
    , bufferedConfig =
        { theme = ""
        , background = ""
        , language = ""
        }
    , levels = Dict.empty
    , langInfo =
        { languages = Dict.empty
        , icons = Dict.empty
        , themes = Dict.empty
        }
    , rootLang = Dict.empty
    , themeLangs = Dict.empty
    , events = []
    , styles = Styles.init
    , chats = []
    , chatView = Nothing
    }

getSelectedLanguage : Data.GameUserResult -> String
getSelectedLanguage gameResult =
    gameResult.userConfig
        |> Maybe.map .language
        |> Maybe.andThen
            (\key ->
                if key == ""
                then Nothing
                else Just key
            )
        |> Maybe.withDefault "de"

getLanguage : Model -> Language
getLanguage model =
    let
        lang : Maybe String
        lang = model.game
            |> Maybe.map getSelectedLanguage

        rootLang : Language
        rootLang =
            Language.getLanguage model.rootLang lang
        
        themeLang : Language
        themeLang =
            Language.getLanguage 
                model.themeLangs
            <| Maybe.andThen
                (\x -> Maybe.map (Language.toThemeKey x) lang)
            <| Maybe.map .theme
            <| Maybe.andThen .game
            <| model.game
    in Language.alternate themeLang rootLang
    
applyResponse : NetworkResponse -> Model -> (Model, List Network.NetworkRequest)
applyResponse response model =
    case response of
        RespRoles roles ->
            Tuple.pair
                { model
                | roles = Just roles
                }
                []
        RespGame game ->
            Tuple.pair
                { model
                | game = Just game
                , oldBufferedConfig = Tuple.pair model.now <|
                    if model.bufferedConfig.theme == ""
                    then Maybe.withDefault model.bufferedConfig game.userConfig
                    else model.bufferedConfig
                , bufferedConfig = game.userConfig
                    |> Maybe.withDefault model.bufferedConfig
                , modal =
                    let
                        get : Maybe Data.GameUserResult -> Maybe (Data.Game, List String)
                        get =
                            Maybe.andThen .game
                            >> Maybe.andThen
                                (\game_ ->
                                    Maybe.map 
                                        (Tuple.pair game_)
                                        game_.winner
                                )
                    in case (get model.game, get <| Just game) of
                        (Nothing, Just (game_, list)) -> WinnerModal game_ list
                        _ -> model.modal
                , levels = 
                    case game.game of
                        Just game_ ->
                            Dict.merge
                            (\_ _ dict -> dict)
                            (\key a b dict ->
                                Dict.insert 
                                    key
                                    (Level.updateData model.now b a)
                                    dict
                            )
                            (\key b dict ->
                                Dict.insert
                                    key
                                    (Level.init model.now b)
                                    dict
                            )
                            model.levels
                            ( game_.user
                                |> Dict.map (\_ -> .level)
                            )
                            Dict.empty
                        Nothing -> Dict.empty
                }
            <| case Maybe.map .language game.userConfig of
                Nothing -> []
                Just l ->
                    List.filterMap identity
                        [ if Dict.member l model.rootLang
                            then Nothing
                            else Just <| Network.GetRootLang l
                        , Maybe.andThen
                            (\key ->
                                if Dict.member key model.themeLangs
                                then Nothing
                                else Just <| Network.GetLang key
                            )
                            <| Maybe.map
                                (\x -> Language.toThemeKey x l)
                            <| Maybe.map .theme
                            <| game.game
                        ]
        RespError error ->
            Tuple.pair
                { model
                | errors = 
                    if List.member error model.errors
                    then model.errors
                    else error :: model.errors
                }
                []
        RespNoError -> (model, [])
        RespLangInfo info ->
            Tuple.pair
                { model
                | langInfo = info
                -- , theme = 
                --     Maybe.map
                --         (\(v1, v2, _) -> (v1, v2))
                --     <| Language.firstTheme info
                }
            <|  ( case Maybe.map Network.GetLang <| Language.firstTheme info of
                    Just x -> (::) x
                    Nothing -> identity
                )
            <| List.map Network.GetRootLang
            <| Dict.keys info.languages
        RespRootLang lang info ->
            Tuple.pair
                { model
                | rootLang = Dict.insert lang info model.rootLang
                }
                []
        RespLang key info ->
            Tuple.pair
                { model
                | themeLangs = Dict.insert key info model.themeLangs
                }
                []

editGame : Model -> (Data.Game -> Data.Game) -> Maybe Data.GameUserResult
editGame model editFunc =
    Maybe.map
        (\gameResult ->
            { gameResult
            | game = Maybe.map editFunc gameResult.game
            }
        )
        model.game

applyEventData : EventData -> Model -> (Model, List Network.NetworkRequest)
applyEventData event model =
    case event of
        EventData.AddParticipant id user -> Tuple.pair 
            { model
            | game = editGame model <| \game ->
                { game
                | user = Dict.insert id user game.user
                , participants = Dict.insert id Nothing game.participants
                }
            }
            []
        EventData.AddVoting voting -> Tuple.pair 
            { model
            | game = editGame model <| \game ->
                { game
                | phase =
                    Maybe.withDefault 
                        (Data.GamePhase "" 
                            (Data.GameStage "" "" "")
                            []
                        ) 
                        game.phase 
                    |> \phase -> Just
                        { phase
                        | voting = 
                            if List.isEmpty
                                <| List.filter
                                    (\v -> v.id == voting.id)
                                <| phase.voting
                            then phase.voting ++ [ voting ]
                            else phase.voting
                        }
                }
            }
            []
        EventData.ChatEvent chat -> Tuple.pair
            { model
            | chats = 
                (::)
                    { chat
                    | time = model.now
                    , shown = model.chatView /= Nothing
                    }
                <| List.filter
                    (\chat_ ->
                        (Time.posixToMillis model.now) - (Time.posixToMillis chat_.time)
                            < 1000 * 60 * 10
                    )
                <| List.take 30
                <| model.chats
            }
            []
        EventData.GameEnd winner -> Tuple.pair
            { model
            | game = editGame model <| \game ->
                { game
                | winner = winner
                , phase = Nothing
                }
            , modal = case winner of
                Just list -> 
                    Maybe.withDefault model.modal
                    <| Maybe.andThen
                        (Maybe.map
                            (\game -> WinnerModal game list)
                        << .game
                        )
                        model.game
                Nothing -> model.modal
            }
            []
        EventData.GameStart newParticipant -> Tuple.pair
            { model
            | game = editGame model <| \game ->
                { game
                | phase = game.phase
                    |> Maybe.withDefault
                        (Data.GamePhase "" 
                            (Data.GameStage "" "" "")
                            []
                        ) 
                    |> Just
                , participants = newParticipant
                , winner = Nothing
                }
            }
            []
        EventData.MultiPlayerNotification notifications -> Tuple.pair
            { model
            | modal = case model.modal of
                PlayerNotification oldDict ->
                    PlayerNotification
                        <| Dict.merge
                            Dict.insert
                            (\k v1 v2 d ->
                                Dict.insert
                                    k
                                    (v1 ++ v2)
                                    d
                            )
                            Dict.insert
                            oldDict
                            notifications
                            Dict.empty
                _ -> PlayerNotification notifications
            }
            []
        EventData.NextPhase nextPhase -> Tuple.pair
            { model
            | game = editGame model <| \game ->
                { game
                | phase = case (nextPhase, game.phase) of
                    (Nothing, _) -> Nothing
                    (Just key, Just oldPhase) -> Just
                        { oldPhase
                        | langId = key
                        }
                    (Just key, Nothing) -> Just
                        <| Data.GamePhase key
                            (Data.GameStage "" "" "")
                            []
                }
            }
            []
        EventData.OnLeaderChanged newLeader -> Tuple.pair
            { model
            | game = editGame model <| \game ->
                { game
                | leader = newLeader
                }
            }
            []
        EventData.OnRoleInfoChanged Nothing _ -> (model, [])
        EventData.OnRoleInfoChanged (Just id) role -> Tuple.pair
            { model
            | game = editGame model <| \game ->
                { game
                | participants = Dict.insert id (Just role) game.participants
                }
            }
            []
        EventData.PlayerNotification nid player -> Tuple.pair
            { model
            | modal = PlayerNotification
                <| case model.modal of
                    PlayerNotification oldDict ->
                        Dict.insert nid
                            ((Dict.get nid oldDict |> Maybe.withDefault [])
                                ++ player
                            )
                            oldDict
                    _ -> Dict.singleton nid player
            }
            []
        EventData.RemoveParticipant id -> Tuple.pair 
            { model
            | game = editGame model <| \game ->
                { game
                | user = Dict.remove id game.user
                , participants = Dict.remove id game.participants
                }
            }
            []
        EventData.RemoveVoting id -> Tuple.pair 
            { model
            | game = editGame model <| \game ->
                { game
                | phase = 
                    Maybe.withDefault
                        (Data.GamePhase "" 
                            (Data.GameStage "" "" "")
                            []
                        ) 
                        game.phase 
                    |> \phase -> Just
                        { phase
                        | voting = List.filter
                            (\v -> v.id /= id)
                            phase.voting
                        }
                }
            }
            []
        EventData.SendStage stage -> Tuple.pair
            { model
            | game = editGame model <| \game ->
                { game
                | phase = case game.phase of
                    Just oldPhase -> Just
                        { oldPhase
                        | stage = stage
                        }
                    Nothing -> Just
                        <| Data.GamePhase "" 
                            stage
                            []
                }
            }
            []
        EventData.SetGameConfig newConfig -> Tuple.pair 
            { model
            | game = editGame model <| \game ->
                { game
                | config = newConfig.config
                , participants =
                    (\participants ->
                        if newConfig.leaderIsPlayer == game.leaderIsPlayer
                        then participants
                        else if newConfig.leaderIsPlayer
                        then Dict.insert game.leader Nothing participants
                        else Dict.remove game.leader participants
                    )
                    <| if Tuple.first game.theme == Tuple.first newConfig.theme
                        then game.participants
                        else game.participants
                            |> Dict.map
                                (always <| always Nothing)
                , leaderIsPlayer = newConfig.leaderIsPlayer
                , deadCanSeeAllRoles = newConfig.deadCanSeeAllRoles
                , allCanSeeRoleOfDead = newConfig.allCanSeeRoleOfDead
                , autostartVotings = newConfig.autostartVotings
                , autofinishVotings = newConfig.autofinishVotings
                , votingTimeout = newConfig.votingTimeout
                , autofinishRound = newConfig.autofinishRound
                , theme = newConfig.theme
                }
            }
            <| Maybe.withDefault []
            <| Maybe.map
                (\game ->
                    case Maybe.map .language game.userConfig of
                        Nothing -> []
                        Just l ->
                            List.filterMap identity
                                [ if Dict.member l model.rootLang
                                    then Nothing
                                    else Just <| Network.GetRootLang l
                                , Maybe.andThen
                                    (\key ->
                                        if Dict.member key model.themeLangs
                                        then Nothing
                                        else Just <| Network.GetLang key
                                    )
                                    <| Maybe.map
                                        (\x -> Language.toThemeKey x l)
                                    <| Just newConfig.theme
                                ]
                )
            <| model.game
        EventData.SetUserConfig newConfig -> Tuple.pair
            { model
            | game = Maybe.map
                (\gameResult ->
                    { gameResult
                    | userConfig = Just newConfig
                    }
                )
                model.game
            , oldBufferedConfig = Tuple.pair model.now <|
                if model.bufferedConfig.theme == ""
                then newConfig
                else model.bufferedConfig
            , bufferedConfig = newConfig
            }
            <| 
                if Dict.member newConfig.language model.rootLang
                then []
                else List.filterMap identity
                    [ Just <| Network.GetRootLang newConfig.language
                    , Maybe.map Network.GetLang
                        <| Maybe.map
                            (\x -> Language.toThemeKey x newConfig.language)
                        <| Maybe.map .theme
                        <| Maybe.andThen .game
                        <| model.game
                    ]
        EventData.SetVotingTimeout vid timeout -> Tuple.pair 
            { model
            | game = editGame model <| \game ->
                { game
                | phase = 
                    Maybe.withDefault
                        (Data.GamePhase "" 
                            (Data.GameStage "" "" "")
                            []
                        ) 
                        game.phase 
                    |> \phase -> Just
                        { phase
                        | voting = List.map
                            (\v ->
                                if v.id == vid
                                then { v | timeout = timeout}
                                else v
                            )
                            phase.voting
                        }
                }
            }
            []
        EventData.SetVotingVote vid oid voter -> Tuple.pair 
            { model
            | game = editGame model <| \game ->
                { game
                | phase = 
                    Maybe.withDefault
                        (Data.GamePhase "" 
                            (Data.GameStage "" "" "")
                            []
                        ) 
                        game.phase 
                    |> \phase -> Just
                        { phase
                        | voting = List.map
                            (\v ->
                                if v.id == vid
                                then
                                    { v
                                    | options = Dict.map
                                        (\key value ->
                                            if key == oid
                                            then 
                                                { value
                                                | user = value.user ++ [ voter ]
                                                }
                                            else value
                                        )
                                        v.options
                                    }
                                else v
                            )
                            phase.voting
                        }
                }
            }
            []