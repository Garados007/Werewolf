module Translation.Data exposing (..)


import Dict exposing (Dict)
import Json.Decode as JD
import Json.Decode.Pipeline exposing (required, optional)
import Json.Encode as JE

type Lang
    = LangNode (Dict String Lang)
    | LangLeaf String

decodeLang : JD.Decoder Lang
decodeLang =
    JD.oneOf
        [ JD.map LangNode
            <| JD.map
                (Dict.filter
                    <| \key _ -> not <| String.startsWith "#" key
                )
            <| JD.dict
            <| JD.lazy
            <| \() -> decodeLang
        , JD.map LangLeaf JD.string
        ]

type LangTranslation
    = TranslationMissing
    | TranslationManual
    | TranslationAutomatic String

decodeLangTranslation : JD.Decoder (Dict String LangTranslation)
decodeLangTranslation =
    JD.map Dict.fromList
    <| JD.list
    <| JD.map2 Tuple.pair
        (JD.field "path" JD.string)
    <| JD.map
        ( Maybe.map TranslationAutomatic
            >> Maybe.withDefault TranslationManual
        )
    <| JD.field "translator"
    <| JD.nullable JD.string


type alias LangIndex =
    { languages: Dict String String
    , icons: Dict String String
    , modes: Dict String LangModeIndex
    }

decodeLangIndex : JD.Decoder LangIndex
decodeLangIndex =
    JD.succeed LangIndex
    |> required "languages" (JD.dict JD.string)
    |> required "icons" (JD.dict JD.string)
    |> required "modes" (JD.dict decodeLangModeIndex)

type alias LangModeIndex =
    { title: Dict String String
    , themes: Dict String LangThemeIndex
    }

decodeLangModeIndex : JD.Decoder LangModeIndex
decodeLangModeIndex =
    JD.succeed LangModeIndex
    |> required "title" (JD.dict JD.string)
    |> required "themes" (JD.dict decodeLangThemeIndex)

type alias LangThemeIndex =
    { title: Dict String String
    , default: Maybe String
    , enabled: Bool
    , ignoreCharacter: List String
    }

decodeLangThemeIndex : JD.Decoder LangThemeIndex
decodeLangThemeIndex =
    JD.succeed LangThemeIndex
    |> required "title" (JD.dict JD.string)
    |> optional "default" (JD.nullable JD.string) Nothing
    |> optional "enabled" JD.bool True
    |> optional "ignore_character" (JD.list JD.string) []

getSources : LangIndex -> List (LangSource, String)
getSources index =
    List.map
        (\x -> (x, getSourcePath x))
    <| List.foldl
        (\(theme, { themes }) l1 ->
            List.foldl
                (\cat l2 ->
                    List.foldl
                        (\lang ->
                            (::) (SourceTheme theme cat lang)
                        )
                        l2
                    <| Dict.keys index.icons
                )
                l1
            <| Dict.keys themes
        )
        ( List.map SourceRoot
            <| Dict.keys index.icons
        )
    <| Dict.toList index.modes

getSourcePath : LangSource -> String
getSourcePath source =
    case source of
        SourceRoot c -> "/content/lang/root/" ++ c ++ ".json"
        SourceTheme a b c -> "/content/lang/" ++ a ++ "/" ++ b ++ "/" ++ c ++ ".json"

type LangSource
    = SourceRoot String
    | SourceTheme String String String

decodeLangSource : JD.Decoder LangSource
decodeLangSource =
    JD.oneOf
        [ JD.map SourceRoot <| JD.string
        , JD.map3 SourceTheme
            (JD.index 0 JD.string)
            (JD.index 1 JD.string)
            (JD.index 2 JD.string)
        ]

encodeLangSource : LangSource -> JE.Value
encodeLangSource lang =
    case lang of
        SourceRoot c -> JE.string c
        SourceTheme a b c -> JE.list JE.string [ a, b, c ]

type EditData
    = EditNode
        { source: List LangSource
        , groups: Dict String EditData
        , open: Bool
        }
    | EditLeaf (List EditLeafInfo)

type alias EditLeafInfo =
    { source: LangSource
    , text: String
    , edit: Maybe String
    , translation: LangTranslation
    }

initEditData : EditData
initEditData = EditNode { source = [], groups = Dict.empty, open = True }

mergeEditData : LangSource -> Lang -> EditData -> EditData
mergeEditData source lang data =
    case (lang, data) of
        (LangNode ln, EditNode dn) ->
            EditNode
                { source = source :: dn.source
                , groups = Dict.merge
                    (\k l ->
                        Dict.insert k
                        <| mergeEditData source l
                        <| initEditData
                    )
                    (\k l e ->
                        Dict.insert k
                        <| mergeEditData source l e
                    )
                    Dict.insert
                    ln
                    dn.groups
                    Dict.empty
                , open = True
                }
        (LangLeaf ll, EditLeaf el) ->
            EditLeaf <|
                { source = source
                , text = ll
                , translation = TranslationManual
                , edit = Nothing
                } :: el
        (LangLeaf ll, EditNode en) ->
            if en.source == []
            then EditLeaf
                <| List.singleton
                    { source = source
                    , text = ll
                    , translation = TranslationManual
                    , edit = Nothing
                    }
            else data
        _ -> data

applyTranslationInfo : LangSource -> Dict String LangTranslation -> EditData -> EditData
applyTranslationInfo source translations =
    let
        apply : String -> EditData -> EditData
        apply prefix data =
            case data of
                EditNode en ->
                    EditNode
                        { en
                        | groups = Dict.map
                            (\key ->
                                apply <|
                                    if prefix == ""
                                    then key
                                    else prefix ++ "." ++ key
                            )
                            en.groups
                        }
                EditLeaf infos ->
                    EditLeaf
                    <| List.map
                        (\info ->
                            if info.source == source
                            then case Dict.get prefix translations of
                                Nothing -> info
                                Just tr -> { info | translation = tr }
                            else info
                        )
                    <| infos
    in apply ""

setEditOpen : Bool -> List String -> EditData -> EditData
setEditOpen open path data =
    case (path, data) of
        ([], EditNode en) ->
            EditNode { en | open = open }
        (p::ps, EditNode en) ->
            EditNode
                { en
                | groups = Dict.update p
                    (Maybe.map <| setEditOpen open ps)
                    en.groups
                }
        _ -> data

setEditText : List String -> LangSource -> String -> EditData -> EditData
setEditText path source text data =
    case (path, data) of
        ([], EditLeaf el) ->
            EditLeaf <|
                let
                    update : List EditLeafInfo -> List EditLeafInfo
                    update list =
                        case list of
                            [] ->
                                List.singleton
                                    { source = source
                                    , text = ""
                                    , edit = Just text
                                    , translation = TranslationMissing
                                    }
                            l::ls ->
                                if l.source == source
                                then { l | edit = Just text } :: ls
                                else l :: update ls
                in update el
        (p::ps, EditNode en) ->
            EditNode
                { en
                | groups = Dict.update p
                    (Maybe.map <| setEditText ps source text)
                    en.groups
                }
        _ -> data

resetEditText : List String -> LangSource -> EditData -> EditData
resetEditText path source data =
    case (path, data) of
        ([], EditLeaf el) ->
            EditLeaf <|
                let
                    update : List EditLeafInfo -> List EditLeafInfo
                    update list =
                        case list of
                            [] -> []
                            l::ls ->
                                if l.source == source
                                then if l.translation == TranslationMissing
                                    then ls
                                    else { l | edit = Nothing } :: ls
                                else l :: update ls
                in update el
        (p::ps, EditNode en) ->
            EditNode
                { en
                | groups = Dict.update p
                    (Maybe.map <| resetEditText ps source)
                    en.groups
                }
        _ -> data

getChanges : EditData -> Dict String JE.Value
getChanges data =
    case data of
        EditNode info ->
            Dict.foldl
                (\key child res1 ->
                    Dict.foldl
                        (\path json ->
                            Dict.update path
                                (Maybe.withDefault Dict.empty
                                    >> Dict.insert key json
                                    >> Just
                                )
                        )
                        res1
                    <| getChanges child
                )
                Dict.empty
                info.groups
            |> Dict.map (\_ -> JE.dict identity identity)
        EditLeaf infos ->
            List.foldl
                (\info ->
                    case info.edit of
                        Nothing -> identity
                        Just text -> Dict.insert (getSourcePath info.source) (JE.string text)
                )
                Dict.empty
                infos
