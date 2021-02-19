module Data exposing
    ( Error
    , Game
    , GameParticipant
    , GamePhase
    , GameUser
    , GameUserResult
    , GameUserStats
    , GameVoting
    , GameVotingOption
    , RoleTemplates
    , UserConfig
    , decodeError
    , decodeGameUserResult
    , decodeRoleTemplates
    )

import Dict exposing (Dict)
import Json.Decode as JD exposing (Decoder)
import Json.Decode.Pipeline exposing (required)
import Json.Decode
import Time exposing (Posix)
import Iso8601
import Level exposing (LevelData)

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
    , running: Bool
    , phase: Maybe GamePhase
    , participants: Dict String (Maybe GameParticipant)
    , user: Dict String GameUser
    , winner: Maybe (List String)
    , config: Dict String Int
    , leaderIsPlayer: Bool
    , deadCanSeeAllRoles: Bool
    , autostartVotings: Bool
    , autofinishVotings: Bool
    , votingTimeout: Bool
    , autofinishRound: Bool
    }

type alias GamePhase =
    { langId: String
    , voting: List GameVoting
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
    { name: String
    , user: List String
    }

type alias GameParticipant =
    { alive: Bool
    , major: Bool
    , tags: List String
    , role: Maybe String
    }

type alias GameUser =
    { name: String
    , img: String
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

decodeGameUserResult : Decoder GameUserResult
decodeGameUserResult =
    JD.succeed GameUserResult
        |> required "game"
            (JD.succeed Game
                |> required "leader" JD.string
                |> required "running" JD.bool
                |> required "phase"
                    (JD.succeed GamePhase
                        |> required "lang-id" JD.string
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
                                        |> required "name" JD.string
                                        |> required "user" (JD.list JD.string)
                                        |> JD.dict
                                    )
                                |> JD.list
                            )
                        |> JD.nullable
                    )
                |> required "participants"
                    (JD.succeed GameParticipant
                        |> required "alive" JD.bool
                        |> required "major" JD.bool
                        |> required "tags" (JD.list JD.string)
                        |> required "role" (JD.nullable JD.string)
                        |> JD.nullable
                        |> JD.dict
                    )
                |> required "user"
                    (JD.succeed GameUser
                        |> required "name" JD.string
                        |> required "img" JD.string
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
                        |> JD.dict
                    )
                |> required "winner"
                    (JD.nullable <| JD.list <| JD.string)
                |> required "config" (JD.dict JD.int)
                |> required "leader-is-player" JD.bool
                |> required "dead-can-see-all-roles" JD.bool
                |> required "autostart-votings" JD.bool
                |> required "autofinish-votings" JD.bool
                |> required "voting-timeout" JD.bool
                |> required "autofinish-rounds" JD.bool
                |> JD.nullable
            )
        |> required "user" (JD.nullable JD.string)
        |> required "user-config"
            ( JD.succeed UserConfig
                |> required "theme" JD.string
                |> required "background" JD.string
                |> JD.nullable
            )

type alias Error = Maybe String

decodeError : Decoder (Maybe String)
decodeError =
    JD.field "error"
        <| JD.nullable
        <| JD.string
