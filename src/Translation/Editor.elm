module Translation.Editor exposing
    ( Model
    , Msg
    , init
    , view
    , update
    )

import Html exposing (Html, div, text)
import Html.Attributes as HA exposing (class)
import Html.Events as HE
import Translation.Data as Data
import Debug.Extra
import Dict exposing (Dict)
import Set exposing (Set)
import Json.Decode as JD
import Json.Encode as JE
import Maybe.Extra
import File.Download
import Time exposing (Posix, Zone)
import Task

type alias Model =
    { index: Data.LangIndex
    , data: Data.EditData
    , sources: List Data.LangSource
    , select: List Data.LangSource
    , editTheme: List (Data.LangSource, String)
    }

type Msg
    = None
    | SetOpen (List String) Bool
    | SetSelect Int Data.LangSource
    | EditNode (List String) Data.LangSource String
    | ResetNode (List String) Data.LangSource
    | EditTheme Data.LangSource String
    | ResetTheme Data.LangSource
    | DownloadInit
    | DownloadTime Zone Posix

init : Data.LangIndex -> Data.EditData -> Model
init index data =
    { index = index
    , data = data
    , sources = Data.getSources index |> List.map Tuple.first
    , select = [ Data.SourceRoot "de", Data.SourceRoot "en" ]
    , editTheme = []
    }

view : Model -> Html Msg
view model =
    div [ class "editor" ]
        [ viewTitle model
        , div [ class "edit-grid" ]
            [ div [ class "edit-grid-head" ]
                <| List.singleton
                <| div []
                [ div [] []
                , div []
                    <| List.singleton
                    <| div [ class "selectors" ]
                    <| List.indexedMap (viewInputSelector model.sources)
                    <| model.select
                ]
            , div [ class "edit-grid-body" ]
                <| (::) (viewEditTheme model)
                <| viewEditorRoot model.select model.data
            ]
        , viewChanges model.editTheme model.data
        ]

viewTitle : Model -> Html Msg
viewTitle model =
    div [ class "editor-title-box" ]
        [ div [ class "title" ]
            [ text "Werewolf Resource Translation" ]
        ]

viewChanges : List (Data.LangSource, String) -> Data.EditData -> Html Msg
viewChanges editTheme data =
    div [ class "edit-changes" ]
        [ div [ class "title" ]
            [ text "Download changes" ]
        , div [ class "description" ]
            [ text <| "Download your changes as a file and go to the "
            , Html.a
                [ HA.href "https://github.com/Garados007/Werewolf/issues/new"
                , HA.target "_blank" 
                ]
                [ text "GitHub Issues" ]
            , text <| ", create one and upload your changes there. The author will verify and "
                ++ "include them to the repository. Thank you for your contribution!"
            ]
        , div [ class "download", HE.onClick DownloadInit ]
            [ text "Download" ]
        , div [ class "preview-title" ]
            [ text "Preview of the file" ]
        , Html.textarea
            [ HA.readonly True
            , HA.value <| getChanges editTheme data
            ] []
        ]

getChanges : List (Data.LangSource, String) -> Data.EditData -> String
getChanges editTheme data =
    JE.encode 2
    <| JE.dict identity identity
    <| ( if editTheme == []
        then identity
        else Dict.insert "/content/lang/index.json"
                (JE.dict identity identity
                    <| Dict.singleton "themes"
                    <| JE.dict identity
                        (JE.dict identity
                            <| JE.dict identity JE.string
                        )
                    <| List.foldl
                        (\(source, edit) ->
                            case source of
                                Data.SourceRoot _ -> identity
                                Data.SourceTheme a b c ->
                                    Dict.update a
                                    <| Just
                                    << Dict.update b
                                        (Just
                                            << Dict.insert c edit
                                            << Maybe.withDefault Dict.empty
                                        )
                                    << Maybe.withDefault Dict.empty
                        )
                        Dict.empty
                        editTheme
                )
        )
    <| Data.getChanges data

viewInputSelector : List Data.LangSource -> Int -> Data.LangSource -> Html Msg
viewInputSelector sources index select =
    let
        langs : Set String
        langs =
            List.map
                (\source ->
                    case source of
                        Data.SourceRoot x -> x
                        Data.SourceTheme _ _  x -> x
                )
                sources
            |> Set.fromList
        
        cats : Dict String (Set String)
        cats = sources
            |> List.filterMap
                (\source ->
                    case source of
                        Data.SourceRoot _ -> Nothing
                        Data.SourceTheme a b _ -> Just (a, b)
                )
            |> List.foldl
                (\(a, b) ->
                    Dict.update a
                    <| Just
                    << Set.insert b
                    << Maybe.withDefault Set.empty
                )
                Dict.empty

        decoder : String -> Data.LangSource
        decoder =
            JD.decodeString Data.decodeLangSource
            >> Result.toMaybe
            >> Maybe.withDefault select

    in div [ class "edit-lang-selector" ]
        [ Html.select
            [ HE.onInput <| SetSelect index << decoder
            ]
            <| (::)
                ( Html.option
                    [ class "option", class "root" 
                    , HA.selected
                        <| case select of
                            Data.SourceRoot _ -> True
                            Data.SourceTheme _ _ _  -> False
                    , HA.value <| JE.encode 0
                        <| Data.encodeLangSource
                        <| Data.SourceRoot
                        <| case select of
                            Data.SourceRoot c -> c
                            Data.SourceTheme _ _  c -> c
                    ]
                    [ text "root" ]
                )
            <| List.map
                (\(a, bs) ->
                    Html.optgroup
                        [ class "option-group"
                        , HA.attribute "label" a
                        ]
                    <| List.map
                        (\b ->
                            Html.option
                                [ class "option", class "theme" 
                                , HA.selected
                                    <| case select of
                                        Data.SourceRoot _ -> False
                                        Data.SourceTheme ta tb _ ->
                                            ta == a && tb == b
                                , HA.value <| JE.encode 0
                                    <| Data.encodeLangSource
                                    <| Data.SourceTheme a b
                                    <| case select of
                                        Data.SourceRoot c -> c
                                        Data.SourceTheme _ _  c -> c
                                ]
                                [ text b ]
                        )
                    <| Set.toList bs
                )
            <| Dict.toList cats
        , Html.select
            [ HE.onInput <| SetSelect index << decoder
            ]
            <| List.map
                (\l ->
                    Html.option
                        [ class "option", class "lang"
                        , HA.selected
                            <| case select of
                                Data.SourceRoot sl -> sl == l
                                Data.SourceTheme _ _ sl -> sl == l
                        , HA.value <| JE.encode 0
                            <| Data.encodeLangSource
                            <| case select of
                                Data.SourceRoot _ -> Data.SourceRoot l
                                Data.SourceTheme a b _ -> Data.SourceTheme a b l
                        ]
                        [ text l ]
                )
            <| Set.toList langs
        ]

viewEditTheme : Model -> Html Msg
viewEditTheme model =
    let
        viewHeadCell : Html Msg
        viewHeadCell =
            div [ class "edit-lang-head" ]
            <| List.singleton
            <| div []
                [ div [ class "spacer" ] []
                , div
                    [ class "closer", class "hidden" ]
                    <| List.repeat 2
                    <| div [] []
                , div
                    [ class "text" 
                    , HA.title
                        <| "This is the name for the theme that should be shown in the selector. "
                        ++ "If this value is empty than the theme will not be listed. Language Root"
                        ++ " Resources does not have a theme name."
                    ]  
                    [ text "Theme Name" ]
                ]
        
        viewRoot : Html Msg
        viewRoot =
            div [ class "edit-info-editor" ]
            <| List.singleton
            <| div [ class "control" ]
            <| List.singleton
            <| div
                [ class "translation"
                , class "root"
                , HA.title <| "The selected resource is a root resource and therefore can no name "
                    ++ "set for it."
                ]
                [ text "root resource - cannot edit" ]
        
        getDefaultThemeName : Data.LangSource -> Maybe String
        getDefaultThemeName source =
            case source of
                Data.SourceRoot _ -> Nothing
                Data.SourceTheme a b c ->
                    model.index.themes
                    |> Dict.get a
                    |> Maybe.andThen (Dict.get b)
                    |> Maybe.andThen (Dict.get c)
        
        viewThemeEditor : Data.LangSource -> Html Msg
        viewThemeEditor source =
            List.filter (Tuple.first >> (==) source) model.editTheme
            |> List.map Tuple.second 
            |> List.head
            |> \edit ->
                div [ class "edit-info-editor" ]
                    [ div [ class "control" ]
                        [ div [ class "translation" ]
                            [ text <| case edit of
                                Just _ -> "modified"
                                Nothing -> case getDefaultThemeName source of
                                    Just _ -> "manual translation"
                                    Nothing -> "not provided"
                            ]
                        , case edit of
                            Nothing -> text ""
                            Just _ ->
                                div [ class "reset"
                                    , HE.onClick <| ResetTheme source
                                    ]
                                    [ text "reset" ]
                        ]
                    , Html.textarea
                        [ HA.value 
                            <| Maybe.withDefault ""
                            <| Maybe.Extra.orElse (getDefaultThemeName source)
                            <| edit
                        , HE.onInput <| EditTheme source
                        ] []
                    ]
        
    in div 
        [ class "edit-lang-group" ]
        [ viewHeadCell
        , div [ class "edit-lang-row" ]
            <| List.singleton
            <| div []
            <| List.map
                (\select ->
                    case select of
                        Data.SourceRoot _ -> viewRoot
                        Data.SourceTheme _ _ _ -> viewThemeEditor select
                )
            <| model.select
        ]

viewEditorRoot : List Data.LangSource -> Data.EditData -> List (Html Msg)
viewEditorRoot select data =
    case data of
        Data.EditNode ed ->
            Dict.toList ed.groups
            |> List.concatMap
                (\(key, info) ->
                    viewEditor select [ key ] info
                )
        Data.EditLeaf info ->
            viewEditor select [] data

viewEditor : List Data.LangSource -> List String -> Data.EditData -> List (Html Msg)
viewEditor select revPath data =
    let
        key : String
        key = List.head revPath |> Maybe.withDefault ""
        
        viewHeadCell : Maybe Bool -> Html Msg
        viewHeadCell open =
            div [ class "edit-lang-head" ]
            <| List.singleton
            <| div []
                [ div [ class "spacer" ]
                    <| List.repeat
                        (List.length revPath - 1)
                    <| div [] []
                , div
                    [ HA.classList
                        [ ("closer", True)
                        , ("open", open == Just True)
                        , ("hidden", open == Nothing)
                        ]
                    , HE.onClick
                        <| SetOpen revPath
                        <| Maybe.withDefault True
                        <| Maybe.map not open
                    ]
                    <| List.repeat 2
                    <| div [] []
                , div
                    [ class "text"
                    , HA.title
                        <| String.concat
                        <| List.intersperse "/"
                        <| List.reverse revPath
                    ]  
                    [ text key ]
                ]

        viewLeafBody : List Data.EditLeafInfo -> List (Html Msg)
        viewLeafBody variants =
            List.map
                (\source ->
                    viewLeafEditor source
                        ( case source of
                            Data.SourceRoot _ -> Nothing
                            Data.SourceTheme _ _ lang ->
                                List.filter
                                    (.source >> (==) (Data.SourceRoot lang))
                                    variants
                                |> List.head
                        )
                    <| List.head
                    <| List.filter (.source >> (==) source) variants
                )
                select

        viewLeafEditor : Data.LangSource -> Maybe Data.EditLeafInfo -> Maybe Data.EditLeafInfo -> Html Msg
        viewLeafEditor source parent info =
            div [ class "edit-info-editor" ]
                [ case parent of
                    Nothing -> text ""
                    Just realInfo ->
                        div [ class "root-box" ]
                            [ div [ class "control" ]
                                [ div [ class "origin" ]
                                    [ text "root" ]
                                ]
                            , Html.textarea
                                [ HA.readonly True
                                , HA.value
                                    <| Maybe.withDefault realInfo.text
                                    <| realInfo.edit
                                ] []
                            ]
                , div [ class "control" ]
                    [ div [ class "translation" ]
                        <| List.singleton
                        <| text
                        <| case info of
                            Nothing -> "not provided"
                            Just realInfo ->
                                case realInfo.edit of
                                    Just _ -> "modified"
                                    Nothing -> case realInfo.translation of
                                        Data.TranslationMissing ->
                                            "missing translation"
                                        Data.TranslationManual ->
                                            "manual translation"
                                        Data.TranslationAutomatic tr ->
                                            "automatic by " ++ tr
                    , case Maybe.andThen .edit info of
                        Just _ -> div
                            [ class "reset" 
                            , HE.onClick <| ResetNode revPath source
                            ]
                            [ text "reset" ]
                        Nothing -> text ""
                    ]
                , Html.textarea
                    [ HA.value
                        <| Maybe.withDefault ""
                        <| Maybe.Extra.orElse
                            (Maybe.map .text info)
                        <| Maybe.andThen .edit info
                    , HE.onInput <| EditNode revPath source
                    ] []
                ]

    in case data of
        Data.EditNode en ->
            (::)
                ( div [ class "edit-lang-group" ]
                    [ viewHeadCell <| Just en.open
                    , div [ class "edit-lang-row" ] [] 
                    ]
                )
            <| if en.open
                then List.concatMap
                        (\(k, e) -> viewEditor select (k :: revPath) e)
                    <| Dict.toList en.groups
                else []
        Data.EditLeaf info ->
            List.singleton
            <| div [ class "edit-lang-group" ]
                [ viewHeadCell Nothing
                , div [ class "edit-lang-row" ]
                    <| List.singleton
                    <| div []
                    <| viewLeafBody info 
                ]
    
update : Msg -> Model -> (Model, Cmd Msg)
update msg model =
    case msg of
        None -> (model, Cmd.none)
        SetOpen revPath open -> Tuple.pair
            { model
            | data = Data.setEditOpen open (List.reverse revPath) model.data
            }
            Cmd.none
        SetSelect index select -> Tuple.pair
            { model
            | select = List.indexedMap
                (\i o ->
                    if i == index
                    then select
                    else o
                )
                model.select
            }
            Cmd.none
        EditNode revPath select newText -> Tuple.pair
            { model
            | data = Data.setEditText (List.reverse revPath) select newText model.data
            }
            Cmd.none
        ResetNode revPath select -> Tuple.pair
            { model
            | data = Data.resetEditText (List.reverse revPath) select model.data
            }
            Cmd.none
        EditTheme select newText -> Tuple.pair
            { model
            | editTheme =
                let
                    updateList : List (Data.LangSource, String) -> List (Data.LangSource, String)
                    updateList list =
                        case list of
                            [] -> [ (select, newText) ]
                            (s,t)::ls ->
                                if s == select
                                then (s, newText) :: ls
                                else (s,t) :: updateList ls
                in updateList model.editTheme
            }
            Cmd.none
        ResetTheme select -> Tuple.pair
            { model
            | editTheme = List.filter (Tuple.first >> (/=) select) model.editTheme
            }
            Cmd.none
        DownloadInit -> Tuple.pair model
            <| Task.perform identity
            <| Task.map2 DownloadTime Time.here Time.now
        DownloadTime zone time -> Tuple.pair model
            <| File.Download.string
                (String.concat
                    [ String.fromInt <| Time.toYear zone time
                    , case Time.toMonth zone time of
                        Time.Jan -> "01"
                        Time.Feb -> "02"
                        Time.Mar -> "03"
                        Time.Apr -> "04"
                        Time.May -> "05"
                        Time.Jun -> "06"
                        Time.Jul -> "07"
                        Time.Aug -> "08"
                        Time.Sep -> "09"
                        Time.Oct -> "10"
                        Time.Nov -> "11"
                        Time.Dec -> "12"
                    , String.padLeft 2 '0' <| String.fromInt <| Time.toDay zone time
                    , "-"
                    , String.padLeft 2 '0' <| String.fromInt <| Time.toHour zone time
                    , String.padLeft 2 '0' <| String.fromInt <| Time.toMinute zone time
                    , String.padLeft 2 '0' <| String.fromInt <| Time.toSecond zone time
                    , "_language-modification.json"
                    ]
                )
                "text/json"
            <| getChanges model.editTheme model.data
