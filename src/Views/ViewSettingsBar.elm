module Views.ViewSettingsBar exposing (..)

import Model exposing (Model, Modal (..))

import Html exposing (Html, div)
import Html.Attributes exposing (class)
import Svg
import Svg.Attributes as SA
import Svg.Events as SE

import Views.ViewThemeEditor

view : Model -> Html Modal
view model =
    div [ class "settings-bar" ]
        [ Svg.svg
            [ SA.width "512"
            , SA.height "512"
            , SA.viewBox "0 0 24 24"
            , SE.onClick <| SettingsModal <| Views.ViewThemeEditor.init model.bufferedConfig
            ]
            [ Svg.path
                [ SA.d """m22.683 
9.394-1.88-.239c-.155-.477-.346-.937-.569-1.374l1.161-1.495c.47-.605.415-1.459-.122-1.979l-1.575-1.575c-.525-.542-1.379-.596-1.985-.127l-1.493 
1.161c-.437-.223-.897-.414-1.375-.569l-.239-1.877c-.09-.753-.729-1.32-1.486-1.32h-2.24c-.757 
0-1.396.567-1.486 1.317l-.239 
1.88c-.478.155-.938.345-1.375.569l-1.494-1.161c-.604-.469-1.458-.415-1.979.122l-1.575 
1.574c-.542.526-.597 1.38-.127 1.986l1.161 1.494c-.224.437-.414.897-.569 
1.374l-1.877.239c-.753.09-1.32.729-1.32 1.486v2.24c0 .757.567 1.396 1.317 
1.486l1.88.239c.155.477.346.937.569 1.374l-1.161 1.495c-.47.605-.415 1.459.122 1.979l1.575 
1.575c.526.541 1.379.595 1.985.126l1.494-1.161c.437.224.897.415 1.374.569l.239 
1.876c.09.755.729 1.322 1.486 1.322h2.24c.757 0 1.396-.567 
1.486-1.317l.239-1.88c.477-.155.937-.346 1.374-.569l1.495 1.161c.605.47 1.459.415 
1.979-.122l1.575-1.575c.542-.526.597-1.379.127-1.985l-1.161-1.494c.224-.437.415-.897.569-1.374l1.876-.239c.753-.09 
1.32-.729 1.32-1.486v-2.24c.001-.757-.566-1.396-1.316-1.486zm-10.683 7.606c-2.757 
0-5-2.243-5-5s2.243-5 5-5 5 2.243 5 5-2.243 5-5 5z"""
                ] []
            ]
        ]