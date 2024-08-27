module Data exposing
    ( ChatMessage
    , ChatEntry(..)
    , ChatLog
    , Error
    , UserInfo
    , Game
    , GameParticipant
    , GamePhase
    , GameUser
    , GameUserEntry
    , GameGlobalState
    , GameUserStats
    , GameStage
    , GameVoting
    , GameVotingOption
    , LobbyJoinToken
    , OnlineInfo
    , RoleTemplates
    , SequenceInfo
    , UserConfig
    , decodeChatMessage
    , decodeError
    , decodeGameGlobalState
    , decodeRoleTemplates
    , ChatServiceMessage
    , decodeChatServiceMessage
    , TextVariable (..)
    , decodeSequenceInfo
    , decodeTextVariable
    )

import Dict exposing (Dict)
import Json.Decode as JD exposing (Decoder)
import Json.Decode.Pipeline exposing (required)
import Time exposing (Posix)
import Iso8601
import Level exposing (LevelData)

type alias UserInfo =
    { username: String
    , picture: String
    }

type alias LobbyJoinToken =
    { token: String
    , aliveUntil: Posix
    }

type alias RoleTemplates = Dict String (List String)

decodeRoleTemplates : Decoder RoleTemplates
decodeRoleTemplates =
    JD.dict <| JD.list JD.string

type alias GameGlobalState =
    { game: Game
    , user: String
    , userConfig: UserConfig
    }

type alias Game =
    { leader: String
    , phase: Maybe GamePhase
    , user: Dict String GameUserEntry
    , winner: Maybe (List String)
    , config: Dict String Int
    , leaderIsPlayer: Bool
    , deadCanSeeAllRoles: Bool
    , allCanSeeRoleOfDead: Bool
    , autostartVotings: Bool
    , autofinishVotings: Bool
    , votingTimeout: Bool
    , autofinishRound: Bool
    , theme: (String, String)
    , sequences: List SequenceInfo
    , autoSkip: Bool
    }

type alias GameUserEntry =
    { role: Maybe GameParticipant
    , user: GameUser
    , online: OnlineInfo
    }

type alias OnlineInfo =
    { isOnline: Bool
    , counter: Int
    , lastChanged: Posix
    }

type alias GamePhase =
    { langId: String
    , stage: GameStage
    , voting: List GameVoting
    }

type alias GameStage =
    { langId: String
    , backgroundId: String
    , theme: String
    }

type alias GameVoting =
    { id: String
    , langId: String
    , started: Bool
    , canVote: Bool
    , maxVoter: Int
    , timeout: Maybe Posix
    , options: Dict String GameVotingOption
    }

type alias GameVotingOption =
    { langId: String
    , vars: Dict String String
    , user: List String
    }

type alias GameParticipant =
    { tags: List String
    , role: Maybe String
    , enabled: Bool
    }

type alias GameUser =
    { name: String
    , img: String
    , isGuest: Bool
    , stats: GameUserStats
    , level: LevelData
    }

type alias GameUserStats =
    { winGames: Int
    , killed: Int
    , looseGames: Int
    , leader: Int
    }

type alias UserConfig =
    { theme: String
    , background: String
    }

decodeGameGlobalState : Decoder GameGlobalState
decodeGameGlobalState =
    JD.succeed GameGlobalState
        |> required "game"
            (JD.succeed Game
                |> required "leader" JD.string
                |> required "phase"
                    (JD.succeed GamePhase
                        |> required "lang-id" JD.string
                        |> required "stage"
                            ( JD.succeed GameStage
                                |> required "lang-id" JD.string
                                |> required "background-id" JD.string
                                |> required "theme" JD.string
                            )
                        |> required "voting"
                            (JD.succeed GameVoting
                                |> required "id" JD.string
                                |> required "lang-id" JD.string
                                |> required "started" JD.bool
                                |> required "can-vote" JD.bool
                                |> required "max-voter" JD.int
                                |> required "timeout"
                                    (JD.nullable Iso8601.decoder)
                                |> required "options"
                                    (JD.succeed GameVotingOption
                                        |> required "lang-id" JD.string
                                        |> required "vars" (JD.dict JD.string)
                                        |> required "user" (JD.list JD.string)
                                        |> JD.dict
                                    )
                                |> JD.list
                            )
                        |> JD.nullable
                    )
                |> required "users"
                    (JD.succeed GameUserEntry
                        |> required "role"
                            (JD.succeed GameParticipant
                                |> required "tags" (JD.list JD.string)
                                |> required "role" (JD.nullable JD.string)
                                |> required "enabled" JD.bool
                                |> JD.nullable
                            )
                        |> required "user"
                            (JD.succeed GameUser
                                |> required "name" JD.string
                                |> required "img" JD.string
                                |> required "is-guest" JD.bool
                                |> required "stats"
                                    (JD.succeed GameUserStats
                                        |> required "win-games" JD.int
                                        |> required "killed" JD.int
                                        |> required "loose-games" JD.int
                                        |> required "leader" JD.int
                                    )
                                -- level
                                |> required "stats"
                                    (JD.succeed LevelData
                                        |> required "level" JD.int
                                        |> required "current-xp" JD.int
                                        |> required "max-xp" JD.int
                                    )
                            )
                        |> JD.andThen
                            (\x -> JD.succeed OnlineInfo
                                |> required "is-online" JD.bool
                                |> required "online-counter" JD.int
                                |> required "last-online-change" Iso8601.decoder
                                |> JD.map (\y -> x y)
                            )
                        |> JD.dict
                    )
                |> required "winner"
                    (JD.nullable <| JD.list <| JD.string)
                |> required "config" (JD.dict JD.int)
                |> required "leader-is-player" JD.bool
                |> required "dead-can-see-all-roles" JD.bool
                |> required "all-can-see-role-of-dead" JD.bool
                |> required "autostart-votings" JD.bool
                |> required "autofinish-votings" JD.bool
                |> required "voting-timeout" JD.bool
                |> required "autofinish-rounds" JD.bool
                |> required "theme"
                    (JD.map2 Tuple.pair
                        (JD.index 0 JD.string)
                        (JD.index 1 JD.string)
                    )
                |> required "sequences" (JD.list decodeSequenceInfo)
                |> required "auto-skip" JD.bool
            )
        |> required "user" JD.string
        |> required "user-config"
            ( JD.succeed UserConfig
                |> required "theme" JD.string
                |> required "background" JD.string
            )

type alias Error = Maybe String

decodeError : Decoder (Maybe String)
decodeError =
    JD.field "error"
        <| JD.nullable
        <| JD.string

type alias ChatLog =
    { time: Posix
    , shown: Bool
    , entry: ChatEntry
    }

type ChatEntry
    = ChatEntryMessage ChatMessage
    | ChatEntryService ChatServiceMessage

type alias ChatMessage =
    { sender: String
    , phase: Maybe String
    , message: String
    , canSend: Bool
    }

decodeChatMessage : Decoder ChatMessage
decodeChatMessage =
    JD.succeed ChatMessage
        |> required "sender" JD.string
        |> required "phase" (JD.nullable JD.string)
        |> required "message" JD.string
        |> required "can-send" JD.bool

type alias ChatServiceMessage =
    { key: String
    , epic: Bool
    , args: Dict String TextVariable
    }

decodeChatServiceMessage : Decoder ChatServiceMessage
decodeChatServiceMessage =
    JD.succeed ChatServiceMessage
    |> required "key" JD.string
    |> required "epic" JD.bool
    |> required "args" (JD.dict decodeTextVariable)

type TextVariable
    = TextVarPlain String
    | TextVarUser String
    | TextVarVoting String
    | TextVarPhase String
    | TextVarVotingOption String String (Dict String String)

decodeTextVariable : Decoder TextVariable
decodeTextVariable =
    JD.andThen
        (\type_ ->
            case type_ of
                "Plain" ->
                    JD.map TextVarPlain
                    <| JD.index 1 JD.string
                "User" ->
                    JD.map TextVarUser
                    <| JD.index 1 JD.string
                "Voting" ->
                    JD.map TextVarVoting
                    <| JD.index 1 JD.string
                "Phase" ->
                    JD.map TextVarPhase
                    <| JD.index 1 JD.string
                "VotingOption" ->
                    JD.map3 TextVarVotingOption
                        (JD.index 2 <| JD.index 0 JD.string)
                        (JD.index 1 JD.string)
                    <| JD.index 3
                    <| JD.dict JD.string
                _ -> JD.fail
                    <| "Unknown variable type " ++ type_
        )
    <| JD.index 0 JD.string

type alias SequenceInfo =
    { name: String
    , stepName: Maybe String
    , stepIndex: Int
    , stepMax: Int
    , target: Maybe String
    }

decodeSequenceInfo : Decoder SequenceInfo
decodeSequenceInfo =
    JD.succeed SequenceInfo
    |> required "name" JD.string
    |> required "step-name" (JD.nullable JD.string)
    |> required "step-index" JD.int
    |> required "step-max" JD.int
    |> Json.Decode.Pipeline.optional "target" (JD.maybe JD.string) Nothing
