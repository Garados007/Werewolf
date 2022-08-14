module EventData exposing (..)

import Data exposing (..)
import Level exposing (LevelData)
import Json.Decode as JD exposing (Decoder)
import Json.Decode.Pipeline exposing (required)
import Iso8601
import Dict exposing (Dict)
import Time exposing (Posix)

type EventData
    = SendGameData GameGlobalState
    | AddParticipant String GameUser
    | AddVoting GameVoting
    | ChatEvent Data.ChatMessage
    | GameEnd (Maybe (List String))
    | GameStart (Dict String (Maybe GameParticipant))
    | MultiPlayerNotification (Dict String (List String))
    | NextPhase (Maybe String)
    | OnLeaderChanged String
    | OnRoleInfoChanged (Maybe String) GameParticipant
    | PlayerNotification String (List String)
    | RemoveParticipant String
    | RemoveVoting String
    | SendStage GameStage
    | SetGameConfig EventGameConfig
    | SetUserConfig UserConfig
    | SetVotingTimeout String (Maybe Posix)
    | SetVotingVote String String String -- voting option voter
    | SubmitRoles RoleTemplates
    | Success
    | GetJoinToken LobbyJoinToken
    | OnlineNotification String Data.OnlineInfo
    | Maintenance (Maybe String) Posix
    | SendStats (Dict String (GameUserStats, LevelData))
    | ChatServiceMessage Data.ChatServiceMessage

type alias EventGameConfig =
    { config: Dict String Int
    , leaderIsPlayer: Bool
    , deadCanSeeAllRoles: Bool
    , allCanSeeRoleOfDead: Bool
    , autostartVotings: Bool
    , autofinishVotings: Bool
    , votingTimeout: Bool
    , autofinishRound: Bool
    , theme: (String, String)
    }

decodeEventData : Decoder EventData
decodeEventData =
    JD.andThen
        (\key ->
            case key of
                "submit-roles" ->
                    JD.string
                    |> JD.list
                    |> JD.dict
                    |> JD.field "roles"
                    |> JD.map SubmitRoles
                "Success" -> JD.succeed Success
                "SendGameData" ->
                    JD.map 
                        SendGameData
                        decodeGameGlobalState
                "AddParticipant" ->
                    JD.succeed GameUser
                    |> required "name" JD.string
                    |> required "img" JD.string
                    |> required "is-guest" JD.bool
                    |> required "stats"
                        ( JD.succeed GameUserStats
                            |> required "win-games" JD.int
                            |> required "killed" JD.int
                            |> required "loose-games" JD.int
                            |> required "leader" JD.int
                        )
                    |> required "stats"
                        (JD.succeed LevelData
                            |> required "level" JD.int
                            |> required "current-xp" JD.int
                            |> required "max-xp" JD.int
                        )
                    |> JD.map2 AddParticipant
                        (JD.field "id" JD.string)
                "AddVoting" ->
                    JD.succeed GameVoting
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
                    |> JD.map AddVoting
                    |> JD.field "voting"
                "ChatEvent" ->
                    JD.map ChatEvent Data.decodeChatMessage
                "GameEnd" ->
                    JD.map GameEnd
                    <| JD.field "winner"
                    <| JD.nullable
                    <| JD.list
                    <| JD.string
                "GameStart" ->
                    JD.succeed GameParticipant
                    |> required "tags" (JD.list JD.string)
                    |> required "role" (JD.nullable JD.string)
                    |> JD.nullable
                    |> JD.dict
                    |> JD.field "participants"
                    |> JD.map GameStart
                "MultiPlayerNotification" ->
                    JD.map MultiPlayerNotification
                    <| JD.field "notifications"
                    <| JD.dict
                    <| JD.list JD.string
                "NextPhase" ->
                    JD.map NextPhase
                    <| JD.field "phase"
                    <| JD.nullable
                    <| JD.field "lang-id"
                    <| JD.string
                "OnLeaderChanged" ->
                    JD.succeed OnLeaderChanged
                    |> required "leader" JD.string
                "OnRoleInfoChanged" ->
                    JD.succeed GameParticipant
                    |> required "tags" (JD.list JD.string)
                    |> required "role" (JD.nullable JD.string)
                    |> JD.map2 OnRoleInfoChanged
                        (JD.field "id" <| JD.nullable JD.string)
                "PlayerNotification" ->
                    JD.succeed PlayerNotification
                    |> required "text-id" JD.string
                    |> required "player" (JD.list JD.string)
                "RemoveParticipant" ->
                    JD.succeed RemoveParticipant
                    |> required "id" JD.string
                "RemoveVoting" ->
                    JD.succeed RemoveVoting
                    |> required "id" JD.string
                "SendStage" ->
                    JD.succeed GameStage
                    |> required "lang-id" JD.string
                    |> required "background-id" JD.string
                    |> required "theme" JD.string
                    |> JD.map SendStage
                "SetGameConfig" ->
                    JD.succeed EventGameConfig
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
                    |> JD.map SetGameConfig
                "SetUserConfig" ->
                    JD.succeed UserConfig
                    |> required "theme" JD.string
                    |> required "background" JD.string
                    |> JD.field "user-config"
                    |> JD.map SetUserConfig
                "SetVotingTimeout" ->
                    JD.succeed SetVotingTimeout
                    |> required "id" JD.string
                    |> required "timeout" (JD.nullable Iso8601.decoder)
                "SetVotingVote" ->
                    JD.succeed SetVotingVote
                    |> required "voting" JD.string
                    |> required "option" JD.string 
                    |> required "voter" JD.string
                "GetJoinToken" ->
                    JD.succeed LobbyJoinToken
                    |> required "token" JD.string
                    |> required "alive-until" Iso8601.decoder
                    |> JD.map GetJoinToken
                "OnlineNotification" ->
                    JD.succeed OnlineInfo
                    |> required "online" JD.bool
                    |> required "counter" JD.int
                    |> required "last-changed" Iso8601.decoder
                    |> JD.map2 OnlineNotification
                        (JD.field "user" JD.string)
                "EnterMaintenance" ->
                    JD.succeed Maintenance
                    |> required "reason" (JD.nullable JD.string)
                    |> required "forced-shutdown" Iso8601.decoder
                "SendStats" ->
                    JD.field "stats"
                    <| JD.map SendStats
                    <| JD.dict
                    <| JD.map2 Tuple.pair
                        (JD.succeed GameUserStats
                            |> required "win-games" JD.int
                            |> required "killed" JD.int
                            |> required "loose-games" JD.int
                            |> required "leader" JD.int
                        )
                        (JD.succeed LevelData
                            |> required "level" JD.int
                            |> required "current-xp" JD.int
                            |> required "max-xp" JD.int
                        )
                "ChatServiceMessage" ->
                    JD.map ChatServiceMessage
                    <| Data.decodeChatServiceMessage
                _ -> JD.fail <| "unknown event " ++ key
        )
    <| JD.field "$type" JD.string
