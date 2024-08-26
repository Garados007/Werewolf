module Avatar exposing
    (AvatarStorage, Msg, empty, request, requireList, view, viewOrImg, subscriptions, update)

import Ports
import Json.Decode as JD
import Json.Encode as JE
import Dict exposing (Dict)
import Html exposing (Html)
import Html.Attributes as HA
import SvgParser
import Set

requestAvatar : String -> Cmd msg
requestAvatar key = Ports.avatarRequest <| JE.string key

receivedAvatar : Sub (Maybe (String, String))
receivedAvatar =
    Ports.avatarResponse
    <| Result.toMaybe
    << JD.decodeValue
        (JD.map2 Tuple.pair
            (JD.field "key" JD.string)
            (JD.field "img" JD.string)
        )

type AvatarStorage
    = AvatarStorage Int (() -> Dict String String)

type Msg = Msg (Maybe (String, String))

empty : AvatarStorage
empty = AvatarStorage 0 <| \() -> Dict.empty

request : AvatarStorage -> String -> Cmd msg
request (AvatarStorage _ storage) key =
    let
        unpacked = storage ()
    in if Dict.member key unpacked || key == ""
        then Cmd.none
        else requestAvatar key

requireList : AvatarStorage -> List String -> (AvatarStorage, Cmd msg)
requireList (AvatarStorage _ storage) keys =
    let
        unpacked = storage ()

        keySet = Set.fromList keys

        newStorage = Dict.toList unpacked
            |> List.filter (\(x, _) -> Set.member x keySet)
            |> Dict.fromList
    in
        ( AvatarStorage (Dict.size newStorage) <| \() -> newStorage
        , keys
            |> List.filter (\key -> key /= "" && not (Dict.member key unpacked))
            |> List.map requestAvatar
            |> Cmd.batch
        )

subscriptions : Sub Msg
subscriptions = Sub.map Msg receivedAvatar

update : Msg -> AvatarStorage -> AvatarStorage
update (Msg msg) (AvatarStorage size storage) =
    case msg of
        Nothing -> AvatarStorage size storage
        Just (key, code) ->
            let
                unpacked = storage ()
            in AvatarStorage (if Dict.member key unpacked then size else size + 1) <| \() -> Dict.insert key code unpacked

viewOrImg : AvatarStorage -> String -> Html msg
viewOrImg storage urlOrKey =
    if String.startsWith "@" urlOrKey
    then view storage <| String.dropLeft 1 urlOrKey
    else Html.img [ HA.src urlOrKey ] []

view : AvatarStorage -> String -> Html msg
view (AvatarStorage _ storage) key =
    let
        unpacked = storage ()
    in case Dict.get key unpacked of
        Nothing -> Html.div
            [ HA.class "avatar loading"
            , HA.attribute "data-key" key
            ]
            []
        Just code ->
            case SvgParser.parse code of
                Ok node -> Html.div
                    [ HA.class "avatar"
                    , HA.attribute "data-key" key
                    ]
                    [ node ]
                Err err -> Html.div
                    [ HA.class "avatar error"
                    , HA.attribute "data-key" key
                    ]
                    [ Html.text err ]
