module Test.Colors exposing (..)

import Browser
import Platform
import Html exposing (Html, div, text)
import Html.Attributes as HA
import Html.Events as HE

type alias HslColor =
    { hue: Float
    , saturation: Float
    , lightness: Float
    }

hslToString : HslColor -> String
hslToString color =
    String.concat
        [ "hsl("
        , String.fromFloat color.hue
        , ", "
        , String.fromFloat <| 100 * color.saturation
        , "%, "
        , String.fromFloat <| 100 * color.lightness
        , "%)"
        ]

setHue : Float -> HslColor -> HslColor
setHue hue color = { color | hue = hue }

setSaturation : Float -> HslColor -> HslColor
setSaturation saturation color = { color | saturation = saturation }

setLightness : Float -> HslColor -> HslColor
setLightness lightness color = { color | lightness = lightness }

brighten : Float -> HslColor -> HslColor
brighten factor color =
    { color 
    -- | lightness = color.lightness * (1 + factor)
    | lightness = max 0 <| min 1 <| color.lightness + factor
    }

modBy2 : Float -> Float
modBy2 value =
    (-) value
    <| (*) 2
    <| toFloat
    <| floor
    <| value / 2

type alias RgbColor =
    { red: Float
    , green: Float
    , blue: Float
    }

hslToRgb : HslColor -> RgbColor
hslToRgb color =
    let 
        c : Float
        c = (1 - abs (2 * color.lightness - 1)) * color.saturation

        x : Float
        x = c * (1 - abs ((modBy2 <| color.hue / 60) - 1))

        m : Float
        m = color.lightness - c * 0.5

        (r,g,b) =
            if color.hue < 60 then (c, x, 0)
            else if color.hue < 120 then (x, c, 0)
            else if color.hue < 180 then (0, c, x)
            else if color.hue < 240 then (0, x, c)
            else if color.hue < 300 then (x, 0, c)
            else (c, 0, x)
    in RgbColor (r + m) (g + m) (b + m)

rgbToString : RgbColor -> String
rgbToString color =
    String.concat
        [ "rgb("
        , String.fromInt <| round <| 255 * color.red
        , ", "
        , String.fromInt <| round <| 255 * color.green
        , ", "
        , String.fromInt <| round <| 255 * color.blue
        , ")"
        ]

realBrightnessSq : RgbColor -> Float
realBrightnessSq color =
    color.red * color.red * 0.241 +
    color.green * color.green * 0.691 +
    color.blue * color.blue * 0.068

type alias Model = HslColor

type Msg
    = SetColor HslColor

main : Platform.Program () Model Msg
main = Browser.sandbox
    { init = HslColor 120 1.0 0.4
    , view = view
    , update = \msg model ->
        case msg of
            SetColor color -> color
    }

view : Model -> Html Msg
view model =
    div []
        [   let dark : Bool
                dark = (>=) 0.25 <| realBrightnessSq <| hslToRgb model

                adjBrighten : Float -> HslColor -> HslColor
                adjBrighten modifier =
                    if dark
                    then brighten (negate modifier)
                    else brighten modifier
        
            in Html.node "style"
                [ HA.rel "stylesheet"
                ]
                <| List.singleton
                <| text
                <| String.concat
                    [ ":root { --col: "
                    , hslToString model
                    , "; --text: "
                    , hslToString <| adjBrighten -0.7 model
                    , "; }"
                    ]
        , Html.input
            [ HA.type_ "range" 
            , HA.min "0"
            , HA.max "360"
            , HA.value <| String.fromFloat model.hue
            , HE.onInput <| SetColor
                << Maybe.withDefault model
                << Maybe.map (\hue -> setHue hue model)
                << String.toFloat
            ] []
        , Html.input
            [ HA.type_ "range" 
            , HA.min "0"
            , HA.max "100"
            , HA.value <| String.fromFloat <| 100 * model.saturation
            , HE.onInput <| SetColor
                << Maybe.withDefault model
                << Maybe.map (\saturation -> setSaturation (saturation * 0.01) model)
                << String.toFloat
            ] []
        , Html.input
            [ HA.type_ "range" 
            , HA.min "0"
            , HA.max "100"
            , HA.value <| String.fromFloat <| 100 * model.lightness
            , HE.onInput <| SetColor
                << Maybe.withDefault model
                << Maybe.map (\lightness -> setLightness (lightness * 0.01) model)
                << String.toFloat
            ] []
        , div
            [ HA.style "width" "5em"
            , HA.style "height" "5em"
            , HA.style "background-color" "var(--col)"
            , HA.style "color" "var(--text)"
            ]
            [ text "Lorem Ipsum" ]
        , text <| rgbToString <| hslToRgb model
        , div []
            <| List.singleton
            <| text
            <| (++) "Brightness: "
            <| String.fromFloat
            <| sqrt
            <| realBrightnessSq <| hslToRgb model
        ]

