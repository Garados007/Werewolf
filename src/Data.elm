module Data exposing
    ( ChatMessage
    , Error
    , UserInfo
    , Game
    , GameParticipant
    , GamePhase
    , GameUser
    , GameUserResult
    , GameUserStats
    , GameStage
    , GameVoting
    , GameVotingOption
    , LobbyJoinToken
    , OnlineInfo
    , RoleTemplates
    , UserConfig
    , decodeChatMessage
    , decodeError
    , decodeGameUserResult
    , decodeRoleTemplates
    )

import Dict exposing (Dict)
import Json.Decode as JD exposing (Decoder)
import Json.Decode.Pipeline exposing (required)
import Time exposing (Posix)
import Iso8601
import Level exposing (LevelData)
import Json.Decode.Pipeline exposing (hardcoded)

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

type alias GameUserResult =
    { game: Maybe Game
    , user: Maybe String
    , userConfig: Maybe UserConfig
    }

type alias Game =
    { leader: String
    , phase: Maybe GamePhase
    , participants: Dict String (Maybe GameParticipant)
    , user: Dict String GameUser
    , online: Dict String OnlineInfo
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
    }

type alias GameUser =
    { name: String
    , img: String
    , isGuest: Bool
    -- , isOnline: Bool
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
    , language: String
    }

decodeGameUserResult : Decoder GameUserResult
decodeGameUserResult =
    JD.succeed GameUserResult
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
                -- |> required "participants"
                --     (JD.succeed GameParticipant
                --         |> required "tags" (JD.list JD.string)
                --         |> required "role" (JD.nullable JD.string)
                --         |> JD.nullable
                --         |> JD.dict
                --     )
                -- |> hardcoded Dict.empty
                |> required "users"
                    (JD.succeed GameParticipant
                        |> required "tags" (JD.list JD.string)
                        |> required "role" (JD.nullable JD.string)
                        |> JD.nullable
                        |> JD.field "role"
                        |> JD.dict
                    )
                |> required "users"
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
                        |> JD.field "user"
                        |> JD.dict
                    )
                |> required "users"
                    (JD.succeed OnlineInfo
                        |> required "is-online" JD.bool
                        |> required "online-counter" JD.int
                        |> required "last-online-change" Iso8601.decoder
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
                |> JD.nullable
            )
        |> required "user" (JD.nullable JD.string)
        |> required "user-config"
            ( JD.succeed UserConfig
                |> required "theme" JD.string
                |> required "background" JD.string
                |> required "language" JD.string
                |> JD.nullable
            )

type alias Error = Maybe String

decodeError : Decoder (Maybe String)
decodeError =
    JD.field "error"
        <| JD.nullable
        <| JD.string

type alias ChatMessage =
    { time: Posix
    , sender: String
    , phase: Maybe String
    , message: String
    , canSend: Bool
    , shown: Bool
    }

decodeChatMessage : Decoder ChatMessage
decodeChatMessage =
    JD.succeed ChatMessage
        |> Json.Decode.Pipeline.hardcoded (Time.millisToPosix 0)
        |> required "sender" JD.string
        |> required "phase" (JD.nullable JD.string)
        |> required "message" JD.string
        |> required "can-send" JD.bool
        |> Json.Decode.Pipeline.hardcoded False
