module Views.ViewThemeEditor exposing (..)

import Data
import Network exposing (EditUserConfig, editUserConfig)
import Language exposing (Language)

import Html exposing (Html, div)
import Html.Attributes as HA exposing (class)
import Html.Events as HE
import Color
import Color.Convert as CC
import Dict exposing (Dict)

import ColorPicker

type alias Model =
    { config: Data.UserConfig
    , picker: ColorPicker.State
    }

type Msg
    = UpdateBgUrl String
    | SetPicker ColorPicker.Msg
    | DoEvent Event

type Event
    = Send EditUserConfig
    | SetLang String
    | Return

init : Data.UserConfig -> Model
init config =
    { config = config
    , picker = ColorPicker.empty
    }

view : Language -> Dict String String -> Model -> String -> Html Msg
view lang flags model selLang =
    div [ class "theme-editor" ]
        [ div [ class "pane", class "return" ]
            [ Html.button
                [ HE.onClick <| DoEvent Return ]
                [ Html.text <| Language.getTextOrPath lang
                    [ "modals", "theme-editor", "go-back" ]
                ]
            ]
        , div [ class "pane" ]
            [ div [ class "group" ]
                <| List.singleton
                <| Html.map SetPicker
                <| ColorPicker.view
                    (CC.hexToColor model.config.theme
                        |> Result.toMaybe
                        |> Maybe.withDefault Color.white
                    )
                    model.picker
            , div [ class "group" ]
                [ Html.input
                    [ HA.type_ "url"
                    , HA.value model.config.background
                    , HA.placeholder
                        <| Language.getTextOrPath lang
                            [ "modals", "theme-editor", "background-url" ]
                    , HE.onInput UpdateBgUrl
                    ] []
                , Html.img
                    [ HA.src model.config.background ]
                    []
                ]
            ]
        , div [ class "pane", class "countries" ]
            <| List.map
                (\(id, css) ->
                    div
                        [ HA.classList
                            <| List.singleton
                            <| Tuple.pair "selected"
                            <| id == selLang
                        ]
                        <| List.singleton
                        <| Html.span
                            [ class "flag-icon"
                            , class <| "flag-icon-" ++ css
                            , HE.onClick <| DoEvent <| SetLang id
                            ]
                            []
                )
            <| Dict.toList flags
        ]

update : Msg -> Model -> (Model, List Event)
update msg model =
    case msg of
        UpdateBgUrl newUrl ->
            (\new -> (new, getChanges model new))
            <|  { model
                | config = model.config |> \config ->
                    { config
                    | background = newUrl
                    }
                }
        SetPicker sub ->
            let
                (newState, newColor) = ColorPicker.update sub
                    (CC.hexToColor model.config.theme
                        |> Result.toMaybe
                        |> Maybe.withDefault Color.white
                    )
                    model.picker

                newModel : Model
                newModel =
                    { model
                    | config = model.config |> \config ->
                        { config
                        | theme = newColor
                            |> Maybe.map CC.colorToHex
                            |> Maybe.withDefault config.theme
                        }
                    , picker = newState
                    }
            in (newModel, getChanges model newModel)
        DoEvent event -> (model, [ event ])

getChanges : Model -> Model -> List Event
getChanges old new =
    { editUserConfig
    | newTheme =
        if new.config.theme == old.config.theme
        then Nothing
        else Just new.config.theme
    , newBackground =
        if new.config.background == old.config.background
        then Nothing
        else Just new.config.background
    }
    |> (\conf -> if conf == editUserConfig then [] else [ Send conf ])
