module Debug.Extra exposing (viewModel)

import Html exposing (Html, pre, text, div, span)
import Html.Attributes exposing (style)


quote =
    "\""


indentChars =
    "[{("


outdentChars =
    "}])"


newLineChars =
    ","


uniqueHead =
    "##FORMAT##"


incr =
    20


viewModel : a -> Html msg
viewModel model =
    let
        lines =
            model
                |> Debug.toString
                |> (\m ->
                        "("
                            ++ m
                            ++ ")"
                            -- |> formatString maxDepth False 0
                            |> formatString2
                            |> String.split uniqueHead
                   )
    in
    pre 
        [ style "display" "table" 
        , Html.Attributes.class "elm-debug-extra-output"
        ] 
        <| (::)
            ( Html.node "style"
                [ Html.Attributes.attribute "rel" "stylesheet"
                , Html.Attributes.attribute "property" "stylesheet" 
                ]
                [ text """

.elm-debug-extra-output {
    width: 100%;
    margin: 0;
}
.elm-debug-extra-output > div:hover {
    background-color: #aaa9a9;
}
.elm-debug-extra-output > div > div {
    background-color: #686666;
    color: white;
    padding: 0 0.5em;
    width: 1px;
    -webkit-touch-callout: none;
    -webkit-user-select: none; 
     -khtml-user-select: none;
       -moz-user-select: none;
        -ms-user-select: none;
            user-select: none;
}

"""
                ]
            )
        <| List.indexedMap viewLine 
        <| List.filter (Tuple.second >> String.trim >> String.isEmpty >> not)
        <| List.map splitLine
        <| lines


viewLine : Int -> (Int, String) -> Html msg
viewLine index (indent, lineTxt) =
    div [ style "display" "table-row"
        ]
        [ div 
            [ style "display" "table-cell"
            , style "text-align" "right"
            ]
            [ text <| String.fromInt <| 1 + index ]
        , span
            [ style "paddingLeft" (px indent)
            , style "marginTop" "0px"
            , style "marginBottom" "0px"
            ]
            [ text lineTxt ]
        ]
    


px : Int -> String
px int =
    String.fromInt int
        ++ "px"

formatString2 : String -> String
formatString2 str =
    String.concat
        <| List.reverse
        <| .result
        <| List.foldl
            (\firstChar cary ->
                if cary.isInQuotes then
                    if firstChar == quote then
                        { cary
                        | isInQuotes = not cary.isInQuotes
                        , result = firstChar 
                            :: cary.result
                        , lastOpen = False
                        , optResult = Nothing
                        }
                    else
                        { cary
                        | result = firstChar 
                            :: cary.result 
                        , lastOpen = False
                        , optResult = Nothing
                        }
                else if String.contains firstChar newLineChars then
                    { cary
                    | result = firstChar 
                        :: pad cary.indent 
                        :: uniqueHead 
                        :: cary.result 
                    , lastOpen = False
                    , optResult = Nothing
                    }
                else if String.contains firstChar indentChars then
                    { cary
                    | indent = cary.indent + incr
                    , result = firstChar 
                        :: pad (cary.indent + incr) 
                        :: uniqueHead 
                        :: cary.result 
                    , lastOpen = True
                    , optResult = Just 
                        <| firstChar
                        :: cary.result
                    }
                else if String.contains firstChar outdentChars then
                    { cary
                    | indent = cary.indent - incr
                    , result =
                        if cary.lastOpen
                        then case cary.optResult of
                            Just result -> firstChar :: result 
                            Nothing -> firstChar :: cary.result
                        else  pad (cary.indent - incr) 
                            :: uniqueHead 
                            :: firstChar 
                            :: pad cary.indent 
                            :: uniqueHead
                            :: cary.result 
                    , lastOpen = False
                    , optResult = Nothing
                    }
                else if firstChar == quote then
                    { cary
                    | isInQuotes = not cary.isInQuotes
                    , result = firstChar 
                        :: cary.result 
                    , lastOpen = False
                    , optResult = Nothing
                    }
                else
                    { cary
                    | result = firstChar 
                        :: cary.result 
                    , lastOpen = False
                    , optResult = Nothing
                    }

            )
            { isInQuotes = False
            , indent = 0
            , result = []
            , lastOpen = False
            , optResult = Nothing
            }
        <| List.map String.fromChar
        <| String.toList str

-- formatString : Int -> Bool -> Int -> String -> String
-- formatString depth isInQuotes indent str =
--     if depth <= 0
--     then "[depth limit]"
--     else
--         case String.left 1 str of
--             "" ->
--                 ""

--             firstChar ->
--                 if isInQuotes then
--                     if firstChar == quote then
--                         firstChar
--                             ++ formatString (depth - 1) (not isInQuotes) indent (String.dropLeft 1 str)

--                     else
--                         firstChar
--                             ++ formatString (depth - 1) isInQuotes indent (String.dropLeft 1 str)

--                 else if String.contains firstChar newLineChars then
--                     uniqueHead
--                         ++ pad indent
--                         ++ firstChar
--                         ++ formatString (depth - 1) isInQuotes indent (String.dropLeft 1 str)

--                 else if String.contains firstChar indentChars then
--                     uniqueHead
--                         ++ pad (indent + incr)
--                         ++ firstChar
--                         ++ formatString (depth - 1) isInQuotes (indent + incr) (String.dropLeft 1 str)

--                 else if String.contains firstChar outdentChars then
--                     firstChar
--                         ++ uniqueHead
--                         ++ pad (indent - incr)
--                         ++ formatString (depth - 1) isInQuotes (indent - incr) (String.dropLeft 1 str)

--                 else if firstChar == quote then
--                     firstChar
--                         ++ formatString (depth - 1) (not isInQuotes) indent (String.dropLeft 1 str)

--                 else
--                     firstChar
--                         ++ formatString (depth - 1) isInQuotes indent (String.dropLeft 1 str)


pad : Int -> String
pad indent =
    String.padLeft 5 '0' <| String.fromInt indent


splitLine : String -> ( Int, String )
splitLine line =
    let
        indent =
            String.left 5 line
                |> String.toInt
                |> Maybe.withDefault 0

        newLine =
            String.dropLeft 5 line
    in
    ( indent, newLine )
