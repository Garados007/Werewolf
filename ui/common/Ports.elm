port module Ports exposing (..)

import Json.Decode as JD
import Json.Encode as JE

import LocalStorage

port receiveSocketMsg : (JD.Value -> msg) -> Sub msg
port sendSocketCommand : JE.Value -> Cmd msg
port receiveSocketClose : (JD.Value -> msg) -> Sub msg

port sendToClipboard : JE.Value -> Cmd msg

port settingGetItem : LocalStorage.GetItemPort msg
port settingSetItem : LocalStorage.SetItemPort msg
port settingClear : LocalStorage.ClearPort msg
port settingListKeys : LocalStorage.ListKeysPort msg
port settingResponse : LocalStorage.ResponsePort msg

port avatarRequest : JE.Value -> Cmd msg
port avatarResponse : (JD.Value -> msg) -> Sub msg
