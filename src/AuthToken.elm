module AuthToken exposing (..)

import OAuth.AuthorizationCode as Auth
import OAuth.Refresh
import Time exposing (Posix)
import Http
import Config
import Config exposing (oauthBaseServerUrl)

type alias AuthToken =
    { token: Auth.AuthenticationSuccess
    , now: Posix
    , expire: Maybe Posix
    , requested: Bool
    , lost: Bool
    }

init : Auth.AuthenticationSuccess -> AuthToken
init token =
    { token = token
    , now = Time.millisToPosix 0
    , expire = Nothing
    , requested = False
    , lost = False
    }

type Msg
    = Time Posix
    | Update (Result Http.Error Auth.AuthenticationSuccess)

update : Msg -> AuthToken -> (AuthToken, Cmd Msg)
update msg auth =
    case msg of
        Update (Ok token) ->
            Tuple.pair
                { auth
                | token = token
                , expire = Nothing
                , requested = False
                , lost = False
                }
                Cmd.none
        Update (Err _) ->
            Tuple.pair
                { auth
                | lost = True
                }
                Cmd.none
        Time now ->
            let
                expire : Posix
                expire = case auth.expire of
                    Just x -> x
                    Nothing ->
                        auth.token.expiresIn
                            |> Debug.log "auth:time:expire:expire-in"
                            |> Maybe.withDefault 300
                            |> (*) 1000
                            |> (+) (Time.posixToMillis now)
                            |> Debug.log "auth:time:expire:result"
                            |> Time.millisToPosix

                new : AuthToken
                new =
                    { auth 
                    | now = now
                    , expire = Just expire
                    }

            in 
                if Debug.log "auth:time:refresh" <| Time.posixToMillis expire <= Time.posixToMillis now
                    && not auth.requested
                then case auth.token.refreshToken of
                    Just token ->
                        Tuple.pair
                            { new | requested = True }
                        <| Http.request 
                        <| OAuth.Refresh.makeTokenRequest
                            Update
                            { credentials = Just
                                { clientId = Config.oauthClientId
                                , secret = ""
                                }
                            , url =
                                { oauthBaseServerUrl
                                | path = Config.oauthTokenEndpoint
                                }
                            , scope = auth.token.scope
                            , token = token
                            }
                    Nothing ->
                        Tuple.pair
                            { new | requested = True, lost = True }
                            Cmd.none
                else (new, Cmd.none)

subscriptions : AuthToken -> Sub Msg
subscriptions auth =
    if auth.lost
    then Sub.none
    else Time.every 1000 Time
