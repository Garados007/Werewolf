module Main exposing (..)

import Browser
import Browser.Navigation exposing (Key)
import Url exposing (Url)
import Url.Parser exposing ((</>), (<?>))
import Url.Parser.Query
import Maybe.Extra
import Dict exposing (Dict)
import Html exposing (Html)
import Html.Attributes as HA
import Html.Events as HE
--!BEGIN
import Debug.Extra
--!END
import Config exposing (oauthBaseServerUrl)
import OAuth
import OAuth.AuthorizationCode as Auth
import Http
import Json.Decode as JD
import MD5
import Data exposing (UserInfo)
import GuestInput
import LobbyInput
import Iso8601
import Pronto
import Language
import Network

import Model
import GameMain
import Html.Attributes exposing (lang)
import Views.ViewVersion

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

type alias LangContainer =
    { lang: String
    , root: Dict String Language.Language
    , info: Language.LanguageInfo
    }

type alias SelectUserData = 
    { dev: Bool
    , lang: LangContainer
    , fail: Bool
    , key: Key
    , url: Url
    }

type alias GuestInputData =
    { dev: Bool
    , lang: LangContainer
    , key: Key
    , url: Url
    , model: GuestInput.Model
    }

type alias OAuthLoginData =
    { dev: Bool
    , lang: LangContainer
    , key: Key
    , url: Url
    , token: Maybe Auth.AuthenticationSuccess
    }

type alias SelectLobbyData =
    { dev: Bool
    , lang: LangContainer
    , key: Key
    , url: Url
    , user: UserInfo
    , token: Maybe Auth.AuthenticationSuccess
    , model: LobbyInput.Model
    , loading: Bool
    }

type alias GameData =
    { server: LobbyInput.ConnectInfo
    , user: Maybe UserInfo
    , game: Model.Model
    }

type alias InitGameData =
    { lang: LangContainer
    , serverId: String
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
    | ReceiveLangInfo (Result Http.Error Language.LanguageInfo)
    | ReceiveRootLang String (Result Http.Error Language.Language)
    | WrapGuestInput GuestInput.Msg
    | WrapLobbyInput LobbyInput.Msg
    | WrapGame GameMain.Msg

getLang : Model -> LangContainer
getLang model =
    case model of
        SelectUser { lang } -> lang
        GuestInput { lang } -> lang
        OAuthLogin { lang } -> lang
        SelectLobby { lang } -> lang
        Game { game } ->
            { lang = game.selLang
            , info = game.langInfo
            , root = game.rootLang
            }
        InitGame { lang } -> lang

getRootLang : LangContainer -> Language.Language
getRootLang lang =
    Dict.get lang.lang lang.root
    |> Maybe.withDefault Language.LanguageUnknown

setLang : LangContainer -> Model -> Model
setLang lang model =
    case model of
        SelectUser data -> SelectUser { data | lang = lang }
        GuestInput data -> GuestInput { data | lang = lang }
        OAuthLogin data -> OAuthLogin { data | lang = lang }
        SelectLobby data -> SelectLobby { data | lang = lang }
        Game data -> Game 
            { data 
            | game = data.game |> \game ->
                { game
                | selLang = lang.lang
                , langInfo = lang.info
                , rootLang = lang.root
                }
            }
        InitGame data -> InitGame { data | lang = lang }

redirectUri : Url -> LangContainer -> Bool -> Url
redirectUri url lang dev =
    { url
    | path = "/login"
    , query = Just
        <| "lang=" ++ lang.lang
        ++ (if dev then "&dev=true" else "")
    }

init : () -> Url -> Key -> (Model, Cmd Msg)
init () url key =
    let
        lang : LangContainer
        lang = 
            { lang = Maybe.withDefault "en"
                <| Maybe.andThen identity
                <| Url.Parser.parse
                    (Url.Parser.query
                        <| Url.Parser.Query.string "lang"
                    )
                    { url | path = "" }
            , info =
                { languages = Dict.empty
                , icons = Dict.empty
                , themes = Dict.empty
                }
            , root = Dict.empty
            }
    in 
        Tuple.mapSecond
            (\cmd ->
                Cmd.batch
                    [ cmd
                    , Network.getLangInfo
                        |> Cmd.map ReceiveLangInfo
                    , Network.getRootLang lang.lang
                        |> Cmd.map 
                            (ReceiveRootLang lang.lang)
                    ]
            )
        <| Maybe.Extra.unpack
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
                        , lang = lang
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
                        case Auth.parseCode url of
                            Auth.Empty ->
                                ( SelectUser
                                    { dev = dev
                                    , lang = lang
                                    , fail = False
                                    , key = key
                                    , url = url
                                    }
                                , Browser.Navigation.pushUrl key
                                    <| "/?lang=" ++ lang.lang ++
                                    (if dev then "&dev=true" else "")
                                )
                            Auth.Error _ ->
                                ( SelectUser
                                    { dev = dev
                                    , lang = lang
                                    , fail = True
                                    , key = key
                                    , url = url
                                    }
                                , Browser.Navigation.pushUrl key
                                    <| "/?fail=true&lang=" ++ lang.lang ++
                                    (if dev then "&dev=true" else "")
                                )
                            Auth.Success { code } ->
                                ( OAuthLogin
                                    { dev = dev
                                    , lang = lang
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
                                        , redirectUri = redirectUri url lang dev
                                        }
                                )
                    )
                , Url.Parser.s "game" </> Url.Parser.string </> Url.Parser.string
                    |> Url.Parser.map
                    (\serverId lobbyId ->
                        ( InitGame
                            { lang = lang
                            , serverId = serverId
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

singleLangBlock : Model -> List String -> List (Html msg)
singleLangBlock model =
    List.singleton
    << singleLang model

singleLang : Model -> List String -> (Html msg)
singleLang model = 
    Html.text
    << Language.getTextOrPath
        (getRootLang <| getLang model)

view : Model -> List (Html Msg)
view model =
--!BEGIN
    (\l -> l ++ [ Debug.Extra.viewModel model ]) <|
--!END
    case model of
        SelectUser _ ->
            [ Html.node "link"
                [ HA.attribute "rel" "stylesheet"
                , HA.attribute "property" "stylesheet"
                , HA.attribute "href" "/content/css/style.css"
                ] []
            , Html.div [ HA.class "init-select-user" ]
                [ Html.h1 [ HA.class "welcomer"]
                    <| singleLangBlock model
                        [ "init", "title" ]
                , Html.h2 []
                    <| singleLangBlock model
                        [ "init", "description" ]
                , Html.div [ HA.class "options" ]
                    [ Html.div 
                        [ HA.class "option" 
                        , HE.onClick SelectLoginMode
                        ]
                        [ Html.div [ HA.class "play-as" ]
                            <| singleLangBlock model
                                [ "init", "user-mode", "play-login" ]
                        , Html.ul []
                            <| List.map
                                (\i ->
                                    Html.li []
                                        <| singleLangBlock model
                                        [ "init", "user-mode", "login", String.fromInt i ]
                                )
                            <| List.range 1 3
                        ]
                    , Html.div 
                        [ HA.class "option" 
                        , HE.onClick SelectGuestMode
                        ]
                        [ Html.div [ HA.class "play-as" ]
                            <| singleLangBlock model
                                [ "init", "user-mode", "play-guest" ]
                        , Html.ul []
                            <| List.map
                                (\i ->
                                    Html.li []
                                        <| singleLangBlock model
                                        [ "init", "user-mode", "guest", String.fromInt i ]
                                )
                            <| List.range 1 2
                        ]
                    ]
                ]
            , Views.ViewVersion.view
            ]
        GuestInput data ->
            [ Html.node "link"
                [ HA.attribute "rel" "stylesheet"
                , HA.attribute "property" "stylesheet"
                , HA.attribute "href" "/content/css/style.css"
                ] []
            , Html.map WrapGuestInput
                <| GuestInput.view data.model 
                <| getRootLang data.lang
            , Views.ViewVersion.view
            ]
        SelectLobby data ->
            [ Html.node "link"
                [ HA.attribute "rel" "stylesheet"
                , HA.attribute "property" "stylesheet"
                , HA.attribute "href" "/content/css/style.css"
                ] []
            , if data.loading || data.model.loading
                then Html.div [ HA.id "elm" ]
                    [ Html.div [ HA.class "lds-heart" ]
                        [ Html.div [] [] ]
                    ]
                else Html.map WrapLobbyInput
                    <| LobbyInput.view data.model
                    <| getRootLang data.lang
            , Views.ViewVersion.view
            ]
        Game data ->
            List.map (Html.map WrapGame)
            <| GameMain.view data.game
        _ -> []

update : Msg -> Model -> (Model, Cmd Msg)
update msg model =
    case (msg, model) of
        (Noop, _) -> (model, Cmd.none)
        (SelectGuestMode, SelectUser data) ->
            ( GuestInput
                { dev = data.dev
                , lang = data.lang
                , key = data.key
                , url = data.url
                , model = GuestInput.init
                }
            , Browser.Navigation.pushUrl data.key
                <| "/guest?lang=" ++ data.lang.lang
                ++ (if data.dev then "&dev=true" else "")
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
                            , lang = data.lang
                            , fail = False
                            , key = data.key
                            , url = data.url
                            }
                        , Browser.Navigation.pushUrl data.key
                            <| "/?lang=" ++ data.lang.lang
                            ++ (if data.dev then "&dev=true" else "")
                        )
                    Just (Ok user) ->
                        ( SelectLobby
                            { dev = data.dev
                            , lang = data.lang
                            , key = data.key
                            , url = data.url
                            , user = user
                            , token = Nothing
                            , model = LobbyInput.init data.dev
                            , loading = False
                            }
                        , Browser.Navigation.pushUrl data.key
                            <| "/lobby?lang=" ++ data.lang.lang
                            ++ (if data.dev then "&dev=true" else "")
                        )
        (WrapGuestInput _, _) -> (model, Cmd.none)
        (SelectLoginMode, SelectUser data) ->
            ( model
            , Browser.Navigation.load
                <| Url.toString
                <| Auth.makeAuthorizationUrl
                    { clientId = Config.oauthClientId
                    , redirectUri = 
                        redirectUri data.url data.lang data.dev
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
                , lang = data.lang
                , fail = True
                , key = data.key
                , url = data.url
                }
            , Browser.Navigation.pushUrl data.key
                <| "/?fail=true&lang=" ++ data.lang.lang
                ++ (if data.dev then "&dev=true" else "")
            )
        (GotAccessToken _, _) -> (model, Cmd.none)
        (GotUserInfo (Ok userinfo), OAuthLogin data) ->
            ( SelectLobby
                { dev = data.dev
                , lang = data.lang
                , key = data.key
                , url = data.url
                , user = userinfo
                , token = data.token
                , model = LobbyInput.init data.dev
                , loading = False
                }
            , Browser.Navigation.pushUrl data.key
                <| "/lobby?lang=" ++ data.lang.lang
                ++ (if data.dev then "&dev=true" else "")
            )
        (GotUserInfo (Err _), OAuthLogin data) ->
            ( SelectUser
                { dev = data.dev
                , lang = data.lang
                , fail = True
                , key = data.key
                , url = data.url
                }
            , Browser.Navigation.pushUrl data.key
                <| "/?fail=true&lang=" ++ data.lang.lang
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
                        { data 
                        | model = new
                        , loading = res /= Nothing
                        }
                    )
                <| case (res, data.token) of
                    (Nothing, _) ->
                        Cmd.none
                    (Just info, Nothing) ->
                        getGuestToken
                            info
                            data.user.username
                            data.user.picture
                            data.lang.lang
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
            GameMain.init
                token.userToken
                info.api
                data.lang.lang
                data.lang.info
                data.lang.root
                token.joinToken
            |> \(new, cmd) ->
                ( Game
                    { server = info
                    , user = Just data.user
                    , game = new
                    }
                , Cmd.batch
                    [ Browser.Navigation.pushUrl data.key
                        <| "/game/" ++ info.server ++ "/" ++ token.userToken
                            ++ "?lang=" ++ data.lang.lang
                    , Cmd.map WrapGame cmd
                    ]
                )
        (ReceiveLobbyToken info (Ok token), InitGame data) ->
            Tuple.mapBoth
                (\new ->
                    Game
                        { server = info
                        , user = Nothing
                        , game = new
                        }
                )
                (Cmd.map WrapGame)
            <| GameMain.init
                token.userToken
                info.api
                data.lang.lang
                data.lang.info
                data.lang.root
                token.joinToken
        (ReceiveLobbyToken _ _, _) -> (model, Cmd.none)
        (WrapGame sub, Game data) ->
            Tuple.mapBoth
                (\new ->
                    Game
                        { data | game = new }
                )
                (Cmd.map WrapGame)
            <| GameMain.update sub data.game
        (WrapGame _, _) -> (model, Cmd.none)
        (ReceiveLangInfo (Ok info), _) ->
            (setLang
                (getLang model
                    |> \lang ->
                        { lang
                        | info = info
                        }
                )
                model
            , Cmd.none
            )
        (ReceiveLangInfo (Err _), _) -> (model, Cmd.none)
        (ReceiveRootLang l (Ok root), _) ->
            (setLang
                (getLang model
                    |> \lang ->
                        { lang
                        | root = Dict.insert l root lang.root
                        }
                )
                model
            , Cmd.none
            )
        (ReceiveRootLang _ (Err _), _) -> (model, Cmd.none)

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
    case model of
        Game data ->
            Sub.map WrapGame
            <| GameMain.subscriptions data.game
        _ -> Sub.none
