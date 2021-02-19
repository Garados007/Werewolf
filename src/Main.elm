module Main exposing (..)

import Data
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

import Browser
import Html exposing (Html, div, text)
import Html.Attributes as HA exposing (class)
import Html.Lazy as HL
import Url exposing (Url)
import Url.Parser exposing ((</>))
import Task
import Debug.Extra
import Time exposing (Posix)
import Maybe.Extra
import Dict
import Views.ViewGamePhase
import Maybe.Extra
import Color exposing (Color)
import Color.Accessibility as CA
import Color.Convert as CC
import Color.Manipulate as CM
import Regex
import Level

type Msg
    = Response NetworkResponse
    | SetUrl Url
    | SetTime Posix
    | FetchData
    | Noop
    | Init
    | WrapUser Views.ViewUserList.Msg
    | WrapEditor Views.ViewRoomEditor.Msg
    | WrapPhase Views.ViewGamePhase.Msg
    | WrapError Int
    | WrapSelectModal Model.Modal
    | WrapThemeEditor Views.ViewThemeEditor.Msg
    | CloseModal

main : Program () Model Msg
main = Browser.application
    { init = \() url key ->
        ( Model.init
            (getId url |> Maybe.withDefault "")
            key
        , Task.perform identity
            <| Task.succeed Init
        )
    , view = \model ->
        { title = "Werwolf"
        , body = view model
            <| Model.getLanguage model
        }
    , update = update
    , subscriptions = subscriptions
    , onUrlRequest = \rurl ->
        case rurl of
            Browser.Internal url ->
                SetUrl url
            Browser.External _ ->
                Noop
    , onUrlChange = SetUrl
    }

view : Model -> Language -> List (Html Msg)
view model lang =
    [ Html.node "link"
        [ HA.attribute "rel" "stylesheet"
        , HA.attribute "property" "stylesheet"
        , HA.attribute "href" "/content/games/werwolf/css/style.css"
        ] []
    , viewStyles
        <| Maybe.withDefault model.bufferedConfig
        <| Maybe.Extra.orElse
            ( Maybe.andThen .userConfig model.game)
        <| case model.modal of
            Model.SettingsModal conf ->
                Just conf.config
            _ -> Nothing
    , tryViewGamePhase model lang
        |> Maybe.Extra.orElseLazy
            (\() -> tryViewGameFrame model lang)
        |> Maybe.Extra.orElseLazy
            (\() -> Just
                <| Views.ViewNoGame.view lang
                <| model.game == Nothing || model.roles == Nothing
            )
        |> Maybe.withDefault (text "")
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
    , Views.ViewErrors.view model.errors
        |> Html.map WrapError
    , Debug.Extra.viewModel model
    ]

tryViewGameFrame : Model -> Language -> Maybe (Html Msg)
tryViewGameFrame model lang =
    Maybe.Extra.andThen2
        (\result roles ->
            Maybe.map2
                (viewGameFrame model lang roles)
                result.game
                result.user
        )
        model.game
        model.roles

viewGameFrame : Model
    -> Language
    -> Data.RoleTemplates
    -> Data.Game
    -> String
    -> Html Msg
viewGameFrame model lang roles game user =
    div [ class "frame-game-outer" ]
        [ div [ class "frame-game-left" ]
            [ Html.map WrapUser
                <| Views.ViewUserList.view
                    lang
                    model.now model.levels
                    model.token game user
            ]
        , div [ class "frame-game-body" ]
            [ Html.map WrapSelectModal
                <| Views.ViewSettingsBar.view model
            , Html.map WrapEditor
                <| Views.ViewRoomEditor.view
                    lang
                    roles
                    model.theme
                    game
                    (user == game.leader)
                    model.editor
            ]
        ]

tryViewGamePhase : Model -> Language -> Maybe (Html Msg)
tryViewGamePhase model lang =
    Maybe.andThen
        (\result ->
            Maybe.Extra.andThen2
                (\game user ->
                    Maybe.map
                        (\phase -> 
                            viewGamePhase model lang game user phase
                        )
                        game.phase
                )
                result.game
                result.user
        )
        model.game

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
                    model.token game user
            ]
        , div [ class "frame-game-body", class "top" ]
            [ Html.map WrapSelectModal
                <| Views.ViewSettingsBar.view model
            , Html.map WrapPhase
                <| Views.ViewGamePhase.view
                    lang
                    model.now
                    model.token
                    game
                    phase
                    (user == game.leader)
                    user
            ]
        ]

viewStyles : Data.UserConfig -> Html msg
viewStyles = HL.lazy <| \config ->
    let
        colorBase : Color
        colorBase = CC.hexToColor config.theme
            |> Result.toMaybe
            |> Maybe.withDefault Color.white
        
        isDark : Bool
        isDark = CA.luminance colorBase <= 0.5

        darken : Float -> Color -> Color
        darken = if isDark then CM.lighten else CM.darken

        colorBackground : Color
        colorBackground = colorBase

        textColor : Color
        textColor = if isDark then Color.white else Color.black
        
        textColorLight : Color
        textColorLight = CM.weightedMix
            colorBase
            textColor
            0.375
        
        textInvColor : Color
        textInvColor = if isDark then Color.black else Color.white
        
        colorLight : Color
        colorLight = darken 0.20 colorBase

        colorMedium : Color
        colorMedium = darken 0.30 colorBase

        colorDark : Color
        colorDark = darken 0.40 colorBase

        colorDarker : Color
        colorDarker = darken 0.50 colorBase

        build : String -> Regex.Regex
        build = Regex.fromString >> Maybe.withDefault Regex.never

    in div [ class "styles" ]
        [ Html.node "style"
            [ HA.rel "stylesheet" ]
            <| List.singleton
            <| text
            <| (\style -> 
                    ":root { " ++ style ++ "; --bg-url: url(\"" ++
                    (config.background 
                        |> Regex.replace (build "\\s") (always "")
                        |> Regex.replace (build "\\\\") (always "\\\\")
                        |> Regex.replace (build "\"") (always "\\\"")
                    )
                    ++ "\"); }" 
                )
            <| String.concat
            <| List.intersperse "; "
            <| List.map
                (\(rule, color) ->
                    "--" ++ rule ++ ": " ++ CC.colorToCssRgba color
                )
                [ ("color-base", colorBase)
                , ("color-background", colorBackground)
                , ("text-color", textColor)
                , ("text-color-light", textColorLight)
                , ("text-inv-color", textInvColor)
                , ("color-light", colorLight)
                , ("color-light-transparent", CM.fadeOut 0.4 colorLight)
                , ("color-medium", colorMedium)
                , ("color-dark", colorDark)
                , ("color-dark-transparent", CM.fadeOut 0.4 colorLight)
                , ("color-darker", colorDarker)
                , ("color-darker-transparent", CM.fadeOut 0.4 colorLight)
                ]
        , div [ class "background" ] []
        ]
    

update : Msg -> Model -> (Model, Cmd Msg)
update msg model =
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
        SetUrl url ->
            Tuple.pair
                { model
                | token = getId url
                    |> Maybe.withDefault model.token
                }
                <| Task.perform identity
                <| Task.succeed Init
        Noop -> (model, Cmd.none)
        Init ->
            Tuple.pair model
                <| Cmd.map Response
                <| Cmd.batch
                    [ Network.executeRequest
                        <| Network.GetGame model.token
                    , Network.executeRequest
                        <| Network.GetRoles
                    , Network.executeRequest
                        <| Network.GetLangInfo
                    ]
        SetTime now ->
            Tuple.pair
                { model | now = now }
                Cmd.none
        FetchData ->
            Tuple.pair model
                <| Cmd.map Response
                <| Network.executeRequest
                <| Network.GetGame model.token
        WrapUser (Views.ViewUserList.Send req) ->
            Tuple.pair model
                <| Cmd.map Response
                <| Network.executeRequest req
        WrapEditor (Views.ViewRoomEditor.SetBuffer buffer req) ->
            Tuple.pair
                { model | editor = buffer }
                <| Cmd.map Response
                <| Network.executeRequest
                <| Network.PostGameConfig model.token req
        WrapEditor (Views.ViewRoomEditor.SendConf req) ->
            Tuple.pair model
                <| Cmd.map Response
                <| Network.executeRequest
                <| Network.PostGameConfig model.token req
        WrapEditor Views.ViewRoomEditor.StartGame ->
            Tuple.pair model
                <| Cmd.map Response
                <| Network.executeRequest
                <| Network.GetGameStart model.token
        WrapEditor Views.ViewRoomEditor.Noop ->
            Tuple.pair model Cmd.none
        WrapPhase Views.ViewGamePhase.Noop ->
            Tuple.pair model Cmd.none
        WrapPhase (Views.ViewGamePhase.Send req) ->
            Tuple.pair model
                <| Cmd.map Response
                <| Network.executeRequest req
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
        WrapSelectModal modal ->
            Tuple.pair { model | modal = modal } Cmd.none
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
                            ( Cmd.map Response
                                << Network.executeRequest
                                << Network.PostUserConfig model.token
                            )
                            sendEvents
                _ -> (model, Cmd.none)
        CloseModal ->
            ({ model | modal = Model.NoModal }, Cmd.none)

subscriptions : Model -> Sub Msg
subscriptions model =
    Sub.batch
        [ Time.every
            (   if Dict.values model.levels
                    |> List.any Level.isAnimating
                then 50
                else 1000
            )
            SetTime  
        -- , Time.every 1000 SetTime
        -- [ Sub.none
        , if model.game 
                |> Maybe.map 
                    (\result -> result.game /= Nothing &&
                        result.user /= Nothing
                    )
                |> Maybe.withDefault True
            then Time.every 3000 (always FetchData)
            else Time.every 20000 (always FetchData)
        ]

getId : Url -> Maybe String
getId =
    Url.Parser.parse
        <| Url.Parser.s "game"
        </> Url.Parser.string
