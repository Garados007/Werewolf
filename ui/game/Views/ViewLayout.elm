module Views.ViewLayout exposing (..)

import Html exposing (Html, div, text)
import Html.Attributes as HA exposing (class)
import Html.Events as HE
import Svg exposing (Svg)

import Language exposing (Language)
import Dict exposing (Dict)

type LayoutImage
    = LayoutImageSrc String
    | LayoutImageSvg (Svg ())

type alias LayoutButton msg =
    { action: msg
    , img: LayoutImage
    , hint: LayoutText
    , class: List String
    }

mapButton : (a -> b) -> LayoutButton a -> LayoutButton b
mapButton tagger button =
    { action = tagger button.action
    , img = button.img
    , hint = button.hint
    , class = button.class
    }

type LayoutText
    = StaticLayoutText String
    | LangLayoutText (List String)
    | LangOptLayoutText (List String) (Dict String String)

type alias LayoutBanner msg =
    { closeable: Maybe msg
    , content: Html msg
    }

mapBanner : (a -> b) -> LayoutBanner a -> LayoutBanner b
mapBanner tagger banner =
    { closeable = Maybe.map tagger banner.closeable
    , content = Html.map tagger banner.content
    }

type alias LayoutConfig msg =
    { titleButtonsLeft: List (LayoutButton msg)
    , titleButtonsRight: List (LayoutButton msg)
    , titleText: LayoutText
    , leftSection: Html msg
    , showLeftSection: Maybe Bool
    , banner: List (LayoutBanner msg)
    , contentClass: String
    , content: List (Html msg)
    , bottomRightButton: Maybe (LayoutButton msg)
    }

render : Language -> LayoutText -> String
render lang layoutText =
    case layoutText of
        StaticLayoutText t -> t
        LangLayoutText p -> Language.getTextOrPath lang p
        LangOptLayoutText p d -> Language.getTextFormatOrPath lang p d

renderButton : Language -> LayoutButton msg -> Html msg
renderButton lang button =
    Html.button
        [ class "view-layout-button"
        , HA.classList
            <| List.map (\x -> (x, True))
            <| button.class
        , HA.title <| render lang button.hint
        , HE.onClick button.action
        ]
        [ case button.img of
            LayoutImageSrc src ->
                Html.img
                    [ HA.src src
                    ] []
            LayoutImageSvg svg ->
                Svg.map (always button.action) svg
        ]

renderBanner : LayoutBanner msg -> Html msg
renderBanner banner =
    div
        [ class "view-layout-banner" ]
        [ div [ class "view-layout-banner-content" ]
            [ banner.content ]
        , Maybe.withDefault (text "")
            <| Maybe.map
                (\event ->
                    div [ class "view-layout-banner-closer"
                        , HE.onClick event
                        ]
                        [ text "X" ]
                )
            <| banner.closeable
        ]

view : Language -> LayoutConfig msg -> Html msg
view lang config =
    div [ class "view-layout" ]
        [ div [ class "view-layout-header" ]
            [ div [ class "view-layout-buttons-left" ]
                <| List.map (renderButton lang)
                <| config.titleButtonsLeft
            , div [ class "view-layout-title" ]
                [ text <| render lang config.titleText ]
            , div [ class "view-layout-buttons-right" ]
                <| List.map (renderButton lang)
                <| config.titleButtonsRight
            ]
        , div [ class "view-layout-main" ]
            [ div
                [ HA.classList
                    [ ("view-layout-left-content", True)
                    , Tuple.pair "view-layout-left-open"
                       <| config.showLeftSection == Just True
                    , Tuple.pair "view-layout-left-close"
                       <| config.showLeftSection == Just False
                    ]
                ]
                <| List.singleton
                <| div [ class "view-layout-left-content-inner" ]
                [ config.leftSection ]
            , div [ class "view-layout-main-body" ]
                <| (List.map renderBanner config.banner)
                ++  [ div
                        [ class "view-layout-main-content"
                        , class config.contentClass
                        ]
                        <| config.content
                    ]
            , Maybe.withDefault (text "")
                <| Maybe.map
                    (\button ->
                        div [ class "view-layout-bottom-right-buttom" ]
                            [ renderButton lang button ]
                    )
                <| config.bottomRightButton
            ]
        ]
