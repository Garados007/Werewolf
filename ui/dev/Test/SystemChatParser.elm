module Test.SystemChatParser exposing (..)

import Parser exposing (..)
import Html exposing (Html)
--!BEGIN
import Debug.Extra
--!END

type Token
    = General String
    | Variable String

main : Html Never
main = Html.div []
    [ execute "Hallo"
    , execute "{welt}"
    , execute "Hallo {welt}"
    , execute "Hallo {welt}!!!"
    , execute "A test {with} two {variable} fields"
    , execute "Invalid {test {case}}"
    , execute "Test with unfinished {variable"
    ]

execute : String -> Html msg
execute content = 
    Html.div []
        [ Html.h2 [] [ Html.text content ]
--!BEGIN
        , Debug.Extra.viewModel <| run parser content
--!END
        ]

parser : Parser (List Token)
parser =
    loop [] parserHelp

parserHelp : List Token -> Parser (Step (List Token) (List Token))
parserHelp revToken =
  oneOf
    [ succeed (\token -> Loop (token :: revToken))
        |= parserSingle
    , succeed ()
        |> map (\_ -> Done (List.reverse revToken))
    ]

parserSingle : Parser Token
parserSingle =
    oneOf
        [ map General
            <| getChompedString
            <| succeed ()
            |. chompIf ((/=) '{')
            |. chompUntilEndOr "{"
        , succeed Variable
            |. symbol "{"
            |= (getChompedString
                    <| succeed ()
                    |. chompUntil "}"
                )
            |. symbol "}"
        ]