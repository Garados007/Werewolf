module Model exposing
    ( Model
    , Modal (..)
    , EditorPage(..)
    , applyResponse
    , applyEventData
    , getLang
    , init
    )

import Data
import EventData exposing (EventData)
import Dict exposing (Dict)
import Network exposing (NetworkResponse(..))
import Time exposing (Posix)
import Level exposing (Level)
import Language
import Language.Config as LangConfig exposing (LangConfig)
import Styles exposing (Styles)
import Storage exposing (Storage)

import Views.ViewThemeEditor
import Set exposing (Set)

type alias Model =
    { state: Maybe Data.GameGlobalState
    , roles: Maybe Data.RoleTemplates
    , removedUser: Dict String Data.GameUser
    , errors: List String
    , token: String
    , now: Posix
    , modal: Modal
    -- local editor
    , editor: Dict String Int
    , editorPage: EditorPage
    -- buffer
    , oldBufferedConfig: (Posix, Data.UserConfig)
    , bufferedConfig: Data.UserConfig
    , levels: Dict String Level
    , lang: LangConfig
    , events: List (Bool,String)
    , styles: Styles
    , chats: List Data.ChatLog
    , chatView: Maybe String
    , joinToken: Maybe Data.LobbyJoinToken
    , codeCopied: Maybe Posix
    , streamerMode: Bool
    , closeReason: Maybe Network.SocketClose
    , maintenance: Maybe Posix
    , storage: Storage
    , missingImg: Set String
    }

type EditorPage
    = PageTheme
    | PageRole
    | PageOptions

type Modal
    = NoModal
    | SettingsModal Views.ViewThemeEditor.Model
    | WinnerModal Data.Game (List String)
    | PlayerNotification (Dict String (List String))
    | RoleInfo String
    | Maintenance (Maybe String)

init : String -> LangConfig -> Maybe Data.LobbyJoinToken 
    -> Storage -> Model
init token lang joinToken storage =
    { state = Nothing
    , roles = Nothing
    , removedUser = Dict.empty
    , errors = []
    , token = token
    , now = Time.millisToPosix 0
    , modal = NoModal
    , editor = Dict.empty
    , editorPage = PageTheme
    , oldBufferedConfig = Tuple.pair
        (Time.millisToPosix 0)
        { theme = ""
        , background = ""
        }
    , bufferedConfig =
        { theme = ""
        , background = ""
        }
    , levels = Dict.empty
    , lang = lang
    , events = []
    , styles = Styles.init
    , chats = []
    , chatView = Nothing
    , joinToken = joinToken
    , codeCopied = Nothing
    , streamerMode = Storage.get .streamerMode storage
        |> Maybe.withDefault False
    , closeReason = Nothing
    , maintenance = Nothing
    , storage = storage
    , missingImg = Set.empty
    }

getLang : Model -> Language.Language
getLang model =
    LangConfig.getLang
        model.lang
    <| Maybe.map
        (\state -> state.game.theme)
        model.state
    
applyResponse : NetworkResponse -> Model -> (Model, List Network.NetworkRequest)
applyResponse response model =
    case response of
        RespError error ->
            Tuple.pair
                { model
                | errors = 
                    if List.member error model.errors
                    then model.errors
                    else error :: model.errors
                }
                []
        RespRootLang lang info ->
            Tuple.pair
                { model
                | lang = LangConfig.setRoot lang info model.lang
                }
                []
        RespLang key info ->
            Tuple.pair
                { model
                | lang = LangConfig.setTheme key info model.lang
                }
                []

editGame : Model -> (Data.Game -> Data.Game) -> Maybe Data.GameGlobalState
editGame model editFunc =
    Maybe.map
        (\state ->
            { state
            | game = editFunc state.game
            }
        )
        model.state

applyEventData : EventData -> Model -> (Model, List Network.Request)
applyEventData event model =
    case event of
        EventData.SubmitRoles roles -> Tuple.pair
            { model
            | roles = Just roles
            }
            []
        EventData.Success -> (model, [])
        EventData.SendGameData state -> Tuple.pair
            { model
            | state = Just state
            , levels = Dict.merge
                -- remove old level entries that are no longer used
                (\_ _ -> identity)
                -- update existing level entries
                (\k old new -> Dict.insert k
                    <| Level.updateData model.now new old
                )
                -- add new level entries
                (\k -> Dict.insert k << Level.init model.now)
                model.levels
                (Dict.map
                    (\_ x -> x.user.level)
                    state.game.user
                )
                Dict.empty
            }
            [ Network.NetReq
                <| Network.GetLang
                <| Language.toThemeKey
                    state.game.theme
                    model.lang.lang
            ]
        EventData.AddParticipant id user -> Tuple.pair 
            { model
            | state = editGame model <| \game ->
                { game
                | user = Dict.insert id
                    { role = Nothing
                    , user = user
                    , online = Data.OnlineInfo
                        False
                        0
                        <| Time.millisToPosix 0
                    }
                    game.user
                }
            , levels = Dict.update id
                (\x -> Just <| case x of
                    Just level -> Level.updateData
                        model.now
                        user.level
                        level
                    Nothing -> Level.init model.now user.level
                )
                model.levels
            }
            []
        EventData.AddVoting voting -> Tuple.pair 
            { model
            | state = editGame model <| \game ->
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
                    { time = model.now
                    , shown = model.chatView /= Nothing
                    , entry = Data.ChatEntryMessage chat
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
            | state = editGame model <| \game ->
                { game
                | winner = winner
                , phase = Nothing
                }
            , modal = case winner of
                Just list -> 
                    Maybe.withDefault model.modal
                    <| Maybe.map
                        (\state -> WinnerModal state.game list)
                        model.state
                Nothing -> model.modal
            }
            []
        EventData.GameStart newParticipant -> Tuple.pair
            { model
            | state = editGame model <| \game ->
                { game
                | phase = game.phase
                    |> Maybe.withDefault
                        (Data.GamePhase "" 
                            (Data.GameStage "" "" "")
                            []
                        ) 
                    |> Just
                , user = Dict.merge
                    (\id a ->
                        Dict.insert id
                            { a | role = Nothing }
                    )
                    (\id a b -> 
                        Dict.insert id
                            { a | role = b }
                    )
                    (\_ _ -> identity)
                    game.user
                    newParticipant
                    Dict.empty
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
            | state = editGame model <| \game ->
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
            | state = editGame model <| \game ->
                { game
                | leader = newLeader
                }
            }
            []
        EventData.OnRoleInfoChanged Nothing _ -> (model, [])
        EventData.OnRoleInfoChanged (Just id) role -> Tuple.pair
            { model
            | state = editGame model <| \game ->
                { game
                | user = Dict.update id
                    (Maybe.map
                        <| \entry -> { entry | role = Just role }
                    )
                    game.user
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
            | state = editGame model <| \game ->
                { game
                | user = Dict.remove id game.user
                }
            , removedUser =
                case model.state
                    |> Maybe.map (.game >> .user)
                    |> Maybe.andThen
                        (Dict.get id)
                of
                    Just { user } ->
                        Dict.insert id user model.removedUser
                    Nothing -> model.removedUser
            , levels = Dict.remove id model.levels
            }
            []
        EventData.RemoveVoting id -> Tuple.pair 
            { model
            | state = editGame model <| \game ->
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
            | state = editGame model <| \game ->
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
            | state = editGame model <| \game ->
                { game
                | config = newConfig.config
                , user =
                    (\users ->
                        if newConfig.leaderIsPlayer == game.leaderIsPlayer
                        then users
                        else Dict.update game.leader
                            (Maybe.map
                                <| \user ->
                                    { user
                                    | role =
                                        if newConfig.leaderIsPlayer
                                        then Nothing
                                        else user.role
                                    }
                            )
                            users
                    )
                    <| if Tuple.first game.theme == Tuple.first newConfig.theme
                        then game.user
                        else Dict.map
                            (\_ entry -> { entry | role = Nothing })
                            game.user
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
            <| List.map Network.NetReq
            <| LangConfig.verifyHasTheme
                newConfig.theme
                model.lang
        EventData.SetUserConfig newConfig -> Tuple.pair
            { model
            | state = Maybe.map
                (\gameResult ->
                    { gameResult
                    | userConfig = newConfig
                    }
                )
                model.state
            , oldBufferedConfig = Tuple.pair model.now <|
                if model.bufferedConfig.theme == ""
                then newConfig
                else model.bufferedConfig
            , bufferedConfig = newConfig
            }
            []
        EventData.SetVotingTimeout vid timeout -> Tuple.pair 
            { model
            | state = editGame model <| \game ->
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
            | state = editGame model <| \game ->
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
        EventData.GetJoinToken token -> Tuple.pair
            { model 
            | joinToken = Just token
            }
            []
        EventData.OnlineNotification user online -> Tuple.pair
            { model
            | state = Maybe.map
                (\state ->
                    { state
                    | game = state.game |> \game ->
                        { game
                        | user = Dict.update
                            user
                            (Maybe.map
                                <| \entry ->
                                    { entry | online = online }
                            )
                            game.user
                        }
                    }
                )
                model.state
            }
            []
        EventData.Maintenance reason shutdown -> Tuple.pair
            { model
            | modal = Maintenance reason
            , maintenance = Just shutdown
            }
            []
        EventData.SendStats stats -> Tuple.pair
            { model
            | state = Maybe.map
                (\state ->
                    { state
                    | game = state.game |> \game ->
                        { game
                        | user = Dict.merge
                            Dict.insert
                            (\k gameUser (stat, level) ->
                                Dict.insert k
                                { gameUser
                                | user = gameUser.user |> \user ->
                                    { user
                                    | stats = stat
                                    , level = level
                                    }
                                }
                            )
                            (\_ _ -> identity)
                            game.user
                            stats
                            Dict.empty
                        }
                    }
                )
                model.state
            , levels = Dict.merge
                Dict.insert
                (\k old (_, new) ->
                    Dict.insert k
                        <| Level.updateData model.now new old
                )
                (\k (_, new) ->
                    Dict.insert k
                        <| Level.init model.now new
                )
                model.levels
                stats
                Dict.empty
            }
            []
        EventData.ChatServiceMessage msg -> Tuple.pair
            { model
            | chats = 
                (::)
                    { time = model.now
                    , shown = model.chatView /= Nothing
                    , entry = Data.ChatEntryService msg
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

