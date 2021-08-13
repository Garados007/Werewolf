module Main exposing (..)

import Browser
import Browser.Navigation exposing (Key)
import Url exposing (Url)
import Url.Parser exposing ((</>), (<?>))
import Url.Parser.Query
import Maybe.Extra
import Dict
import Html exposing (Html)
import Html.Events as HE
import Debug.Extra
import Config exposing (oauthBaseServerUrl)
import OAuth
import OAuth.AuthorizationCode as Auth
import Http
import Json.Decode as JD
import Json.Encode as JE
import MD5
import Data exposing (UserInfo)
import GuestInput
import LobbyInput
import Iso8601
import Pronto

{-| Large parts of the former Main.elm are moved now to GameMain.elm. Main.elm gets a whole new 
purpose and setup routines.

Main has now the responsibility to enable login and joining/creating games. This is a functionality
that didn't exists in the old Game Page.

Now there exists the following states which will cycle and iterate through depending on the user
choises:

- `SelectUser`: The user will see the two options. Login or guest mode. If the user click on
    guest mode the state will transition to `GuestInput`. If the user click on Login a full
    OAuth process will be startet and transition to `OAuthLogin`.

    Path: `/?dev=*` (or any invalid path)
- `GuestInput`: Insert the details for the guest user. After successful input it will transition 
    to `SelectLobyy`. If the user aborts it will transition back to `SelectUser`.

    Path: `/guest?dev=*`
- `OAuthLogin`: A full OAuth process will be triggered. If it succeeds we have our access token and 
    refresh token. Both of them has to be updated regulary. After that it will transition to
    `SelectLobby`.

    A failed Login will transition to `SelectUser` with an addition url option `?fail=true`.

    Path: `/login?dev=*`
- `SelectLobby`: The user has to choose between creating and joining a lobby.

    If the user chooses to create a lobby the page will connect to pronto and fetch a new game 
    server. After that it will transition to `Game` with the returned credentials.

    If the user  chooses to join a lobby the page will ask for a short join code and asking pronto
    for the server. After that it will transition to `Game`.

    Path: `/lobby?dev=*`
- `Game`: The game will connect to the game server via web socket and fetch all information only
    from here. Here starts the original functionality of Main.elm.

    OAuth credentials are no longer refreshed because in this state the user is authentificated
    through the existing web socket connection.

    Path: `/game/<server-id>/<lobby-id>`
- `InitGame`: This is a special state. This is only used after a page refresh and the page was in
    the `Game` state before. The game server url are loaded from pronto. After that it will
    transition to `Game`.

    If the user was logged in using OAuth its credentials are now lost. They are not and will not be
    restored. The page will use the provided user information from the game itself.
-}

main : Program () Model Msg
main =
    Browser.application
        { init = init
        , view = \model ->
            { title = "Werewolf"
            , body = view model
            }
        , update = update
        , subscriptions = subscriptions
        , onUrlChange = always Noop
        , onUrlRequest = always Noop
        }

type alias SelectUserData = 
    { dev: Bool
    , fail: Bool
    , key: Key
    , url: Url
    }

type alias GuestInputData =
    { dev: Bool
    , key: Key
    , url: Url
    , model: GuestInput.Model
    }

type alias OAuthLoginData =
    { dev: Bool
    , key: Key
    , url: Url
    , token: Maybe Auth.AuthenticationSuccess
    }

type alias SelectLobbyData =
    { dev: Bool
    , key: Key
    , url: Url
    , user: UserInfo
    , token: Maybe Auth.AuthenticationSuccess
    , model: LobbyInput.Model
    }

type alias GameData =
    { token: LobbyJoinInfo
    , server: LobbyInput.ConnectInfo
    , user: Maybe UserInfo
    }

type alias InitGameData =
    { serverId: String
    , lobbyId: String
    }

type Model
    = SelectUser SelectUserData
    | GuestInput GuestInputData
    | OAuthLogin OAuthLoginData
    | SelectLobby SelectLobbyData
    | Game GameData
    | InitGame InitGameData

type Msg
    = Noop
    | SelectGuestMode
    | SelectLoginMode
    | GotAccessToken (Result Http.Error Auth.AuthenticationSuccess)
    | GotUserInfo (Result Http.Error UserInfo)
    | ReceiveGuestToken LobbyInput.ConnectInfo (Result Http.Error String)
    | ReceiveLobbyToken LobbyInput.ConnectInfo (Result Http.Error LobbyJoinInfo)
    | WrapGuestInput GuestInput.Msg
    | WrapLobbyInput LobbyInput.Msg

init : () -> Url -> Key -> (Model, Cmd Msg)
init () url key =
    Maybe.Extra.unpack
        (\() ->
            Url.Parser.Query.map2 
                Tuple.pair
                (queryBool "dev")
                (queryBool "fail")
            |> Url.Parser.query
            |> (\ p -> Url.Parser.parse p { url | path = "/" } )
            |> Maybe.withDefault (False, False)
            |> \(dev, fail) -> 
                ( SelectUser
                    { dev = dev
                    , fail = fail
                    , key = key
                    , url = url
                    }
                , Cmd.none
                )
        )
        identity
    <| Url.Parser.parse
        ( Url.Parser.oneOf
            [ Url.Parser.s "login" <?> queryBool "dev"
                |> Url.Parser.map
                (\dev ->
                    case Debug.log "oauth" <| Auth.parseCode url of
                        Auth.Empty ->
                            ( SelectUser
                                { dev = dev
                                , fail = False
                                , key = key
                                , url = url
                                }
                            , Browser.Navigation.pushUrl key
                                <| "/" ++ (if dev then "?dev=true" else "")
                            )
                        Auth.Error _ ->
                            ( SelectUser
                                { dev = dev
                                , fail = True
                                , key = key
                                , url = url
                                }
                            , Browser.Navigation.pushUrl key
                                <| "/?fail=true" ++ (if dev then "&dev=true" else "")
                            )
                        Auth.Success { code } ->
                            ( OAuthLogin
                                { dev = dev
                                , key = key
                                , url = url
                                , token = Nothing
                                }
                            , Http.request <|
                                Auth.makeTokenRequest
                                    GotAccessToken
                                    { credentials =
                                        { clientId = Config.oauthClientId
                                        , secret = Nothing
                                        }
                                    , code = code
                                    , url = 
                                        { oauthBaseServerUrl 
                                        | path = Config.oauthTokenEndpoint 
                                        }
                                    , redirectUri = Debug.log "redirect"
                                        { url
                                        | path = "/login"
                                        , query = if dev then Just "dev=true" else Nothing
                                        }
                                    }
                            )
                )
            , Url.Parser.s "game" </> Url.Parser.string </> Url.Parser.string
                |> Url.Parser.map
                (\serverId lobbyId ->
                    ( InitGame
                        { serverId = serverId
                        , lobbyId = lobbyId
                        }
                    , Cmd.map
                        (Maybe.withDefault Noop)
                        <| Cmd.map
                            ( Maybe.map
                                <| \info ->
                                    ReceiveLobbyToken
                                        { server = info.id
                                        , api = info.info.games
                                            |> List.filterMap
                                                (\game ->
                                                    if game.name == "werewolf"
                                                    then Just game.uri
                                                    else Nothing 
                                                )
                                            |> List.head
                                            |> Maybe.withDefault info.info.uri
                                        , lobby = Nothing
                                        }
                                    <| Ok
                                        { userToken = lobbyId
                                        , joinToken = Nothing
                                        }

                            )
                        <| Pronto.getServerInfo
                            { host = Config.prontoHost }
                            serverId
                    )
                )
            ]
        )
        url


queryBool : String -> Url.Parser.Query.Parser Bool
queryBool name =
    Url.Parser.Query.map
        (Maybe.withDefault False)
    <| Url.Parser.Query.enum name
    <| Dict.fromList
        [ ("true", True)
        , ("false", False)
        , ("1", True)
        , ("0", False)
        ]

view : Model -> List (Html Msg)
view model = (\l -> l ++ [ Debug.Extra.viewModel model ])
    <| case model of
        SelectUser _ ->
            [ Html.text "Select User mode"
            , Html.button
                [ HE.onClick SelectGuestMode ]
                [ Html.text "Guest" ]
            , Html.button
                [ HE.onClick SelectLoginMode ]
                [ Html.text "Login" ]
            ]
        GuestInput data ->
            [ Html.map WrapGuestInput
                <| GuestInput.view data.model
            ]
        SelectLobby data ->
            [ Html.map WrapLobbyInput
                <| LobbyInput.view data.model
            ]
        _ -> []

update : Msg -> Model -> (Model, Cmd Msg)
update msg model =
    case (msg, model) of
        (Noop, _) -> (model, Cmd.none)
        (SelectGuestMode, SelectUser data) ->
            ( GuestInput
                { dev = data.dev
                , key = data.key
                , url = data.url
                , model = GuestInput.init
                }
            , Browser.Navigation.pushUrl data.key
                <| "/guest"
                ++ (if data.dev then "?dev=true" else "")
            )
        (SelectGuestMode, _) -> (model, Cmd.none)
        (WrapGuestInput sub, GuestInput data) ->
            GuestInput.update sub data.model
            |> \(new, cmd, res) ->
                Tuple.mapSecond
                    (\other ->
                        Cmd.batch
                            [ other
                            , Cmd.map WrapGuestInput cmd
                            ]
                    )
                <| case res of
                    Nothing ->
                        ( GuestInput
                            { data
                            | model = new
                            }
                        , Cmd.none
                        )
                    Just (Err ()) ->
                        ( SelectUser
                            { dev = data.dev
                            , fail = False
                            , key = data.key
                            , url = data.url
                            }
                        , Browser.Navigation.pushUrl data.key
                            <| "/"
                            ++ (if data.dev then "?dev=true" else "")
                        )
                    Just (Ok user) ->
                        ( SelectLobby
                            { dev = data.dev
                            , key = data.key
                            , url = data.url
                            , user = user
                            , token = Nothing
                            , model = LobbyInput.init data.dev
                            }
                        , Browser.Navigation.pushUrl data.key
                            <| "/lobby"
                            ++ (if data.dev then "?dev=true" else "")
                        )
        (WrapGuestInput _, _) -> (model, Cmd.none)
        (SelectLoginMode, SelectUser data) ->
            ( model
            , Browser.Navigation.load
                <| Url.toString
                <| Auth.makeAuthorizationUrl
                    { clientId = Config.oauthClientId
                    , redirectUri = data.url |> \url ->
                        { url
                        | path = "/login"
                        , query = if data.dev then Just "dev=true" else Nothing
                        }
                    , scope = [ "openid", "profile" ]
                    , state = Nothing
                    , url =
                        { oauthBaseServerUrl
                        | path = Config.oauthAuthorizationEndpoint
                        }
                    }
            )
        (SelectLoginMode, _) -> (model, Cmd.none)
        (GotAccessToken (Ok token), OAuthLogin data) ->
            ( OAuthLogin
                { data
                | token = Just token
                }
            , Http.request
                { method = "GET"
                , body = Http.emptyBody
                , headers = OAuth.useToken token.token []
                , url = Url.toString
                    { oauthBaseServerUrl
                    | path = Config.oauthUserInfoEndpoint
                    }
                , expect = Http.expectJson GotUserInfo
                    <| JD.map2 UserInfo
                        (JD.field Config.oauthUsernameMap JD.string)
                    <| JD.oneOf
                        [ JD.field Config.oauthPictureMap JD.string
                        , JD.field Config.oauthEmailMap JD.string
                            |> JD.map
                                (\mail ->
                                    "https://www.gravatar.com/avatar/"
                                    ++ MD5.hex mail
                                    ++ "?d=identicon"
                                )
                        ]
                , timeout = Nothing
                , tracker = Nothing
                }
            )
        (GotAccessToken (Err _), OAuthLogin data) ->
            ( SelectUser
                { dev = data.dev
                , fail = True
                , key = data.key
                , url = data.url
                }
            , Browser.Navigation.pushUrl data.key
                <| "/?fail=true"
                ++ (if data.dev then "&dev=true" else "")
            )
        (GotAccessToken _, _) -> (model, Cmd.none)
        (GotUserInfo (Ok userinfo), OAuthLogin data) ->
            ( SelectLobby
                { dev = data.dev
                , key = data.key
                , url = data.url
                , user = userinfo
                , token = data.token
                , model = LobbyInput.init data.dev
                }
            , Browser.Navigation.pushUrl data.key
                <| "/lobby"
                ++ (if data.dev then "?dev=true" else "")
            )
        (GotUserInfo (Err _), OAuthLogin data) ->
            ( SelectUser
                { dev = data.dev
                , fail = True
                , key = data.key
                , url = data.url
                }
            , Browser.Navigation.pushUrl data.key
                <| "/?fail=true"
                ++ (if data.dev then "&dev=true" else "")
            )
        (GotUserInfo _, _) -> (model, Cmd.none)
        (WrapLobbyInput sub, SelectLobby data) ->
            LobbyInput.update sub data.model
            |> \(new, cmd, res) ->
                Tuple.mapSecond
                    (\other ->
                        Cmd.batch
                            [ other
                            , Cmd.map WrapLobbyInput cmd
                            ]
                    )
                <| Tuple.pair
                    (SelectLobby
                        { data | model = new }
                    )
                <| case (res, data.token) of
                    (Nothing, _) ->
                        Cmd.none
                    (Just info, Nothing) ->
                        getGuestToken
                            info
                            data.user.username
                            data.user.picture
                            "en"
                    (Just info, Just token) ->
                        getEnterLobby
                            info
                            (OAuth.tokenToString token.token
                                |> String.split " "
                                |> List.drop 1
                                |> List.head
                                |> Maybe.withDefault ""
                            )
                            False
        (WrapLobbyInput _, _) -> (model, Cmd.none)
        (ReceiveGuestToken info (Ok token), SelectLobby _) ->
            Tuple.pair model
            <| getEnterLobby
                info
                token
                True
        (ReceiveGuestToken  _ _, _) -> (model, Cmd.none)
        -- after that transition to game, we have everything
        (ReceiveLobbyToken info (Ok token), SelectLobby data) ->
            ( Game
                { token = token
                , server = info
                , user = Just data.user
                }
            , Browser.Navigation.pushUrl data.key
                <| "/game/" ++ info.server ++ "/" ++ token.userToken
            )
        (ReceiveLobbyToken info (Ok token), InitGame data) ->
            ( Game
                { token = token
                , server = info
                , user = Nothing
                }
            , Cmd.none
            )
        (ReceiveLobbyToken _ _, _) -> (model, Cmd.none)

formEncodedBody : List (String, String) -> Http.Body
formEncodedBody = 
    Http.stringBody "application/x-www-form-urlencoded"
        << String.concat
        << List.intersperse "&"
        << List.map
            (\(k, v) -> Url.percentEncode k ++ "=" ++ Url.percentEncode v
            )

getGuestToken : LobbyInput.ConnectInfo -> String -> String -> String -> Cmd Msg
getGuestToken info name image language =
    Http.post
        { url = info.api ++ "/api/guest/create"
        , body = formEncodedBody
                [ ("name", name)
                , ("image", image)
                , ("language", language)
                ]
        , expect = Http.expectJson 
            (ReceiveGuestToken info)
            JD.string
        }

type alias LobbyJoinInfo =
    { userToken: String
    , joinToken: Maybe Data.LobbyJoinToken
    }

getEnterLobby : LobbyInput.ConnectInfo -> String -> Bool -> Cmd Msg
getEnterLobby info token guest =
    case info.lobby of
        Just lobby ->
            Http.post
                { url = info.api ++ "/api/lobby/join"
                    ++ (if guest then "?guest=true" else "")
                , body = formEncodedBody
                        [ ("lobby", lobby)
                        , ("token", token)
                        ]
                , expect = Http.expectJson
                    (ReceiveLobbyToken info)
                    <| JD.map
                        (\t -> LobbyJoinInfo t Nothing)
                    <| JD.string
                }
        Nothing ->
            Http.post
                { url = info.api ++ "/api/lobby/create"
                    ++ (if guest then "?guest=true" else "")
                , body = formEncodedBody
                        [ ("token", token) ]
                , expect = Http.expectJson
                    (ReceiveLobbyToken info)
                    <| JD.map2 LobbyJoinInfo
                        (JD.field "id" JD.string)
                    <| JD.map Just
                    <| JD.field "join-token"
                    <| JD.map2 Data.LobbyJoinToken
                        (JD.field "token" JD.string)
                        (JD.field "alive-until" Iso8601.decoder)
                }

subscriptions : Model -> Sub Msg
subscriptions model =
    Sub.none
