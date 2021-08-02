module Test.OAuth exposing (main)

import Base64.Encode as Base64
import Browser exposing (Document, application)
import Browser.Navigation as Navigation exposing (Key)
import Bytes exposing (Bytes)
import Bytes.Encode as Bytes
import Delay exposing (after)
import Html exposing (..)
import Html.Attributes exposing (..)
import Html.Events exposing (..)
import Http
import Json.Decode as Json
import OAuth
import OAuth.AuthorizationCode as OAuth
import Url exposing (Protocol(..), Url)
import Random

main : Program () Model Msg
main =
    application
        { init = init
            -- Maybe.map convertBytes >> init
        , update =
            update
        , subscriptions = always Sub.none
        , onUrlRequest =
            always NoOp
        , onUrlChange =
            always NoOp
        , view =
            view
                { title = "Auth0 - Flow: Authorization Code"
                , btnClass = class "btn-auth0"
                }
        }


{-| OAuth configuration.

Note that this demo also fetches basic user information with the obtained access token,
hence the user info endpoint and JSON decoder

-}
configuration : Configuration
configuration =
    { authorizationEndpoint =
        { defaultHttpsUrl | host = "auth.2complex.de", path = "/auth/realms/2complex/protocol/openid-connect/auth" }
    , tokenEndpoint =
        { defaultHttpsUrl | host = "auth.2complex.de", path = "/auth/realms/2complex/protocol/openid-connect/token" }
    , userInfoEndpoint =
        { defaultHttpsUrl | host = "auth.2complex.de", path = "/auth/realms/2complex/protocol/openid-connect/userinfo" }
    , userInfoDecoder =
        Json.map3 UserInfo
            (Json.field "name" Json.string)
            (Json.field "preferred_username" Json.string)
            (Json.map (Maybe.withDefault "") <| Json.maybe <| Json.field "picture" Json.string)
    , clientId =
        "werewolf"
    , scope =
        [ "openid", "profile" ]
    }



--
-- Model
--


type alias Model =
    { redirectUri : Url
    , flow : Flow
    }


{-| This demo evolves around the following state-machine\*

        +--------+
        |  Idle  |
        +--------+
             |
             | Redirect user for authorization
             |
             v
     +--------------+
     |  Authorized  |
     +--------------+
             |
             | Exchange authorization code for an access token
             |
             v
    +-----------------+
    |  Authenticated  |
    +-----------------+
             |
             | Fetch user info using the access token
             v
         +--------+
         |  Done  |
         +--------+

(\*) The 'Errored' state hasn't been represented here for simplicity.

-}
type Flow
    = Idle
    | Authorized OAuth.AuthorizationCode
    | Authenticated OAuth.Token
    | Done UserInfo
    | Errored Error


type Error
    = ErrStateMismatch
    | ErrAuthorization OAuth.AuthorizationError
    | ErrAuthentication OAuth.AuthenticationError
    | ErrHTTPGetAccessToken
    | ErrHTTPGetUserInfo


type alias UserInfo =
    { name : String
    , username: String
    , picture: String
    }


type alias Configuration =
    { authorizationEndpoint : Url
    , tokenEndpoint : Url
    , userInfoEndpoint : Url
    , userInfoDecoder : Json.Decoder UserInfo
    , clientId : String
    , scope : List String
    }


{-| During the authentication flow, we'll run twice into the `init` function:

  - The first time, for the application very first run. And we proceed with the `Idle` state,
    waiting for the user (a.k.a you) to request a sign in.

  - The second time, after a sign in has been requested, the user is redirected to the
    authorization server and redirects the user back to our application, with a code
    and other fields as query parameters.

When query params are present (and valid), we consider the user `Authorized`.

-}
init : () -> Url -> Key -> ( Model, Cmd Msg )
init mflags origin navigationKey =
    let
        redirectUri =
            { origin | query = Nothing, fragment = Nothing }

        clearUrl =
            Navigation.replaceUrl navigationKey (Url.toString redirectUri)
    in
    case OAuth.parseCode origin of
        OAuth.Empty ->
            ( { flow = Idle, redirectUri = redirectUri }
            , Cmd.none
            )

        -- It is important to set a `state` when making the authorization request
        -- and to verify it after the redirection. The state can be anything but its primary
        -- usage is to prevent cross-site request forgery; at minima, it should be a short,
        -- non-guessable string, generated on the fly.
        --
        -- We remember any previously generated state  state using the browser's local storage
        -- and give it back (if present) to the elm application upon start
        OAuth.Success { code, state } ->
            -- case mflags of
                -- Nothing ->
                --     ( { flow = Errored ErrStateMismatch, redirectUri = redirectUri }
                --     , clearUrl
                --     )

                -- Just flags ->
                --     if state /= Just flags.state then
                --         ( { flow = Errored ErrStateMismatch, redirectUri = redirectUri }
                --         , clearUrl
                --         )

                --     else
                        ( { flow = Authorized code, redirectUri = redirectUri }
                        , Cmd.batch
                            -- Artificial delay to make the live demo easier to follow.
                            -- In practice, the access token could be requested right here.
                            [ after 750 AccessTokenRequested
                            , clearUrl
                            ]
                        )

        OAuth.Error error ->
            ( { flow = Errored <| ErrAuthorization error, redirectUri = redirectUri }
            -- , clearUrl
            , Cmd.none
            )



--
-- Msg
--


type Msg
    = NoOp
    | SignInRequested
    | GotRandomBytes (List Int)
    | AccessTokenRequested
    | GotAccessToken (Result Http.Error OAuth.AuthenticationSuccess)
    | UserInfoRequested
    | GotUserInfo (Result Http.Error UserInfo)
    | SignOutRequested


getAccessToken : Configuration -> Url -> OAuth.AuthorizationCode -> Cmd Msg
getAccessToken { clientId, tokenEndpoint } redirectUri code =
    Http.request <|
        OAuth.makeTokenRequest GotAccessToken
            { credentials =
                { clientId = clientId
                , secret = Nothing
                }
            , code = code
            , url = tokenEndpoint
            , redirectUri = redirectUri
            }


getUserInfo : Configuration -> OAuth.Token -> Cmd Msg
getUserInfo { userInfoDecoder, userInfoEndpoint } token =
    Http.request
        { method = "GET"
        , body = Http.emptyBody
        , headers = OAuth.useToken token []
        , url = Url.toString userInfoEndpoint
        , expect = Http.expectJson GotUserInfo userInfoDecoder
        , timeout = Nothing
        , tracker = Nothing
        }


randomBytes : Int -> Cmd (List Int)
randomBytes length =
    Random.generate identity
    <| Random.list length
    <| Random.int 0 255



--
-- Update
--


update : Msg -> Model -> ( Model, Cmd Msg )
update msg model =
    case ( model.flow, msg ) of
        ( Idle, SignInRequested ) ->
            signInRequested model

        ( Idle, GotRandomBytes bytes ) ->
            gotRandomBytes model bytes

        ( Authorized code, AccessTokenRequested ) ->
            accessTokenRequested model code

        ( Authorized _, GotAccessToken authenticationResponse ) ->
            gotAccessToken model authenticationResponse

        ( Authenticated token, UserInfoRequested ) ->
            userInfoRequested model token

        ( Authenticated _, GotUserInfo userInfoResponse ) ->
            gotUserInfo model userInfoResponse

        ( Done _, SignOutRequested ) ->
            signOutRequested model

        _ ->
            noOp model


noOp : Model -> ( Model, Cmd Msg )
noOp model =
    ( model, Cmd.none )


signInRequested : Model -> ( Model, Cmd Msg )
signInRequested model =
    ( { model | flow = Idle }
    , Cmd.map GotRandomBytes
        <| randomBytes 16
    )


gotRandomBytes : Model -> List Int -> ( Model, Cmd Msg )
gotRandomBytes model bytes =
    let
        { state } =
            convertBytes bytes

        authorization =
            { clientId = configuration.clientId
            , redirectUri = model.redirectUri
            , scope = configuration.scope
            , state = Just state
            , url = configuration.authorizationEndpoint
            }
    in
    ( { model | flow = Idle }
    , authorization
        |> OAuth.makeAuthorizationUrl
        |> Url.toString
        |> Navigation.load
    )


accessTokenRequested : Model -> OAuth.AuthorizationCode -> ( Model, Cmd Msg )
accessTokenRequested model code =
    ( { model | flow = Authorized code }
    , getAccessToken configuration model.redirectUri code
    )


gotAccessToken : Model -> Result Http.Error OAuth.AuthenticationSuccess -> ( Model, Cmd Msg )
gotAccessToken model authenticationResponse =
    case authenticationResponse of
        Err (Http.BadBody body) ->
            case Json.decodeString OAuth.defaultAuthenticationErrorDecoder body of
                Ok error ->
                    ( { model | flow = Errored <| ErrAuthentication error }
                    , Cmd.none
                    )

                _ ->
                    ( { model | flow = Errored ErrHTTPGetAccessToken }
                    , Cmd.none
                    )

        Err _ ->
            ( { model | flow = Errored ErrHTTPGetAccessToken }
            , Cmd.none
            )

        Ok { token } ->
            ( { model | flow = Authenticated token }
            , after 750 UserInfoRequested
            )


userInfoRequested : Model -> OAuth.Token -> ( Model, Cmd Msg )
userInfoRequested model token =
    ( { model | flow = Authenticated token }
    , getUserInfo configuration token
    )


gotUserInfo : Model -> Result Http.Error UserInfo -> ( Model, Cmd Msg )
gotUserInfo model userInfoResponse =
    case userInfoResponse of
        Err _ ->
            ( { model | flow = Errored ErrHTTPGetUserInfo }
            , Cmd.none
            )

        Ok userInfo ->
            ( { model | flow = Done userInfo }
            , Cmd.none
            )


signOutRequested : Model -> ( Model, Cmd Msg )
signOutRequested model =
    ( { model | flow = Idle }
    , Navigation.load (Url.toString model.redirectUri)
    )



--
-- View
--


type alias ViewConfiguration msg =
    { title : String
    , btnClass : Attribute msg
    }


view : ViewConfiguration Msg -> Model -> Document Msg
view ({ title } as config) model =
    { title = title
    , body = viewBody config model
    }


viewBody : ViewConfiguration Msg -> Model -> List (Html Msg)
viewBody config model =
    [ div [ class "flex", class "flex-column", class "flex-space-around" ] <|
        case model.flow of
            Idle ->
                div [ class "flex" ]
                    [ viewAuthorizationStep False
                    , viewStepSeparator False
                    , viewAuthenticationStep False
                    , viewStepSeparator False
                    , viewGetUserInfoStep False
                    ]
                    :: viewIdle config

            Authorized _ ->
                div [ class "flex" ]
                    [ viewAuthorizationStep True
                    , viewStepSeparator True
                    , viewAuthenticationStep False
                    , viewStepSeparator False
                    , viewGetUserInfoStep False
                    ]
                    :: viewAuthorized

            Authenticated _ ->
                div [ class "flex" ]
                    [ viewAuthorizationStep True
                    , viewStepSeparator True
                    , viewAuthenticationStep True
                    , viewStepSeparator True
                    , viewGetUserInfoStep False
                    ]
                    :: viewAuthenticated

            Done userInfo ->
                div [ class "flex" ]
                    [ viewAuthorizationStep True
                    , viewStepSeparator True
                    , viewAuthenticationStep True
                    , viewStepSeparator True
                    , viewGetUserInfoStep True
                    ]
                    :: viewUserInfo config userInfo

            Errored err ->
                div [ class "flex" ]
                    [ viewErroredStep
                    ]
                    :: viewErrored err
    ]


viewIdle : ViewConfiguration Msg -> List (Html Msg)
viewIdle { btnClass } =
    [ button
        [ onClick SignInRequested, btnClass ]
        [ text "Sign in" ]
    ]


viewAuthorized : List (Html Msg)
viewAuthorized =
    [ span [] [ text "Authenticating..." ]
    ]


viewAuthenticated : List (Html Msg)
viewAuthenticated =
    [ span [] [ text "Getting user info..." ]
    ]


viewUserInfo : ViewConfiguration Msg -> UserInfo -> List (Html Msg)
viewUserInfo { btnClass } { name, username, picture } =
    [ div [ class "flex", class "flex-column" ]
        [ img [ class "avatar", src picture ] []
        , p [] [ text <| name ++ " (" ++ username ++ ")" ]
        , div []
            [ button
                [ onClick SignOutRequested, btnClass ]
                [ text "Sign out" ]
            ]
        ]
    ]


viewErrored : Error -> List (Html Msg)
viewErrored error =
    [ span [ class "span-error" ] [ viewError error ] ]


viewError : Error -> Html Msg
viewError e =
    text <|
        case e of
            ErrStateMismatch ->
                "'state' doesn't match, the request has likely been forged by an adversary!"

            ErrAuthorization error ->
                oauthErrorToString { error = error.error, errorDescription = error.errorDescription }

            ErrAuthentication error ->
                oauthErrorToString { error = error.error, errorDescription = error.errorDescription }

            ErrHTTPGetAccessToken ->
                "Unable to retrieve token: HTTP request failed. CORS is likely disabled on the authorization server."

            ErrHTTPGetUserInfo ->
                "Unable to retrieve user info: HTTP request failed."


viewAuthorizationStep : Bool -> Html Msg
viewAuthorizationStep isActive =
    viewStep isActive ( "Authorization", style "left" "-110%" )


viewAuthenticationStep : Bool -> Html Msg
viewAuthenticationStep isActive =
    viewStep isActive ( "Authentication", style "left" "-125%" )


viewGetUserInfoStep : Bool -> Html Msg
viewGetUserInfoStep isActive =
    viewStep isActive ( "Get User Info", style "left" "-135%" )


viewErroredStep : Html Msg
viewErroredStep =
    div
        [ class "step", class "step-errored" ]
        [ span [ style "left" "-50%" ] [ text "Errored" ] ]


viewStep : Bool -> ( String, Attribute Msg ) -> Html Msg
viewStep isActive ( step, position ) =
    let
        stepClass =
            class "step"
                :: (if isActive then
                        [ class "step-active" ]

                    else
                        []
                   )
    in
    div stepClass [ span [ position ] [ text step ] ]


viewStepSeparator : Bool -> Html Msg
viewStepSeparator isActive =
    let
        stepClass =
            class "step-separator"
                :: (if isActive then
                        [ class "step-active" ]

                    else
                        []
                   )
    in
    span stepClass []



--
-- Helpers
--


toBytes : List Int -> Bytes
toBytes =
    List.map Bytes.unsignedInt8 >> Bytes.sequence >> Bytes.encode


base64 : Bytes -> String
base64 =
    Base64.bytes >> Base64.encode


convertBytes : List Int -> { state : String }
convertBytes =
    toBytes >> base64 >> (\state -> { state = state })


oauthErrorToString : { error : OAuth.ErrorCode, errorDescription : Maybe String } -> String
oauthErrorToString { error, errorDescription } =
    let
        desc =
            errorDescription |> Maybe.withDefault "" |> String.replace "+" " "
    in
    OAuth.errorCodeToString error ++ ": " ++ desc


defaultHttpsUrl : Url
defaultHttpsUrl =
    { protocol = Https
    , host = ""
    , path = ""
    , port_ = Nothing
    , query = Nothing
    , fragment = Nothing
    }
