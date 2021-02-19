module Views.ViewThemeEditor exposing (..)

import Data
import Network exposing (EditUserConfig, editUserConfig)
import Language exposing (Language)

import Html exposing (Html, div)
import Html.Attributes as HA exposing (class)
import Html.Events as HE
import Color
import Color.Convert as CC

import ColorPicker

type alias Model =
    { config: Data.UserConfig
    , picker: ColorPicker.State
    }

type Msg
    = UpdateBgUrl String
    | SetPicker ColorPicker.Msg

type Event
    = Send EditUserConfig

init : Data.UserConfig -> Model
init config =
    { config = config
    , picker = ColorPicker.empty
    }

view : Language -> Model -> Html Msg
view lang model =
    div [ class "theme-editor" ]
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
