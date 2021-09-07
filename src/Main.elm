module Main exposing (..)

import Main.Tools exposing (..)

import Browser
import Browser.Navigation exposing (Key)
import Url exposing (Url)
import Url.Parser exposing ((</>))
import Maybe.Extra
import Dict
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
import Styles
import Storage exposing (Storage)

import Model
import GameMain
import Html.Attributes exposing (lang)
import Views.ViewVersion
import Views.ViewLayout as Layout
import Views.ViewUserPreview
import Views.Icons
import Data exposing (Game)
import Time
import AuthToken exposing (AuthToken)

{-| Large parts of the former Main.elm are moved now to GameMain.elm. Main.elm gets a whole new 
purpose and setup routines.

Main has now the responsibility to enable login and joining/creating games. This is a functionality
that didn't exists in the old Game Page.

Now there exists the following states which will cycle and iterate through depending on the user
choises:

- `SelectUser`: The user has the option to login and transition to `OAuthLogin` or enter the
    guest information and transition to `SelectLobby`.

    Path: `/?dev=*` (or any invalid path)
- `OAuthLogin`: A full OAuth process will be triggered. If it succeeds we have our access token and 
    refresh token. Both of them has to be updated regulary. After that it will transition to
    `SelectLobby`.

    A failed Login will transition to `SelectUser` with an addition url option `?fail=true`.

    Path: `/?dev=*`
- `SelectLobby`: The user has to choose between creating and joining a lobby.

    If the user chooses to create a lobby the page will connect to pronto and fetch a new game 
    server. After that it will transition to `Game` with the returned credentials.

    If the user  chooses to join a lobby the page will ask for a short join code and asking pronto
    for the server. After that it will transition to `Game`.

    Path: `/?dev=*`
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
            { title = 
                Maybe.withDefault "Werewolf"
                <| Language.getText
                    (getRootLang <| getLang model)
                    [ "init", "title" ]
            , body = view model
            }
        , update = update
        , subscriptions = subscriptions
        , onUrlChange = Navigate
        , onUrlRequest = \request ->
            case request of
                Browser.Internal url -> Navigate url
                Browser.External _ -> Noop
        }

type alias SelectUserData = 
    -- common data
    { nav: NavOpts
    , lang: LangContainer
    , storage: Storage
    -- actual data
    , guest: GuestInput.Model
    }

type alias OAuthLoginData =
    -- common data
    { nav: NavOpts
    , lang: LangContainer
    , storage: Storage
    -- actual data
    , token: Maybe Auth.AuthenticationSuccess
    }

type alias SelectLobbyData =
    -- common data
    { nav: NavOpts
    , lang: LangContainer
    , storage: Storage
    -- actual data
    , token: Maybe AuthToken
    , user: UserInfo
    , model: LobbyInput.Model
    , loading: Bool
    , viewUser: Maybe Bool
    }

type alias GameData =
    { server: LobbyInput.ConnectInfo
    , lobbyUser: Maybe (UserInfo, Maybe AuthToken)
    , game: Model.Model
    , viewUser: Maybe Bool
    , nav: NavOpts
    }

type alias InitGameData =
    { lang: LangContainer
    , serverId: String
    , lobbyId: String
    , lobbyUser: Maybe (UserInfo, Maybe AuthToken)
    , storage: Storage
    , nav: NavOpts
    }

type Model
    = SelectUser SelectUserData
    | OAuthLogin OAuthLoginData
    | SelectLobby SelectLobbyData
    | Game GameData
    | InitGame InitGameData

type Msg
    = Noop
    | SelectLoginMode
    | ResetUser
    | ViewUser Bool
    | GotAccessToken (Result Http.Error Auth.AuthenticationSuccess)
    | GotUserInfo (Result Http.Error UserInfo)
    | ReceiveGuestToken LobbyInput.ConnectInfo (Result Http.Error String)
    | ReceiveLobbyToken LobbyInput.ConnectInfo (Result Http.Error LobbyJoinInfo)
    | ReceiveLangInfo (Result Http.Error Language.LanguageInfo)
    | ReceiveRootLang String (Result Http.Error Language.Language)
    | WrapGuestInput GuestInput.Msg
    | WrapLobbyInput LobbyInput.Msg
    | WrapGame GameMain.Msg
    | WrapStorage Storage.Msg
    | WrapAuthToken AuthToken.Msg
    | RemoveLostToken
    | Navigate Url

getLang : Model -> LangContainer
getLang model =
    case model of
        SelectUser { lang } -> lang
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

getStorage : Model -> Storage
getStorage model =
    case model of
        SelectUser { storage } -> storage
        OAuthLogin { storage } -> storage
        SelectLobby { storage } -> storage
        Game { game } -> game.storage
        InitGame { storage } -> storage

setStorage : Storage -> Model -> Model
setStorage storage model =
    case model of
        SelectUser data -> SelectUser { data | storage = storage }
        OAuthLogin data -> OAuthLogin { data | storage = storage }
        SelectLobby data -> SelectLobby { data | storage = storage }
        Game data -> Game 
            { data 
            | game = data.game |> \game ->
                { game
                | storage = storage
                }
            }
        InitGame data -> InitGame { data | storage = storage }

init : () -> Url -> Key -> (Model, Cmd Msg)
init () url key =
    let
        nav : NavOpts
        nav = parseNavOpts url key

        lang : LangContainer
        lang = 
            { lang = nav.lang
            , info =
                { languages = Dict.empty
                , icons = Dict.empty
                , themes = Dict.empty
                }
            , root = Dict.empty
            }
        
        (storage, storageCmd) = Storage.init
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
                    , Cmd.map WrapStorage storageCmd
                    ]
            )
        <| Maybe.Extra.unpack
            (\() ->
                case Auth.parseCode url of
                    Auth.Empty ->
                        navigateTo (navFail False nav) "/"
                        |> \(nnav, ncmd) ->
                        ( SelectUser
                            { nav = nnav
                            , lang = lang
                            , guest = GuestInput.init storage
                            , storage = storage
                            }
                        , ncmd
                        )
                    Auth.Error _ ->
                        navigateTo (navFail True nav) "/"
                        |> \(nnav, ncmd) ->
                        ( SelectUser
                            { nav = nnav
                            , lang = lang
                            , guest = GuestInput.init storage
                            , storage = storage
                            }
                        , ncmd
                        )
                    Auth.Success { code } ->
                        navigateTo (navFail False nav) "/"
                        |> \(nnav, ncmd) ->
                        ( OAuthLogin
                            { nav = nnav
                            , lang = lang
                            , token = Nothing
                            , storage = storage
                            }
                        , Cmd.batch
                            [ Http.request <|
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
                                    , redirectUri = redirectUri nav
                                    }
                            , ncmd
                            ]
                        )
            )
            identity
        <| Url.Parser.parse
            ( Url.Parser.s "game" </> Url.Parser.string </> Url.Parser.string
                |> Url.Parser.map
                (\serverId lobbyId ->
                    navigateTo nav
                        ("/game/" ++ serverId ++ "/" ++ lobbyId)
                    |> \(nnav, ncmd) ->
                    ( InitGame
                        { lang = lang
                        , serverId = serverId
                        , lobbyId = lobbyId
                        , lobbyUser = Nothing
                        , storage = storage
                        , nav = nnav
                        }
                    , Cmd.batch 
                        [ initGame serverId lobbyId
                        , ncmd
                        ]
                    )
                )
            )
            url

initGame : String -> String -> Cmd Msg
initGame serverId lobbyId =
    Cmd.map
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

urlChange : Model -> Url -> (Model, Cmd Msg)
urlChange model url =
    let
        lang : LangContainer
        lang = getLang model

        nav_old : NavOpts
        nav_old = case model of
            SelectUser data -> data.nav
            OAuthLogin data -> data.nav
            SelectLobby data -> data.nav
            Game data -> data.nav
            InitGame data -> data.nav

        storage : Storage
        storage = getStorage model

        (new, newCmd) =
            Maybe.Extra.unpack
                (\() ->
                    -- navigate to user and lobby selection
                    case model of
                        SelectUser _ -> (model, Cmd.none)
                        OAuthLogin _ -> (model, Cmd.none)
                        SelectLobby _ -> (model, Cmd.none)
                        Game { lobbyUser, viewUser } ->
                            case lobbyUser of
                                Just (user, token) ->
                                    ( SelectLobby
                                        { nav = nav_old
                                        , lang = lang
                                        , storage = storage
                                        , token = token
                                        , user = user
                                        , model = LobbyInput.init nav_old.dev
                                        , loading = False
                                        , viewUser = viewUser
                                        }
                                    , Network.wsExit
                                    )
                                Nothing ->
                                    ( SelectUser
                                        { nav = nav_old
                                        , lang = lang
                                        , guest = GuestInput.init storage
                                        , storage = storage
                                        }
                                    , Network.wsExit
                                    )
                        InitGame _ ->
                            ( SelectUser
                                { nav = nav_old
                                , lang = lang
                                , guest = GuestInput.init storage
                                , storage = storage
                                }
                            , Cmd.none
                            )
                )
                identity
            <| Url.Parser.parse
                (Url.Parser.s "game" </> Url.Parser.string </> Url.Parser.string
                |> Url.Parser.map
                    (\serverId lobbyId ->
                        -- navigate to game
                        (InitGame
                            { lang = lang
                            , serverId = serverId
                            , lobbyId = lobbyId
                            , lobbyUser = case model of
                                SelectLobby data ->
                                    Just (data.user, data.token)
                                Game data -> data.lobbyUser
                                InitGame data -> data.lobbyUser
                                _ -> Nothing
                            , storage = storage
                            , nav = nav_old
                            }
                        , initGame serverId lobbyId
                        )
                    )
                )
                url

        nav_new : NavOpts
        nav_new = 
            (\nav ->
                if nav_old.url /= nav.url
                then nav
                else { nav | url = url }
            )
            <| case new of
                SelectUser data -> data.nav
                OAuthLogin data -> data.nav
                SelectLobby data -> data.nav
                Game data -> data.nav
                InitGame data -> data.nav

        hasChange : Bool
        hasChange = nav_old.url /= nav_new.url

        setNav : Model -> Model
        setNav mod =
            case mod of
                SelectUser data ->
                    SelectUser { data | nav = nav_new }
                OAuthLogin data ->
                    OAuthLogin { data | nav = nav_new }
                SelectLobby data ->
                    SelectLobby { data | nav = nav_new }
                Game data ->
                    Game { data | nav = nav_new }
                InitGame data ->
                    InitGame { data | nav = nav_new }

    in  if hasChange
        then (setNav new, newCmd)
        else (setNav model, Cmd.none)

singleLangBlock : Model -> List String -> List (Html msg)
singleLangBlock model =
    List.singleton
    << singleLang model

singleLang : Model -> List String -> (Html msg)
singleLang model = 
    Html.text
    << Language.getTextOrPath
        (getRootLang <| getLang model)

viewUserButtons : Maybe Bool -> List (Layout.LayoutButton Msg)
viewUserButtons viewUsers =
    case viewUsers of
        Nothing ->
            [ Layout.LayoutButton
                (ViewUser True)
                (Layout.LayoutImageSvg Views.Icons.svgUsers)
                (Layout.StaticLayoutText "Show User")
                [ "view-layout-left-show", "view-layout-left-auto" ]
            , Layout.LayoutButton
                (ViewUser False)
                (Layout.LayoutImageSvg Views.Icons.svgUsers)
                (Layout.StaticLayoutText "Hide User")
                [ "view-layout-left-hide", "view-layout-left-auto" ]
            ]
        Just True ->
            [ Layout.LayoutButton
                (ViewUser False)
                (Layout.LayoutImageSvg Views.Icons.svgUsers)
                (Layout.StaticLayoutText "Hide User")
                [ "view-layout-left-hide" ]
            ]
        Just False ->
            [ Layout.LayoutButton
                (ViewUser True)
                (Layout.LayoutImageSvg Views.Icons.svgUsers)
                (Layout.StaticLayoutText "Show User")
                [ "view-layout-left-show" ]
            ]

view : Model -> List (Html Msg)
view model =
--!BEGIN
    (\l -> l ++ [ Debug.Extra.viewModel model ]) <|
--!END
    ((++)
        [ Html.node "link"
            [ HA.attribute "rel" "stylesheet"
            , HA.attribute "property" "stylesheet"
            , HA.attribute "href" 
                <| Network.versionUrl "/content/css/style.css"
            ] []
        , Styles.view
            (case model of
                Game data -> data.game.now
                _ -> Time.millisToPosix 0
            )
            (case model of
                Game data -> data.game.styles
                _ -> Styles.init
            )
        ]
    )
    <| case model of
        SelectUser data ->
            List.singleton
            <| Layout.view (getRootLang <| getLang model)
                { titleButtonsLeft = []
                , titleButtonsRight = []
                , titleText = Layout.LangLayoutText
                    [ "init", "title" ]
                , leftSection = Html.text ""
                , showLeftSection = Just False
                , banner = []
                , contentClass = "init-select-user"
                , content =
                    [ Html.h3 []
                        <| singleLangBlock model
                            [ "init", "user-mode", "play-login" ]
                    , Html.div [ HA.class "login-button" ]
                        <| List.singleton
                        <| Html.button
                            [ HE.onClick SelectLoginMode ]
                        <| singleLangBlock model
                            [ "init", "user-mode", "continue-login" ]
                    , Html.h3 []
                        <| singleLangBlock model
                            [ "init", "user-mode", "play-guest" ]
                    , Html.map WrapGuestInput
                        <| GuestInput.view data.guest 
                        <| getRootLang data.lang
                    , Views.ViewVersion.view
                    ]
                , bottomRightButton = Nothing
                }
        SelectLobby data -> List.singleton <|
            if data.loading || data.model.loading
            then Html.div [ HA.id "elm" ]
                [ Html.div [ HA.class "lds-heart" ]
                    [ Html.div [] [] ]
                ]
            else Layout.view (getRootLang data.lang)
                { titleButtonsLeft = viewUserButtons data.viewUser
                , titleButtonsRight = []
                , titleText = Layout.LangLayoutText
                    [ "init", "title" ]
                , leftSection =
                    Html.map (always ResetUser)
                    <| Views.ViewUserPreview.view
                        (getRootLang data.lang)
                        (data.token == Nothing)
                        data.user
                , showLeftSection = data.viewUser
                , banner = List.concat
                    [ if Maybe.map .lost data.token == Just True
                        then List.singleton
                            { closeable = Just RemoveLostToken
                            , content = Html.text
                                <| Language.getTextOrPath
                                    (getRootLang data.lang)
                                    [ "init", "lost-token" ]
                            }
                        else []
                    , List.map (Layout.mapBanner WrapLobbyInput)
                        <| LobbyInput.viewBanner data.model
                    ]
                , contentClass = "init-select-user"
                , content =
                    [ Html.map WrapLobbyInput
                        <| LobbyInput.view data.model
                            (Maybe.map .lost data.token /= Just True)
                        <| getRootLang data.lang
                    , Views.ViewVersion.view
                    ]
                , bottomRightButton = Nothing
                }
        Game data -> List.singleton <|
            if GameMain.isLoading data.game
            then Html.div [ HA.id "elm" ]
                [ Html.div [ HA.class "lds-heart" ]
                    [ Html.div [] [] ]
                ]
            else Layout.view (Model.getLanguage data.game)
                { titleButtonsLeft =
                    (\x -> x ++ viewUserButtons data.viewUser)
                    <| List.map (Layout.mapButton WrapGame)
                    <| GameMain.viewTopLeftButtons data.game
                , titleButtonsRight =
                    List.map (Layout.mapButton WrapGame)
                    <| GameMain.viewTopRightButtons data.game
                , titleText = GameMain.viewTitle data.game
                , leftSection =
                    Html.map WrapGame
                    <| GameMain.viewLeftSection data.game
                , showLeftSection = data.viewUser
                , banner =
                    List.map (Layout.mapBanner WrapGame)
                    <| GameMain.viewBanner data.game
                , contentClass = ""
                , content = List.map (Html.map WrapGame)
                    <| GameMain.view data.game
                , bottomRightButton = Nothing
                }
            
        _ -> []

update : Msg -> Model -> (Model, Cmd Msg)
update msg model =
    case (msg, model) of
        (Noop, _) -> (model, Cmd.none)
        (ViewUser viewUser, SelectLobby data) ->
            ( SelectLobby { data | viewUser = Just viewUser }
            , Cmd.none
            )
        (ViewUser viewUser, Game data) ->
            ( Game { data | viewUser = Just viewUser }
            , Cmd.none
            )
        (ViewUser _, _) -> (model, Cmd.none)
        (WrapGuestInput sub, SelectUser data) ->
            GuestInput.update sub data.guest
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
                        ( SelectUser
                            { data
                            | guest = new
                            }
                        , Cmd.none
                        )
                    Just user ->
                        Storage.set 
                            (\x -> { x | guestName = Just user.username })
                            data.storage
                        |> \(storage, storageCmd) ->
                            ( SelectLobby
                                { nav = data.nav
                                , lang = data.lang
                                , user = user
                                , token = Nothing
                                , model = LobbyInput.init data.nav.dev
                                , loading = False
                                , viewUser = Nothing
                                , storage = storage
                                }
                            , storageCmd
                            )
        (WrapGuestInput _, _) -> (model, Cmd.none)
        (SelectLoginMode, SelectUser data) ->
            ( model
            , Browser.Navigation.load
                <| Url.toString
                <| Auth.makeAuthorizationUrl
                    { clientId = Config.oauthClientId
                    , redirectUri = 
                        redirectUri data.nav
                    , scope = [ "openid", "profile" ]
                    , state = Nothing
                    , url =
                        { oauthBaseServerUrl
                        | path = Config.oauthAuthorizationEndpoint
                        }
                    }
            )
        (SelectLoginMode, _) -> (model, Cmd.none)
        (ResetUser, SelectLobby data) ->
            ( SelectUser
                { nav = data.nav
                , guest = GuestInput.init data.storage
                , lang = data.lang
                , storage = data.storage
                }
            , Cmd.none
            )
        (ResetUser, _) -> (model, Cmd.none)
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
                        ( JD.oneOf
                            [ JD.field Config.oauthUsernameMap JD.string
                            , JD.field Config.oauthUsernameDefaultMap JD.string
                            ]
                        )
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
            navigateTo (navFail True data.nav) "/"
            |> \(nnav, ncmd) ->
            ( SelectUser
                { nav = nnav
                , lang = data.lang
                , guest = GuestInput.init data.storage
                , storage = data.storage
                }
            , ncmd
            )
        (GotAccessToken _, _) -> (model, Cmd.none)
        (GotUserInfo (Ok userinfo), OAuthLogin data) ->
            ( SelectLobby
                { nav = data.nav
                , lang = data.lang
                , user = userinfo
                , token = Maybe.map AuthToken.init data.token
                , model = LobbyInput.init data.nav.dev
                , loading = False
                , viewUser = Nothing
                , storage = data.storage
                }
            , Cmd.none
            )
        (GotUserInfo (Err _), OAuthLogin data) ->
            navigateTo (navFail True data.nav) "/"
            |> \(nav, ncmd) ->
            ( SelectUser
                { nav = nav
                , lang = data.lang
                , guest = GuestInput.init data.storage
                , storage = data.storage
                }
            , ncmd
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
                            (OAuth.tokenToString token.token.token
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
                data.storage
            |> \(new, cmd) ->
                navigateTo (navFail False data.nav)
                    ("/game/" ++ info.server ++ "/" ++ token.userToken)
            |> \(nav, ncmd) ->
                ( Game
                    { server = info
                    , lobbyUser = Just (data.user, data.token)
                    , game = new
                    , viewUser = data.viewUser
                    , nav = nav
                    }
                , Cmd.batch
                    [ ncmd
                    , Cmd.map WrapGame cmd
                    ]
                )
        (ReceiveLobbyToken info (Ok token), InitGame data) ->
            Tuple.mapBoth
                (\new ->
                    Game
                        { server = info
                        , lobbyUser = data.lobbyUser
                        , game = new
                        , viewUser = Nothing
                        , nav = data.nav
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
                data.storage
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
        (ReceiveRootLang "de" (Err _), _) -> (model, Cmd.none)
        (ReceiveRootLang l (Err _), _) ->
            Tuple.pair model
            <| Browser.Navigation.load 
            <| "?lang=de&lang-fallback-for=" ++ Url.percentEncode l
        (WrapStorage sub, _) ->
            getStorage model
            |> Storage.update sub
            |> \(storage, key) ->
                (case (key, model) of
                    (Just Storage.StorageGuestName, SelectUser data) ->
                        SelectUser
                            { data
                            | guest = data.guest |> \guest ->
                                { guest
                                | name =
                                    if guest.name == ""
                                    then Storage.get .guestName storage
                                        |> Maybe.withDefault ""
                                    else guest.name
                                }
                            }
                    (Just Storage.StorageStreamerMode, Game data) ->
                        Game
                            { data
                            | game = data.game |> \game ->
                                { game
                                | streamerMode = Storage.get .streamerMode storage
                                    |> Maybe.withDefault game.streamerMode
                                }
                            }
                    _ -> model
                )
            |> \newModel ->
                ( setStorage storage newModel
                , Cmd.none
                )
        (WrapAuthToken sub, SelectLobby data) ->
            case data.token of
                Just auth ->
                    AuthToken.update sub auth
                    |> \(new, cmd) ->
                        (SelectLobby
                            { data | token = Just new }
                        , Cmd.map WrapAuthToken cmd
                        )
                Nothing -> (model, Cmd.none)
        (WrapAuthToken sub, Game data) ->
            case data.lobbyUser of
                Just (user, Just auth) ->
                    AuthToken.update sub auth
                    |> \(new, cmd) ->
                        (Game
                            { data 
                            | lobbyUser = Just (user, Just new)
                            }
                        , Cmd.map WrapAuthToken cmd
                        )
                _ -> (model, Cmd.none)
        (WrapAuthToken _, _) -> (model, Cmd.none)
        (RemoveLostToken, SelectLobby data) ->
            (SelectLobby { data | token = Nothing }
            , Cmd.none
            )
        (RemoveLostToken, _) -> (model, Cmd.none)
        (Navigate url, _) -> urlChange model url

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
    Sub.batch
        [ case model of
            SelectLobby { token } ->
                Maybe.map AuthToken.subscriptions token
                |> Maybe.withDefault Sub.none
                |> Sub.map WrapAuthToken
            Game data ->
                Sub.batch
                    [ Sub.map WrapGame
                        <| GameMain.subscriptions data.game
                    , Maybe.andThen Tuple.second data.lobbyUser
                        |> Maybe.map AuthToken.subscriptions
                        |> Maybe.withDefault Sub.none
                        |> Sub.map WrapAuthToken
                    ]
            _ -> Sub.none
        , Sub.map WrapStorage Storage.subscriptions
        ]
