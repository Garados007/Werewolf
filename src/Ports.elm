port module Ports exposing (..)

import Json.Decode as JD
import Json.Encode as JE

port receiveSocketMsg : (JD.Value -> msg) -> Sub msg
port sendSocketCommand : JE.Value -> Cmd msg
port receiveSocketClose : (JD.Value -> msg) -> Sub msg

port sendToClipboard : JE.Value -> Cmd msg
