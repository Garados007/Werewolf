module GuestInput exposing (..)

import Data exposing (UserInfo)
import Html exposing (Html, div, text)
import Html.Attributes as HA exposing (class)
import Html.Events as HE
import Debounce
import Triple
import MD5

type alias Model =
    { name: String
    , email: String
    , debouncer: Debounce.Model String
    }

type Msg
    = SetName String
    | SetEmail String
    | Debounce (Debounce.Msg String)
    | DoBack
    | DoContinue

init : Model
init =
    { name = ""
    , email = ""
    , debouncer = Debounce.init 500 ""
    }

view : Model -> Html Msg
view model =
    div [ class "guest-input-box" ]
        [ Html.h1 []
            [ text "Guest" ]
        , div [ class "profile-image" ]
            [ Html.img
                [ HA.src
                    <| "https://www.gravatar.com/avatar/"
                    ++ MD5.hex (Debounce.settled model.debouncer)
                    ++ "?d=identicon"
                ] []
            ]
        , div [ class "input" ]
            [ div []
                [ div [ class "name" ]
                    [ text "Name" ]
                , div [ class "value" ]
                    [ Html.input
                        [ HA.type_ "text"
                        , HA.value model.name
                        , HE.onInput SetName
                        ] []
                    ]
                ]
            , div []
                [ div [ class "name" ]
                    [ text "Email" ]
                , div [ class "hint" ]
                    [ text 
                        <| "Insert your email or any arbitary text here. This will be used to "
                        ++ "generate a user icon for you. If you have setup a profile picture at "
                        ++ "Gravatar you can use your own picture if you insert your email adress. "
                        ++ "The input is not stored and will be hashed afterwards."
                    ]
                , div [ class "value" ]
                    [ Html.input
                        [ HA.type_ "text"
                        , HA.value model.email
                        , HE.onInput SetEmail
                        , HA.placeholder model.name
                        ] []
                    ]
                ]
            ]
        , div [ class "button" ]
            [ Html.button
                [ HE.onClick DoBack ]
                [ text "Back" ]
            , Html.button
                [ HE.onClick DoContinue 
                , HA.disabled <| model.name == ""
                ]
                [ text "Continue" ]
            ]
        ]

update : Msg -> Model -> (Model, Cmd Msg, Maybe (Result () UserInfo))
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
        DoBack ->
            Triple.triple
                model
                Cmd.none
            <| Just
            <| Err ()
        DoContinue ->
            if model.name == ""
            then Triple.triple model Cmd.none Nothing
            else
                Triple.triple
                    model
                    Cmd.none
                <| Just
                <| Ok
                <| Data.UserInfo model.name
                <| "https://www.gravatar.com/avatar/"
                ++ MD5.hex 
                    ( if model.email == ""
                        then model.name
                        else model.email
                    )
                ++ "?d=identicon"
