module LobbyInput exposing (..)

import Html exposing (Html, div, text)
import Html.Attributes as HA exposing (class)
import Html.Events as HE
import Pronto
import Triple
import Config
import Language exposing (Language)
import Views.ViewLayout as Layout

type alias Model =
    { lobbyToken: String
    , dev: Bool
    , error: Maybe Error
    , loading: Bool
    }

type Msg
    = Input String
    | SelectJoin
    | GotJoinToken (Maybe Pronto.JoinTokenInfo)
    | SelectCreate
    | GotCreateServer (Maybe Pronto.TargetHost)
    | RemoveError

type alias ConnectInfo =
    { server: String
    , api: String
    , lobby: Maybe String
    }

type Error
    = InvalidLobbyToken
    | NoServerFound

init : Bool -> Model
init dev =
    { lobbyToken = ""
    , error = Nothing
    , dev = dev
    , loading = False
    }

singleLangBlock : Language -> List String -> List (Html msg)
singleLangBlock lang =
    List.singleton
    << singleLang lang

singleLang : Language -> List String -> (Html msg)
singleLang lang = 
    Html.text
    << Language.getTextOrPath lang

viewBanner : Model -> List (Layout.LayoutBanner Msg)
viewBanner model =
    case model.error of
        Nothing -> []
        Just InvalidLobbyToken ->
            List.singleton
                { closeable = Just RemoveError
                , content = text "Invalid Lobby token"
                }
        Just NoServerFound ->
            List.singleton
                { closeable = Just RemoveError
                , content = text "No Server found"
                }

view : Model -> Language -> Html Msg
view model lang =
    div [ class "lobby-selection-box" ]
        [ div [ class "options" ]
            [ div [ class "option" ]
                [ div [ class "title" ]
                    <| singleLangBlock lang
                        [ "init", "lobby-input", "create" ]
                , div [ class "space" ] []
                , Html.button
                    [ HE.onClick SelectCreate ]
                    <| singleLangBlock lang
                        [ "init", "lobby-input", "start" ]
                ]
            , div [ class "option" ]
                [ div [ class "title" ]
                    <| singleLangBlock lang
                        [ "init", "lobby-input", "join" ]
                , Html.input
                    [ HA.type_ "text" 
                    , HA.value model.lobbyToken
                    , HA.placeholder
                        <| Language.getTextOrPath lang
                            [ "init", "lobby-input", "hint" ]
                    , HE.onInput Input
                    ] []
                , Html.button
                    [ HE.onClick SelectJoin ]
                    <| singleLangBlock lang
                        [ "init", "lobby-input", "start" ]
                ]
            ]
        , case model.error of
            Nothing -> text ""
            Just InvalidLobbyToken ->
                div [ class "error" ]
                    [ text "Invalid Lobby token" ]
            Just NoServerFound ->
                div [ class "error" ]
                    [ text "No Server found" ]
        ]

update : Msg -> Model -> (Model, Cmd Msg, Maybe ConnectInfo)
update msg model =
    case msg of
        Input code ->
            Triple.triple
                { model 
                | lobbyToken = String.left 4 code
                , error = Nothing
                }
                Cmd.none
                Nothing
        SelectJoin ->
            Triple.triple
                { model | loading = True }
                (Pronto.getJoinTokenInfo
                    { host = Config.prontoHost }
                    model.lobbyToken
                |> Cmd.map GotJoinToken
                )
                Nothing
        GotJoinToken (Just token) ->
            Triple.triple
                model
                Cmd.none
            <| Just
                { server = token.server
                , api = token.gameUri
                    |> Maybe.withDefault token.apiUri
                    |> fixPath
                , lobby = Just token.lobby
                }
        GotJoinToken Nothing ->
            Triple.triple
                { model 
                | error = Just InvalidLobbyToken
                , loading = False
                }
                Cmd.none
                Nothing
        SelectCreate ->
            Triple.triple
                { model | loading = True }
                ( Pronto.getTargetHost
                    { host = Config.prontoHost }
                    { game = "werewolf"
                    , developer = model.dev
                    , fallback = True
                    }
                |> Cmd.map GotCreateServer
                )
                Nothing
        GotCreateServer (Just info) ->
            Triple.triple
                model
                Cmd.none
            <| Just
                { server = info.id
                , api = fixPath info.gameUri
                , lobby = Nothing
                }
        GotCreateServer Nothing ->
            Triple.triple
                { model 
                | error = Just NoServerFound
                , loading = False
                }
                Cmd.none
                Nothing
        RemoveError ->
            Triple.triple
                { model | error = Nothing }
                Cmd.none
                Nothing

fixPath : String -> String
fixPath path =
    if String.endsWith "/" path
    then String.left (String.length path - 1) path
        |> fixPath
    else path