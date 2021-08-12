module Styles exposing
    ( Styles
    , init
    , isAnimating
    , view
    , pushState
    )

import Html exposing (Html, div, text)
import Html.Attributes as HA exposing (class)
import Html.Lazy as HL
import Color exposing (Color)
import Color.Accessibility as CA
import Color.Convert as CC
import Color.Manipulate as CM
import Time exposing (Posix)
import Data
import Regex

type alias Styles =
    { oldConfig: Data.UserConfig
    , lastChange: Posix
    , hasOld: Bool
    , newConfig: Data.UserConfig
    }

init : Styles
init =
    { oldConfig =
        { theme = ""
        , background = ""
        , language = ""
        }
    , lastChange = Time.millisToPosix 0
    , hasOld = False
    , newConfig = 
        { theme = ""
        , background = ""
        , language = ""
        }
    }

isAnimating : Posix -> Styles -> Bool
isAnimating now styles =
    styles.oldConfig /= styles.newConfig &&
    Time.posixToMillis now - Time.posixToMillis styles.lastChange < 5000

view : Posix -> Styles -> Html msg
view now styles =
    let
        
        weight : Float
        weight =
            if isAnimating now styles
            then min 1 <| 
                (toFloat <| (Time.posixToMillis now) - (Time.posixToMillis styles.lastChange)) / 2000
            else 1
    in HL.lazy3 viewConfig styles.oldConfig styles.newConfig weight


viewConfig : Data.UserConfig -> Data.UserConfig -> Float -> Html msg
viewConfig oldConfig config weight =
    let
        colorBase : Color
        colorBase = CM.weightedMix
            (CC.hexToColor config.theme
                |> Result.toMaybe
                |> Maybe.withDefault Color.white
            )
            (CC.hexToColor oldConfig.theme
                |> Result.toMaybe
                |> Maybe.withDefault Color.white
            )
            weight
        
        isDark : Bool
        isDark = CA.luminance colorBase <= 0.5

        darken : Float -> Color -> Color
        darken = if isDark then CM.lighten else CM.darken

        colorBackground : Color
        colorBackground = colorBase

        textColor : Color
        textColor = if isDark then Color.white else Color.black
        
        textColorLight : Color
        textColorLight = CM.weightedMix
            colorBase
            textColor
            0.375
        
        textInvColor : Color
        textInvColor = if isDark then Color.black else Color.white
        
        colorLight : Color
        colorLight = darken 0.20 colorBase

        colorMedium : Color
        colorMedium = darken 0.30 colorBase

        colorDark : Color
        colorDark = darken 0.40 colorBase

        colorDarker : Color
        colorDarker = darken 0.50 colorBase

        textHightlight : Color
        textHightlight = darken 0.5 <| CM.rotateHue 180 colorBase

        build : String -> Regex.Regex
        build = Regex.fromString >> Maybe.withDefault Regex.never
    in div [ class "styles" ]
        [ Html.node "style"
            [ HA.rel "stylesheet" ]
            <| List.singleton
            <| text
            <| (\style -> 
                    ":root { " ++ style ++ "; --bg-url: url(\"" ++
                    (config.background 
                        |> Regex.replace (build "\\s") (always "")
                        |> Regex.replace (build "\\\\") (always "\\\\")
                        |> Regex.replace (build "\"") (always "\\\"")
                    ) ++
                    "\"); --bg-old-url: url(\"" ++
                    (oldConfig.background 
                        |> Regex.replace (build "\\s") (always "")
                        |> Regex.replace (build "\\\\") (always "\\\\")
                        |> Regex.replace (build "\"") (always "\\\"")
                    ) ++
                    "\"); }" 
                )
            <| String.concat
            <| List.intersperse "; "
            <| List.map
                (\(rule, color) ->
                    "--" ++ rule ++ ": " ++ CC.colorToCssRgba color
                )
                [ ("color-base", colorBase)
                , ("color-background", colorBackground)
                , ("text-color", textColor)
                , ("text-color-light", textColorLight)
                , ("text-inv-color", textInvColor)
                , ("text-hightlight", textHightlight)
                , ("color-light", colorLight)
                , ("color-light-transparent", CM.fadeOut 0.4 colorLight)
                , ("color-medium", colorMedium)
                , ("color-dark", colorDark)
                , ("color-dark-transparent", CM.fadeOut 0.4 colorDark)
                , ("color-dark-semitransparent", CM.fadeOut 0.2 colorDark)
                , ("color-darker", colorDarker)
                , ("color-darker-transparent", CM.fadeOut 0.4 colorDarker)
                ]
        , div 
            [ class "background"
            , class "old"
            ] []
        , div 
            [ class "background"
            , HA.style "opacity" <| String.fromFloat weight
            ] []
        ]

pushState : Posix -> Styles -> Data.UserConfig -> Styles
pushState now styles config =
    if styles.hasOld
    then 
        if config == styles.newConfig
        then styles
        else 
            { styles
            | oldConfig = styles.newConfig
            , newConfig = config
            , lastChange = now
            }
    else
        { oldConfig = config
        , lastChange = now
        , hasOld = True
        , newConfig = config
        }
