module Views.ViewRoleInfo exposing (view)

import Html exposing (Html, div, text)
import Html.Attributes exposing (class)
import Language exposing (Language)

view : Language -> String -> Html Never
view lang roleKey =
    div [ class "role-info" ]
        <| List.map
            (Html.p [] << List.singleton << text)
        <| String.lines
        <| Language.getTextOrPath lang
            [ "theme", "role", roleKey, "info" ]
