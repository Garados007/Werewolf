module GameMain exposing (..)

import Data
import EventData exposing (EventData)
import Model exposing (Model)
import Network exposing (NetworkResponse)
import Language exposing (Language)

import Views.ViewUserList
import Views.ViewRoomEditor
import Views.ViewNoGame
import Views.ViewGamePhase
import Views.ViewErrors
import Views.ViewSettingsBar
import Views.ViewThemeEditor
import Views.ViewModal
import Views.ViewWinners
import Views.ViewPlayerNotification
import Views.ViewRoleInfo
import Views.ViewChat
import Views.ViewCloseReason

import Browser.Dom
import Html exposing (Html, div, text)
import Html.Attributes as HA exposing (class)
import Task
import Time exposing (Posix)
import Maybe.Extra
import Dict
import Views.ViewGamePhase
import Maybe.Extra
import Level
import Styles
import Ports

import Json.Decode as JD
import Json.Encode as JE
import WebSocket
import Maybe
import Level
import Styles
import Styles
import Views.ViewModal
import Model
import Language exposing (LanguageInfo)
import Dict exposing (Dict)
import Http

type Msg
    = Response NetworkResponse
    | SetTime Posix
    | Noop
    | Init
    | WrapUser Views.ViewUserList.Msg
    | WrapEditor Views.ViewRoomEditor.Msg
    | WrapPhase Views.ViewGamePhase.Msg
    | WrapError Int
    | WrapSelectModal Views.ViewSettingsBar.Msg
    | WrapThemeEditor Views.ViewThemeEditor.Msg
    | WrapChat Views.ViewChat.Msg
    | CloseModal
    | WsMsg (Result JD.Error WebSocket.WebSocketMsg)
    | WsClose (Result JD.Error Network.SocketClose)

init : String -> String -> String -> LanguageInfo -> Dict String Language 
    -> Maybe Data.LobbyJoinToken -> (Model, Cmd Msg)
init token api selLang langInfo rootLang joinToken =
    ( Model.init token selLang langInfo rootLang joinToken
    , Cmd.batch
        [ Task.perform identity
            <| Task.succeed Init
        , Network.wsSend Network.FetchRoles
        , Network.wsConnect api token
        ]
    )

view : Model -> List (Html Msg)
view model = view_internal model <| Model.getLanguage model

view_internal : Model -> Language -> List (Html Msg)
view_internal model lang =
    [ Html.node "link"
        [ HA.attribute "rel" "stylesheet"
        , HA.attribute "property" "stylesheet"
        , HA.attribute "href" "/content/css/style.css"
        ] []
    , Html.node "link"
        [ HA.attribute "rel" "stylesheet"
        , HA.attribute "property" "stylesheet"
        , HA.attribute "href" "/content/vendor/flag-icon-css/css/flag-icon.min.css"
        ] []
    , Styles.view model.now model.styles
    , tryViewGamePhase model lang
        |> Maybe.Extra.orElseLazy
            (\() -> tryViewGameFrame model lang)
        |> Maybe.Extra.orElseLazy
            (\() -> Just
                <| Views.ViewNoGame.view lang
                <| model.state == Nothing || model.roles == Nothing
            )
        |> Maybe.withDefault (text "")
    , case (model.chatView, Maybe.map .game model.state) of
        (Just input, Just game) -> 
            Views.ViewChat.view lang game model.chats input
                |> Html.map WrapChat
        _ -> text ""
    , case model.modal of
        Model.NoModal -> text ""
        Model.SettingsModal conf ->
            Views.ViewModal.viewExtracted CloseModal WrapThemeEditor
                ( Language.getTextOrPath lang
                    [ "modals", "theme-editor", "title" ]
                )
                <| List.singleton
                <| Views.ViewThemeEditor.view
                    lang
                    model.langInfo.icons
                    conf
        Model.WinnerModal game list ->
            Html.map (always CloseModal)
                <| Views.ViewModal.viewOnlyClose 
                    ( Language.getTextOrPath lang
                        [ "modals", "winner", "title" ]
                    )
                <| List.singleton
                <| Views.ViewWinners.view
                    lang
                    model.now model.levels
                    game list
        Model.PlayerNotification notification ->
            Html.map (always CloseModal)
                <| Views.ViewModal.viewOnlyClose
                    ( Language.getTextOrPath lang
                        [ "modals", "player-notification", "title" ]
                    )
                <| List.map
                    (\(nid,player) ->
                        Views.ViewPlayerNotification.view
                            lang
                            (Maybe.map .game model.state)
                            model.removedUser
                            nid
                            player
                    )
                <| Dict.toList notification
        Model.RoleInfo roleKey ->
            Html.map (always CloseModal)
                <| Views.ViewModal.viewOnlyClose
                    ( Language.getTextOrPath lang
                        [ "theme", "roles", roleKey ]
                    )
                <| List.singleton
                <| Views.ViewRoleInfo.view lang roleKey
        Model.Maintenance reason ->
            Html.map (always CloseModal)
            <| Views.ViewModal.viewOnlyClose
                ( Language.getTextOrPath lang
                    [ "modals", "maintenance", "title" ]
                )
            [ div [ class "maintenance", class "desc" ]
                [ text <| Language.getTextOrPath lang
                    [ "modals", "maintenance", "desc" ]
                ]
            , case reason of
                Nothing -> text ""
                Just r ->
                    div [ class "maintenance", class "reason" ]
                    [ Html.span []
                        [ text <| Language.getTextOrPath lang
                            [ "modals", "maintenance", "reason" ]
                        ]
                    , Html.span [] [ text r ]
                    ]
            ]
    , case model.closeReason of
        Nothing -> text ""
        Just info ->
            Html.map (always Noop)
            <| Views.ViewCloseReason.view lang info
    , Views.ViewErrors.view model.errors
        |> Html.map WrapError
    ]

viewEvents : List (Bool, String) -> Html msg
viewEvents events =
    div [ class "event-list" ]
        <| List.map
            (\(used, content) ->
                div [ HA.classList
                        [ ("event-item", True)
                        , ("used", used)
                        ]
                    ]
                    [ text content ]
            )
            events
    -- text ""

tryViewGameFrame : Model -> Language -> Maybe (Html Msg)
tryViewGameFrame model lang =
    Maybe.map2
        (viewGameFrame model lang)
        model.roles
        model.state

viewGameFrame : Model
    -> Language
    -> Data.RoleTemplates
    -> Data.GameGlobalState
    -> Html Msg
viewGameFrame model lang roles state =
    div [ class "frame-game-outer" ]
        [ div [ class "frame-game-left" ]
            [ Html.map WrapUser
                <| Views.ViewUserList.view
                    lang
                    model.now model.levels
                    state.game 
                    state.user
                    model.joinToken
                    model.codeCopied
            ]
        , div [ class "frame-game-body" ]
            [ Html.map WrapSelectModal
                <| Views.ViewSettingsBar.view model
            , Html.map WrapEditor
                <| Views.ViewRoomEditor.view
                    lang
                    model.langInfo
                    roles
                    state
                    (Just state.game.theme)
                    state.game
                    (state.user == state.game.leader)
                    model.editor
            ]
        , viewEvents model.events
        ]

tryViewGamePhase : Model -> Language -> Maybe (Html Msg)
tryViewGamePhase model lang =
    Maybe.andThen
        (\state ->
            Maybe.map
                (\phase -> 
                    viewGamePhase model lang state.game state.user phase
                )
                state.game.phase
        )
        model.state

viewGamePhase : Model
    -> Language
    -> Data.Game
    -> String
    -> Data.GamePhase
    -> Html Msg
viewGamePhase model lang game user phase =
    div [ class "frame-game-outer" ]
        [ div [ class "frame-game-left" ]
            [ Html.map WrapUser
                <| Views.ViewUserList.view
                    lang
                    model.now model.levels
                    game user
                    model.joinToken
                    model.codeCopied
            ]
        , div [ class "frame-game-body", class "top" ]
            [ Html.map WrapSelectModal
                <| Views.ViewSettingsBar.view model
            , Html.map WrapPhase
                <| Views.ViewGamePhase.view
                    lang
                    model.now
                    game
                    phase
                    (user == game.leader)
                    user
            ]
        , viewEvents model.events
        ]

update : Msg -> Model -> (Model, Cmd Msg)
update msg model =
    let
        (newModel, cmd) = update_internal msg model
    in Tuple.pair
        { newModel
        | styles = Styles.pushState
                newModel.now
                newModel.styles
            <| Maybe.withDefault 
                model.bufferedConfig
            <| Maybe.Extra.orElse
                ( Maybe.map .userConfig model.state)
            <| Maybe.Extra.orElse
                ( Maybe.map .game model.state
                    |> Maybe.andThen .phase
                    |> Maybe.map .stage
                    |> Maybe.andThen
                        (\stage ->
                            if stage.backgroundId == ""
                            then Nothing
                            else Just
                                { theme = stage.theme
                                , background = stage.backgroundId
                                , language = ""
                                }   
                        )
                )
            <| case model.modal of
                Model.SettingsModal conf ->
                    Just conf.config
                _ -> Nothing
        }
        cmd

update_internal : Msg -> Model -> (Model, Cmd Msg)
update_internal msg model =
    case msg of
        Response resp ->
            Tuple.mapSecond
                (List.map
                    (Network.executeRequest
                        >> Cmd.map Response
                    )
                    >> Cmd.batch
                )
            <| Model.applyResponse resp model
        Noop -> (model, Cmd.none)
        Init ->
            Tuple.pair model
                <| Cmd.map Response
                <| Cmd.batch []
                    -- [ Network.executeRequest
                    --     <| Network.GetGame model.token
                    -- , Network.executeRequest
                    --     <| Network.GetRoles
                    -- ]
        SetTime now ->
            Tuple.pair
                { model | now = now }
                Cmd.none
        WrapUser (Views.ViewUserList.Send req) ->
            Tuple.pair model
            <| Network.execute Response req
        WrapUser (Views.ViewUserList.CopyToClipboard content) ->
            Tuple.pair
                { model
                | codeCopied = Just model.now
                }
            <| Ports.sendToClipboard
            <| JE.string content
        WrapEditor (Views.ViewRoomEditor.SetBuffer buffer req) ->
            Tuple.pair
                { model | editor = buffer }
            <| Network.wsSend
            <| Network.SetGameConfig req
        WrapEditor (Views.ViewRoomEditor.SendConf req) ->
            Tuple.pair model
            <| Network.wsSend
            <| Network.SetGameConfig req
        WrapEditor Views.ViewRoomEditor.StartGame ->
            Tuple.pair model
            <| Network.wsSend Network.GameStart
        WrapEditor (Views.ViewRoomEditor.ShowRoleInfo roleKey) ->
            Tuple.pair
                { model | modal = Model.RoleInfo roleKey }
                Cmd.none
        WrapEditor Views.ViewRoomEditor.Noop ->
            Tuple.pair model Cmd.none
        WrapPhase Views.ViewGamePhase.Noop ->
            Tuple.pair model Cmd.none
        WrapPhase (Views.ViewGamePhase.Send req) ->
            Tuple.pair model
            <| Network.execute Response req
        WrapError index ->
            Tuple.pair
                { model 
                | errors = 
                    List.filterMap
                        (\(ind, entry) ->
                            if ind /= index
                            then Just entry
                            else Nothing
                        )
                    <| List.indexedMap Tuple.pair
                    <| model.errors
                }
                Cmd.none
        WrapSelectModal (Views.ViewSettingsBar.ViewModal modal) ->
            Tuple.pair { model | modal = modal } Cmd.none
        WrapSelectModal Views.ViewSettingsBar.ViewChat ->
            Tuple.pair 
                { model 
                | chatView = Just ""
                , chats = List.map
                    (\chat -> { chat | shown = True })
                    model.chats
                } 
                Cmd.none
        WrapThemeEditor sub ->
            case model.modal of
                Model.SettingsModal editor ->
                    let 
                        (newEditor, newEvent) = Views.ViewThemeEditor.update sub editor
                        sendEvents = List.filterMap
                            (\event ->
                                case event of
                                    Views.ViewThemeEditor.Send req -> Just req
                            )
                            newEvent
                    in Tuple.pair
                        { model | modal = Model.SettingsModal newEditor }
                        <| Cmd.batch
                        <| List.map 
                            (Network.wsSend 
                                << Network.SetUserConfig
                            )
                            sendEvents
                _ -> (model, Cmd.none)
        WrapChat (Views.ViewChat.SetInput input) ->
            Tuple.pair
                { model | chatView = Just input }
                Cmd.none
        WrapChat (Views.ViewChat.Send input) ->
            Tuple.pair
                { model | chatView = Just "" }
            <| Network.wsSend
            <| Network.Message
                (model.state
                    |> Maybe.map .game
                    |> Maybe.andThen .phase
                    |> Maybe.map .langId
                )
                input
        WrapChat Views.ViewChat.Close ->
            Tuple.pair
                { model | chatView = Nothing }
                Cmd.none
        CloseModal ->
            ({ model | modal = Model.NoModal }, Cmd.none)
        WsMsg (Ok (WebSocket.Data d)) ->
            let
                decodedData : Result String EventData
                decodedData = d.data
                    |> JD.decodeString EventData.decodeEventData
                    |> Result.mapError JD.errorToString

                formatedRaw : String
                formatedRaw = d.data
                    |> JD.decodeString JD.value
                    |> Result.toMaybe
                    |> Maybe.map (JE.encode 2)
                    |> Maybe.withDefault d.data

                doScroll : String -> Cmd Msg
                doScroll id =
                    Browser.Dom.getViewportOf id
                        |> Task.andThen
                            (\info ->
                                Browser.Dom.setViewportOf
                                    id
                                    0
                                    info.scene.height
                            )
                        |> Task.attempt
                            (always Noop)

                doScrollIfNewChat : (Model, Cmd Msg) -> (Model, Cmd Msg)
                doScrollIfNewChat (newModel,newMsg) =
                    if newModel.chats /= model.chats
                    then Tuple.pair newModel
                        <| Cmd.batch
                            [ newMsg
                            , doScroll "chat-box-history"
                            ]
                    else (newModel, newMsg)
                    
            in case decodedData of
                Ok data ->
                    doScrollIfNewChat
                    <| Tuple.mapSecond
                        (List.map
                            (Network.execute Response)
                            >> Cmd.batch
                        )
                    <| Model.applyEventData data model
                        -- { model
                        -- | events = (True, formatedRaw) :: model.events
                        -- }
                Err err -> Tuple.pair 
                    { model
                    | events = (False, formatedRaw) :: model.events
                    , errors = (++) model.errors
                        <| List.singleton 
                        <| "Socket error: " ++ err
                    }
                    Cmd.none
        WsMsg (Ok (WebSocket.Error { error })) ->
            Tuple.pair
                { model
                | errors = model.errors ++ [ error ]
                }
                Cmd.none
        WsMsg (Err err) ->
            Tuple.pair
                { model
                | errors = (++) model.errors
                    <| List.singleton
                    <| JD.errorToString err
                }
                Cmd.none
        WsClose (Err err) ->
            Tuple.pair
                { model
                | errors = (++) model.errors
                    <| List.singleton
                    <| JD.errorToString err
                }
                Cmd.none
        WsClose (Ok reason) ->
            Tuple.pair
                { model
                | closeReason = Just reason
                }
                Cmd.none

subscriptions : Model -> Sub Msg
subscriptions model =
    Sub.batch
        [ Time.every
            (   if (Dict.values model.levels
                        |> List.any Level.isAnimating
                    ) 
                    || Styles.isAnimating model.now model.styles
                then 50
                else 1000
            )
            SetTime  
        -- , Time.every 1000 SetTime
        -- [ Sub.none
        -- , if model.game 
        --         |> Maybe.map 
        --             (\result -> result.game /= Nothing &&
        --                 result.user /= Nothing
        --             )
        --         |> Maybe.withDefault True
        --     then Time.every 3000 (always FetchData)
        --     else Time.every 20000 (always FetchData)
        -- , Time.every 60000 (always FetchData)
        , Network.wsReceive WsMsg
        , Network.wsClose WsClose
        ]
