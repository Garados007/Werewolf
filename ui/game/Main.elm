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
import Svg
import Svg.Attributes as SA
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
import Language.Config as LangConfig exposing (LangConfig)
import Network
import Network.NetworkManager
import Styles
import Storage exposing (Storage)
import Avatar
import Debounce

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
import Random
import Random.String
import Random.Char

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
                (if getNav model |> .dev
                    then (++) "(Dev) "
                    else identity
                )
                <| Maybe.withDefault "Werewolf"
                <| Language.getText
                    (LangConfig.getRootLang <| getLang model)
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
    , lang: LangConfig
    , storage: Storage
    , avatar: Avatar.AvatarStorage
    -- actual data
    , guest: GuestInput.Model
    }

type alias OAuthLoginData =
    -- common data
    { nav: NavOpts
    , lang: LangConfig
    , storage: Storage
    , avatar: Avatar.AvatarStorage
    -- actual data
    , token: Maybe Auth.AuthenticationSuccess
    }

type alias SelectLobbyData =
    -- common data
    { nav: NavOpts
    , lang: LangConfig
    , storage: Storage
    , avatar: Avatar.AvatarStorage
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
    { lang: LangConfig
    , serverId: String
    , lobbyId: String
    , lobbyUser: Maybe (UserInfo, Maybe AuthToken)
    , storage: Storage
    , avatar: Avatar.AvatarStorage
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
    | WrapAvatar Avatar.Msg
    | WrapAuthToken AuthToken.Msg
    | RemoveLostToken
    | Navigate Url
    | RemoveFallback
    | InitGuestCode String

getLang : Model -> LangConfig
getLang model =
    case model of
        SelectUser { lang } -> lang
        OAuthLogin { lang } -> lang
        SelectLobby { lang } -> lang
        Game { game } -> game.lang
        InitGame { lang } -> lang

setLang : LangConfig -> Model -> Model
setLang lang model =
    case model of
        SelectUser data -> SelectUser { data | lang = lang }
        OAuthLogin data -> OAuthLogin { data | lang = lang }
        SelectLobby data -> SelectLobby { data | lang = lang }
        Game data -> Game
            { data
            | game = data.game |> \game ->
                { game | lang = lang }
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

getAvatarStorage : Model -> Avatar.AvatarStorage
getAvatarStorage model =
    case model of
        SelectUser { avatar } -> avatar
        OAuthLogin { avatar } -> avatar
        SelectLobby { avatar } -> avatar
        Game { game } -> game.avatar
        InitGame { avatar } -> avatar

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

getNav : Model -> NavOpts
getNav model =
    case model of
        SelectUser { nav } -> nav
        OAuthLogin { nav } -> nav
        SelectLobby { nav } -> nav
        Game { nav } -> nav
        InitGame { nav } -> nav

setNav : NavOpts -> Model -> Model
setNav nav model =
    case model of
        SelectUser data -> SelectUser { data | nav = nav }
        OAuthLogin data -> OAuthLogin { data | nav = nav }
        SelectLobby data -> SelectLobby { data | nav = nav }
        Game data -> Game { data | nav = nav }
        InitGame data -> InitGame { data | nav = nav }


init : () -> Url -> Key -> (Model, Cmd Msg)
init () url key =
    let
        nav : NavOpts
        nav = parseNavOpts url key

        lang : LangConfig
        lang = LangConfig.init nav.lang

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
                    , Random.generate InitGuestCode
                        <| Random.String.rangeLengthString 4 12
                        <| Random.Char.latin
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
                            , avatar = Avatar.empty
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
                            , avatar = Avatar.empty
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
                            , avatar = Avatar.empty
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
                        , avatar = Avatar.empty
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
        lang : LangConfig
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

        avatar : Avatar.AvatarStorage
        avatar = getAvatarStorage model

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
                                        , avatar = avatar
                                        , token = token
                                        , user = user
                                        , model = LobbyInput.init nav_old.dev
                                        , loading = False
                                        , viewUser = viewUser
                                        }
                                    , Tuple.second Network.NetworkManager.exit
                                    )
                                Nothing ->
                                    ( SelectUser
                                        { nav = nav_old
                                        , lang = lang
                                        , guest = GuestInput.init storage
                                        , storage = storage
                                        , avatar = avatar
                                        }
                                    , Tuple.second Network.NetworkManager.exit
                                    )
                        InitGame _ ->
                            ( SelectUser
                                { nav = nav_old
                                , lang = lang
                                , guest = GuestInput.init storage
                                , storage = storage
                                , avatar = avatar
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
                            , avatar = avatar
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

    in  if hasChange
        then (setNav nav_new new, newCmd)
        else (setNav nav_new model, Cmd.none)

singleLangBlock : Model -> List String -> List (Html msg)
singleLangBlock model =
    List.singleton
    << singleLang model

singleLang : Model -> List String -> (Html msg)
singleLang model =
    Html.text
    << Language.getTextOrPath
        (LangConfig.getRootLang <| getLang model)

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

viewLangFallbackBanner : Model -> List (Layout.LayoutBanner Msg) -> List (Layout.LayoutBanner Msg)
viewLangFallbackBanner model list =
    case getNav model |> .langFallback of
        Just origin ->
            { closeable = Just RemoveFallback
            , content = Html.text
                <| Language.getTextFormatOrPath
                    (LangConfig.getRootLang <| getLang model)
                    [ "fallback" ]
                <| Dict.fromList
                    [ ("lang", origin) ]
            }
            :: list
        Nothing -> list

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
            <| Layout.view (LangConfig.getRootLang <| getLang model)
                { titleButtonsLeft = []
                , titleButtonsRight = []
                , titleText = Layout.LangLayoutText
                    [ "init", "title" ]
                , leftSection = Html.text ""
                , showLeftSection = Just False
                , banner = viewLangFallbackBanner model []
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
                        <| GuestInput.view data.avatar data.guest
                        <| LangConfig.getRootLang data.lang
                    , Views.ViewVersion.view
                    ]
                , bottomRightButton = Nothing
                }
        SelectLobby data -> List.singleton <|
            if data.loading || data.model.loading
            then viewLoading
            else Layout.view (LangConfig.getRootLang data.lang)
                { titleButtonsLeft = viewUserButtons data.viewUser
                , titleButtonsRight = []
                , titleText = Layout.LangLayoutText
                    [ "init", "title" ]
                , leftSection =
                    Html.map (always ResetUser)
                    <| Views.ViewUserPreview.view
                        (LangConfig.getRootLang data.lang)
                        data.avatar
                        (data.token == Nothing)
                        data.user
                , showLeftSection = data.viewUser
                , banner = viewLangFallbackBanner model
                    <| List.concat
                    [ if Maybe.map .lost data.token == Just True
                        then List.singleton
                            { closeable = Just RemoveLostToken
                            , content = Html.text
                                <| Language.getTextOrPath
                                    (LangConfig.getRootLang data.lang)
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
                        <| LangConfig.getRootLang data.lang
                    , Views.ViewVersion.view
                    ]
                , bottomRightButton = Nothing
                }
        Game data -> List.singleton <|
            if GameMain.isLoading data.game
            then viewLoading
            else Layout.view
                (Model.getLang data.game)
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
                , banner = viewLangFallbackBanner model
                    <| List.map (Layout.mapBanner WrapGame)
                    <| GameMain.viewBanner data.game
                , contentClass = ""
                , content = List.map (Html.map WrapGame)
                    <| GameMain.view data.game
                , bottomRightButton = Nothing
                }

        _ -> []

viewLoading : Html msg
viewLoading =
    Html.div [ HA.id "elm" ]
        [ Html.div [ HA.class "lds-loading" ]
            <| List.singleton
            <| Svg.svg [ SA.viewBox "0 0 400 400" ]
            <| List.singleton
            <| Svg.path
                [ SA.d "M187.719 9.388c-5.793 2.519-9.438 7.871-9.425 13.837.009 3.822.296 4.601 3.109 8.422.955 1.298 1.952 3.033 2.215 3.856 1.503 4.713 3.671 6.724 8.325 7.722 11.902 2.553 20.947-12.944 12.121-20.768-1.472-1.305-3.393-3.386-4.27-4.625-6.386-9.021-8.058-10.19-12.075-8.444M259.955 21.3c-1.927 2.62-2.45 4.182-2.782 8.308-.377 4.686-1.237 7.022-4.071 11.059-6.72 9.569-6.082 13.184 2.816 15.961 12.094 3.776 18.18-1.284 17.555-14.594-.55-11.709-3.017-18.609-7.592-21.233-3.017-1.73-4.371-1.616-5.926.499m-94.331 33.569c-5.619 2.079-20.028 22.336-29.257 41.131-13.68 27.86-13.083 34.99 4.686 56 8.799 10.403 10.774 11.2 27.758 11.2 11.973 0 13.55-.211 17.189-2.303 3.649-2.097 9.761-8.835 11.946-13.17 9.431-18.712 12.036-44.207 5.851-57.261-2.812-5.936-11.595-17.278-16.397-21.175-15.445-12.535-19.542-15.248-21.776-14.422M262.4 78.423c-10.832 1.601-22.299 10.997-28.212 23.116-4.105 8.415-5.084 10.767-6.515 15.661-7.04 24.073-6.442 40.162 1.811 48.648 2.786 2.865 6.728 5.572 12.644 8.683 5.373 2.825 10.88 6.269 15.996 10.004 12.483 9.115 16.518 8.955 23.595-.935 1.495-2.09 3.974-5.51 5.508-7.6 3.578-4.875 5.998-9.683 7.819-15.535 1.974-6.343 4.424-12.868 7.494-19.958 7.121-16.442 8.18-31.961 2.868-42.027-4.783-9.065-13.309-16.962-21.269-19.701-2.448-.842-16.854-1.078-21.739-.356m-160.295 2.816c-10.941 5.416-2.63 33.161 9.934 33.161 6.481 0 10.371-9.36 6.816-16.4-.5-.99-1.322-2.88-1.827-4.2-4.33-11.324-9.178-15.404-14.923-12.561M84.8 117.049c-14.618 2.845-23.658 25.978-22.49 57.551.468 12.634 1.625 22.629 3.196 27.6 2.788 8.826 14.994 19.804 30.92 27.81 17.489 8.791 26.99 8.381 32.663-1.41 6.21-10.717 7.318-22.205 3.416-35.4-2.156-7.287-2.534-9.842-2.899-19.6-.431-11.517-1.659-17.284-5.02-23.577-.536-1.003-1.807-3.713-2.825-6.023-5.795-13.153-6.581-14.537-9.806-17.272-8.479-7.19-19.592-11.151-27.155-9.679m233.807 6.373c-9.958 3.423-21.233 36.327-16.011 46.724 4.698 9.351 23.111 12.729 28.692 5.262 6.045-8.086 8.853-24.137 5.782-33.046-3.462-10.04-13.725-20.569-18.463-18.94M306.8 182.748c-6.358 1.338-17.681 5.161-23.852 8.052-6.621 3.103-8.977 4.889-16.948 12.851-7.423 7.415-10.44 10.259-15.223 14.349-9.188 7.859-17.329 18.104-19.388 24.4-2.343 7.162-.435 11.382 8.411 18.605 1.98 1.617 4.95 4.118 6.6 5.558 1.65 1.44 4.44 3.856 6.2 5.368a352.462 352.462 0 016.769 6.014c8.834 8.08 17.353 12.956 21.33 12.21 1.722-.323 4.101-2.101 8.705-6.507 1.982-1.896 5.21-4.798 7.174-6.448 23.659-19.879 32.8-32.549 35.275-48.895 1.091-7.204.999-17.299-.192-21.105-6.177-19.738-13.42-26.861-24.861-24.452m-116.6 39.898c-8.373.768-11.071 1.083-15.4 1.798-7.26 1.198-14.197 2.912-20.8 5.14-17.881 6.032-22.528 10.175-34.784 31.016-5.547 9.432-11.044 15.103-21.635 22.323-14.472 9.866-17.854 18.802-11.545 30.506 5.431 10.075 16.029 18.015 33.564 25.149 1.43.582 4.76 2.022 7.4 3.2 6.04 2.697 10.356 4.254 16.2 5.844 2.53.689 6.402 1.85 8.604 2.579 2.202.73 4.722 1.459 5.6 1.621 10.427 1.926 15.943 6.821 18.569 16.479 2.341 8.61 3.215 10.175 7.181 12.867 14.663 9.951 41.417 12.247 56.994 4.889 10.388-4.906 16.511-13.864 23.736-34.726l1.916-5.531.045-13c.034-9.702.212-14.16.7-17.575 2.287-15.98-3.173-23.572-18.145-25.229-13.26-1.468-20.025-4.927-24.242-12.396a232.948 232.948 0 00-3.287-5.542c-2.746-4.488-3.452-6.506-5.504-15.712-1.823-8.186-1.886-8.619-2.59-17.746-.758-9.825-2.01-12.988-5.92-14.964-1.454-.735-12.367-1.383-16.657-.99"
                ] []
        ]

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
            GuestInput.update data.avatar sub data.guest
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
                                , avatar = data.avatar
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
                , avatar = data.avatar
                }
            , Random.generate InitGuestCode
                <| Random.String.rangeLengthString 4 12
                <| Random.Char.latin
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
                                (\mail -> MD5.hex mail)
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
                , avatar = data.avatar
                }
            , Cmd.batch
                [ ncmd
                , Random.generate InitGuestCode
                    <| Random.String.rangeLengthString 4 12
                    <| Random.Char.latin
                ]
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
                , avatar = data.avatar
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
                , avatar = data.avatar
                }
            , Cmd.batch
                [ ncmd
                , Random.generate InitGuestCode
                    <| Random.String.rangeLengthString 4 12
                    <| Random.Char.latin
                ]
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
                data.lang
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
                data.lang
                token.joinToken
                data.storage
        (ReceiveLobbyToken _ _, _) -> (model, Cmd.none)
        (WrapGame sub, Game data) ->
            case sub of
                GameMain.Return -> returnGame data
                _ ->
                    (\(new, cmd) ->
                        if new.lang.lang /= data.game.lang.lang
                        then data.nav
                            |> \old_nav -> navigateTo
                                { old_nav
                                | lang = new.lang.lang
                                , langFallback = Nothing
                                }
                                old_nav.url.path
                            |> \(nav, ncmd) ->
                                ( Game
                                    { data
                                    | game = new
                                    , nav = nav
                                    }
                                , Cmd.batch
                                    [ Cmd.map WrapGame cmd
                                    , ncmd
                                    ]
                                )
                        else
                            ( Game
                                { data | game = new }
                            , Cmd.map WrapGame cmd
                            )
                    )
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
                , Storage.get .guestName storage
                    |> Maybe.map
                        (Avatar.request <| getAvatarStorage newModel)
                    |> Maybe.withDefault Cmd.none
                )
        (WrapAvatar sub, _) ->
            getAvatarStorage model
            |> Avatar.update sub
            |> \avatar ->
                (case model of
                    SelectUser data ->
                        SelectUser { data | avatar = avatar }
                    OAuthLogin data ->
                        OAuthLogin { data | avatar = avatar }
                    SelectLobby data ->
                        SelectLobby { data | avatar = avatar }
                    InitGame data ->
                        InitGame { data | avatar = avatar }
                    Game data ->
                        Game
                            { data
                            | game = data.game |> \game ->
                                { game | avatar = avatar }
                            }
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
        (RemoveFallback, _) ->
            getNav model
            |> \old_nav ->
                navigateTo
                    { old_nav | langFallback = Nothing }
                    old_nav.url.path
            |> \(nav, ncmd) ->
                (setNav nav model, ncmd)
        (InitGuestCode code, SelectUser data) ->
            ( SelectUser
                { data
                | guest = data.guest |> \guest ->
                    { guest | email = code, debouncer = Debounce.init 250 code  }
                }
            , Avatar.request data.avatar code
            )
        (InitGuestCode _, _) -> (model, Cmd.none)

returnGame : GameData -> (Model, Cmd Msg)
returnGame data =
    navigateTo data.nav "/"
    |> \(nav, ncmd) ->
        (case data.lobbyUser of
            Just (user, token) ->
                Avatar.requireList data.game.avatar
                    (if String.startsWith "@" user.picture
                        then [ String.dropLeft 1 user.picture ]
                        else []
                    )
                |> Tuple.mapFirst
                    (\avatar ->
                        SelectLobby
                            { nav = nav
                            , lang = data.game.lang
                            , storage = data.game.storage
                            , avatar = avatar
                            , token = token
                            , user = user
                            , model = LobbyInput.init nav.dev
                            , loading = False
                            , viewUser = data.viewUser
                            }
                    )
            Nothing ->
                ( SelectUser
                    { nav = nav
                    , lang = data.game.lang
                    , guest = GuestInput.init data.game.storage
                    , storage = data.game.storage
                    , avatar = data.game.avatar
                    }
                , Random.generate InitGuestCode
                    <| Random.String.rangeLengthString 4 12
                    <| Random.Char.latin
                )
        )
    |> Tuple.mapSecond
        (\cmd -> Cmd.batch [ Tuple.second Network.NetworkManager.exit, ncmd, cmd ])

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
        , Sub.map WrapAvatar Avatar.subscriptions
        ]
