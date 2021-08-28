module Config exposing (..)

import Url exposing (Url)

{- This config contains some hardcoded values for the web app.
If you want to change them you have to change them here and recompile
the whole application.
-}

{- # OAuth Settings
-}

oauthBaseServerUrl : Url
oauthBaseServerUrl =
    { protocol = Url.Https
    , host = "auth.2complex.de"
    , port_ = Nothing
    , path = ""
    , query = Nothing
    , fragment = Nothing
    }

oauthAuthorizationEndpoint : String
oauthAuthorizationEndpoint = "/auth/realms/2complex/protocol/openid-connect/auth"

oauthTokenEndpoint : String
oauthTokenEndpoint = "/auth/realms/2complex/protocol/openid-connect/token"

oauthUserInfoEndpoint : String
oauthUserInfoEndpoint = "/auth/realms/2complex/protocol/openid-connect/userinfo"

oauthUsernameMap : String
oauthUsernameMap = "preferred_username"

oauthPictureMap : String
oauthPictureMap = "picture"

oauthEmailMap : String
oauthEmailMap = "email"

oauthClientId : String
oauthClientId = "werewolf"

{- # Pronto Settings
-}

prontoHost : String
prontoHost = "https://pronto.2complex.de"

version : String
version = "debug"
