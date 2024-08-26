module Views.ViewModal exposing
    ( Msg (..)
    , view
    , viewCondClose
    , viewOnlyClose
    , viewExtracted
    )

import Html exposing (Html, div, text)
import Html.Attributes exposing (class)
import Html.Events as HE

type Msg msg
    = Close
    | Wrap msg

viewCondClose : Bool -> String -> List (Html msg) -> Html (Msg msg)
viewCondClose close title content =
    div [ class "modal-background" ]
        [ div
            [ class "modal-background-closer"
            , HE.onClick Close
            ] []
        , div [ class "modal-window" ]
            [ div [ class "modal-title-box" ]
                [ div [ class "modal-title" ]
                    [ text title ]
                , if close
                    then div
                        [ class "modal-close"
                        , HE.onClick Close
                        ]
                        [ text "X" ]
                    else text ""
                ]
            , Html.map Wrap
                <| div [ class "modal-content" ]
                    content
            ]
        ]

view : String -> List (Html msg) -> Html (Msg msg)
view = viewCondClose True

viewOnlyClose : String -> List (Html Never) -> Html ()
viewOnlyClose title content =
    Html.map
        (\ msg ->
            case msg of
                Close -> ()
                Wrap subMsg -> never subMsg
        )
        <| view title content

viewExtracted : outerMsg -> (innerMsg -> outerMsg)
    -> String -> List (Html innerMsg) -> Html outerMsg
viewExtracted onClose onEvent title content =
    Html.map
        (\ msg ->
            case msg of
                Close -> onClose
                Wrap subMsg -> onEvent subMsg
        )
        <| view title content
