module Views.ViewModal exposing
    ( Msg (..)
    , view
    , viewOnlyClose
    , viewExtracted
    )

import Html exposing (Html, div, text)
import Html.Attributes exposing (class)
import Html.Events as HE

type Msg msg
    = Close
    | Wrap msg

view : String -> List (Html msg) -> Html (Msg msg)
view title content =
    div [ class "modal-background" ]
        <| List.singleton
        <| div [ class "modal-window" ]
            [ div [ class "modal-title-box" ]
                [ div [ class "modal-title" ]
                    [ text title ]
                , div 
                    [ class "modal-close" 
                    , HE.onClick Close
                    ]
                    [ text "X" ]
                ]
            , Html.map Wrap
                <| div [ class "modal-content" ]
                    content
            ]

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
