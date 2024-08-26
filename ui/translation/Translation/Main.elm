module Translation.Main exposing (..)

import Browser
import Dict exposing (Dict)
import Html exposing (Html)
import Html.Attributes as HA
import Http
import Translation.Data as Data
import Task
import Translation.ViewProgress
import Translation.Editor as Editor

type Model
    = ModelInit
    | ModelIndex
        { index: Data.LangIndex
        , data: Data.EditData
        , missing: Int
        , total: Int
        }
    | ModelLangInfo
        { index: Data.LangIndex
        , data: Data.EditData
        , missing: Int
        , total: Int
        }
    | ModelEditor Editor.Model
    | ModelErr Http.Error

type Msg
    = RecIndex (Result Http.Error Data.LangIndex)
    | RecLang Data.LangSource (Result Http.Error Data.Lang)
    | RecLangInfo Data.LangSource (Result Http.Error (Dict String Data.LangTranslation))
    | StartLoadInfo
    | StartEditor
    | WrapEditor Editor.Msg

main : Program () Model Msg
main =
    Browser.document
        { init = init
        , view = \model ->
            { title = "Translation Service"
            , body = view model
            }
        , update = update
        , subscriptions = always Sub.none
        }

init : () -> (Model, Cmd Msg)
init () =
    Tuple.pair ModelInit
    <| Http.get
        { url = "/content/lang/index.json"
        , expect = Http.expectJson RecIndex Data.decodeLangIndex
        }

view : Model -> List (Html Msg)
view model =
    [ Html.node "link"
        [ HA.attribute "rel" "stylesheet"
        , HA.attribute "property" "stylesheet"
        , HA.attribute "href" "/content/css/translation/view-progress.css"
        ] []
    , Html.node "link"
        [ HA.attribute "rel" "stylesheet"
        , HA.attribute "property" "stylesheet"
        , HA.attribute "href" "/content/css/translation/view-editor.css"
        ] []
    , Html.node "link"
        [ HA.attribute "rel" "stylesheet"
        , HA.attribute "property" "stylesheet"
        , HA.attribute "href" "/content/css/translation/view-editor-title.css"
        ] []
    , Html.node "link"
        [ HA.attribute "rel" "stylesheet"
        , HA.attribute "property" "stylesheet"
        , HA.attribute "href" "/content/css/translation/view-download.css"
        ] []
    , case model of
        ModelInit ->
            Translation.ViewProgress.view "load index" 0 10
        ModelIndex m ->
            Translation.ViewProgress.view "load language resource files"
                (m.total - m.missing) (2 * m.total)
        ModelLangInfo m ->
            Translation.ViewProgress.view "load translation status"
                (2 * m.total - m.missing) (2 * m.total)
        ModelEditor m ->
            Html.map WrapEditor
            <| Editor.view m
        ModelErr err ->
            Translation.ViewProgress.view
                ( (++) "Unexpeced Error: " <| case err of
                    Http.BadUrl x -> "Bad Url (" ++ x ++ ")"
                    Http.Timeout -> "Network Timeout"
                    Http.NetworkError -> "Network Error"
                    Http.BadStatus x -> "Bad Status " ++ String.fromInt x
                    Http.BadBody x -> "Bad Body (" ++ x ++ ")"

                ) 0 0
    ]

update : Msg -> Model -> (Model, Cmd Msg)
update msg model =
    case (msg, model) of
        (RecIndex (Err err), ModelInit) -> (ModelErr err, Cmd.none)
        (RecIndex (Ok index), ModelInit) ->
            let 
                list : List (Data.LangSource, String)
                list = Data.getSources index

                length : Int
                length = List.length list
            in Tuple.pair
                (ModelIndex
                    { index = index
                    , data = Data.initEditData
                    , missing = length
                    , total = length
                    }
                )
                <| Cmd.batch
                <| List.map
                    (\(source, url) ->
                        Http.get
                            { url = url
                            , expect = Http.expectJson (RecLang source) Data.decodeLang
                            }
                    )
                    list
        (RecIndex _, _) -> (model, Cmd.none)
        (RecLang _ (Err _), ModelIndex m) ->
            Tuple.pair
                (ModelIndex
                    { m
                    | missing = m.missing - 1
                    }
                )
            <| if m.missing <= 1
                then Task.perform identity <| Task.succeed StartLoadInfo
                else Cmd.none
        (RecLang lang (Ok data), ModelIndex m) ->
            Tuple.pair
                (ModelIndex
                    { m
                    | missing = m.missing - 1
                    , data = Data.mergeEditData lang data m.data
                    }
                )
            <| if m.missing <= 1
                then Task.perform identity <| Task.succeed StartLoadInfo
                else Cmd.none
        (RecLang _ _, _) -> (model, Cmd.none)
        (StartLoadInfo, ModelIndex m) ->
            Tuple.pair
                (ModelLangInfo
                    { m
                    | missing = m.total
                    }
                )
            <| Cmd.batch
            <| List.map
                (\(source, url) ->
                    Http.get
                        { url = String.replace "/lang/" "/lang-info/" url
                        , expect = Http.expectJson (RecLangInfo source) Data.decodeLangTranslation
                        }
                )
            <| Data.getSources m.index
        (StartLoadInfo, _) -> (model, Cmd.none)
        (RecLangInfo _ (Err _), ModelLangInfo m) ->
            Tuple.pair
                (ModelLangInfo
                    { m
                    | missing = m.missing - 1
                    }
                )
            <| if m.missing <= 1
                then Task.perform identity <| Task.succeed StartEditor
                else Cmd.none
        (RecLangInfo lang (Ok data), ModelLangInfo m) ->
            Tuple.pair
                (ModelLangInfo
                    { m
                    | missing = m.missing - 1
                    , data = Data.applyTranslationInfo lang data m.data
                    }
                )
            <| if m.missing <= 1
                then Task.perform identity <| Task.succeed StartEditor
                else Cmd.none
        (RecLangInfo _ _, _) -> (model, Cmd.none)
        (StartEditor, ModelLangInfo m) ->
            Tuple.pair
                (ModelEditor <| Editor.init m.index m.data)
                Cmd.none
        (StartEditor, _) -> (model, Cmd.none)
        (WrapEditor sub, ModelEditor editor) ->
            Tuple.mapBoth
                ModelEditor
                (Cmd.map WrapEditor)
            <| Editor.update sub editor
        (WrapEditor _, _) -> (model, Cmd.none)
