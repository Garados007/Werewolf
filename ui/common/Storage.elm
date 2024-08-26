module Storage exposing
    ( Storage
    , Data
    , Key (..)
    , Msg
    , init
    , set
    , get
    , update
    , subscriptions
    )

import Ports
import LocalStorage
import Json.Decode as JD
import Json.Encode as JE

type Storage = Storage Data

type alias Data =
    { guestName: Maybe String
    , streamerMode: Maybe Bool
    }

type Key
    = StorageGuestName
    | StorageStreamerMode

type alias Msg = LocalStorage.Response

storage : LocalStorage.LocalStorage msg
storage = LocalStorage.make
    Ports.settingGetItem
    Ports.settingSetItem
    Ports.settingClear
    Ports.settingListKeys
    "werewolf"

init : (Storage, Cmd Msg)
init =
    (Storage
        { guestName = Nothing
        , streamerMode = Nothing
        }
    , Cmd.batch
        <| List.map 
            (LocalStorage.getItem storage)
            [ "guestName"
            , "streamerMode"
            ]
    )

set : (Data -> Data) -> Storage -> (Storage, Cmd msg)
set updater (Storage data) =
    let
        new : Data
        new = updater data

        query : (Data -> Maybe a) -> String -> (a -> JE.Value) -> Maybe (Cmd msg)
        query selector key value =
            if selector data /= selector new && selector new /= Nothing
            then Just <| case selector new of
                Just val -> LocalStorage.setItem storage key <| value val
                Nothing -> LocalStorage.setItem storage key JE.null
            else Nothing
    in
        ( Storage new
        , Cmd.batch
            <| List.filterMap identity
                [ query .guestName "guestName" JE.string
                , query .streamerMode "streamerMode" JE.bool
                ]
        )

get : (Data -> Maybe a) -> Storage -> Maybe a
get selector (Storage data) = selector data

update : Msg -> Storage -> (Storage, Maybe Key)
update msg (Storage data) =
    let
        updater : (Maybe a -> Data) -> JD.Decoder a -> Key -> JD.Value -> (Storage, Maybe Key)
        updater setter decoder key value =
            case JD.decodeValue (JD.nullable decoder) value of
                Ok val ->
                    (Storage
                        <| setter val
                    , Just key
                    )
                Err _ -> (Storage data, Nothing)

    in case msg of
        LocalStorage.Item "guestName" value ->
            updater
                (\x -> { data | guestName = x })
                JD.string
                StorageGuestName
                value
        LocalStorage.Item "streamerMode" value ->
            updater
                (\x -> { data | streamerMode = x })
                JD.bool
                StorageStreamerMode
                value
        _ -> (Storage data, Nothing)

subscriptions : Sub Msg
subscriptions =
    LocalStorage.responseHandler identity storage
        |> Ports.settingResponse
