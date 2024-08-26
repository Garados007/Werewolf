module GuestInput exposing (..)

import Data exposing (UserInfo)
import Html exposing (Html, div)
import Html.Attributes as HA exposing (class)
import Html.Events as HE
import Debounce
import Triple
import MD5
import Language exposing (Language)
import Storage exposing (Storage)

type alias Model =
    { name: String
    , email: String
    , debouncer: Debounce.Model String
    }

type Msg
    = SetName String
    | SetEmail String
    | Debounce (Debounce.Msg String)
    | DoContinue

init : Storage -> Model
init storage =
    { name = Maybe.withDefault ""
        <| Storage.get .guestName storage
    , email = ""
    , debouncer = Debounce.init 500 ""
    }

singleLangBlock : Language -> List String -> List (Html msg)
singleLangBlock lang =
    List.singleton
    << singleLang lang

singleLang : Language -> List String -> (Html msg)
singleLang lang =
    Html.text
    << Language.getTextOrPath lang

view : Model -> Language -> Html Msg
view model lang =
    div [ class "guest-input-box" ]
        [ div [ class "input" ]
            [ div [ class "name" ]
                <| singleLangBlock lang
                    [ "init", "guest-input", "name" ]
            , div [ class "value" ]
                [ Html.input
                    [ HA.type_ "text"
                    , HA.value model.name
                    , HE.onInput SetName
                    ] []
                ]
            ]
        , div [ class "profile-image" ]
            [ Html.img
                [ HA.src
                    <| "https://www.gravatar.com/avatar/"
                    ++ MD5.hex (Debounce.settled model.debouncer)
                    ++ "?d=identicon"
                ] []
            ]
        , div [ class "input" ]
            [ div [ class "name" ]
                <| singleLangBlock lang
                    [ "init", "guest-input", "image-code" ]
            , div [ class "hint" ]
                <| singleLangBlock lang
                    [ "init", "guest-input", "image-hint" ]
            , div [ class "value" ]
                [ Html.input
                    [ HA.type_ "text"
                    , HA.value model.email
                    , HE.onInput SetEmail
                    , HA.placeholder model.name
                    ] []
                ]
            ]
        , div [ class "button" ]
            [ Html.button
                [ HE.onClick DoContinue
                , HA.disabled <| model.name == ""
                ]
                <| singleLangBlock lang
                    [ "init", "guest-input", "continue" ]
            ]
        ]

update : Msg -> Model -> (Model, Cmd Msg, Maybe UserInfo)
update msg model =
    case msg of
        SetName name ->
            if model.email == ""
            then Debounce.update
                    (Debounce.Change name)
                    model.debouncer
                |> \(new, cmd, _) ->
                    Triple.triple
                        { model
                        | name = name
                        , debouncer = new
                        }
                        (Cmd.map Debounce cmd)
                        Nothing
            else Triple.triple
                { model | name = name }
                Cmd.none
                Nothing
        SetEmail email ->
            Debounce.update
                (Debounce.Change <|
                    if email == ""
                    then model.name
                    else email
                )
                model.debouncer
            |> \(new, cmd, _) ->
                Triple.triple
                    { model
                    | email = email
                    , debouncer = new
                    }
                    (Cmd.map Debounce cmd)
                    Nothing
        Debounce sub ->
            Debounce.update sub model.debouncer
            |> \(new, cmd, _) ->
                Triple.triple
                    { model | debouncer = new }
                    (Cmd.map Debounce cmd)
                    Nothing
        DoContinue ->
            if model.name == ""
            then Triple.triple model Cmd.none Nothing
            else
                Triple.triple
                    model
                    Cmd.none
                <| Just
                <| Data.UserInfo model.name
                <| "https://www.gravatar.com/avatar/"
                ++ MD5.hex
                    ( if model.email == ""
                        then model.name
                        else model.email
                    )
                ++ "?d=identicon"
