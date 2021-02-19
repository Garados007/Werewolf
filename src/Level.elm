module Level exposing
    ( Level
    , LevelData
    , getData
    , init
    , isAnimating
    , updateData
    )

import Time exposing (Posix)

type alias LevelData =
    { level: Int
    , xp: Int
    , maxXp: Int
    }

type Level = Level
    { data: LevelData
    , old: LevelData
    , changed: Posix
    }

init : Posix -> LevelData -> Level
init time data =
    Level
        { data = data
        , old = data
        , changed = time
        }

getData : Posix -> Level -> LevelData
getData time (Level level) =
    if level.data == level.old
    then level.data
    else
        let 
            timeDiff : Int
            timeDiff = (Time.posixToMillis time) - (Time.posixToMillis level.changed)

            showLevel : Int
            showLevel = min level.data.level
                <| (+) level.old.level
                <| timeDiff // 1000
            
            minXP : Int
            minXP =
                if showLevel == level.old.level
                then level.old.xp
                else 0
            
            maxXP : Int
            maxXP =
                if showLevel == level.data.level
                then level.data.xp
                else if showLevel == level.old.level
                then level.old.maxXp
                else 1000

            realMaxXP : Int
            realMaxXP =
                if showLevel == level.data.level
                then level.data.maxXp
                else if showLevel == level.old.level
                then level.old.maxXp
                else 1000
            
            currentTime : Int
            currentTime = timeDiff - 1000 * (showLevel - level.old.level)

            xp : Int
            xp = min maxXP
                <| (+) minXP
                <| round
                <| (*) (toFloat <| maxXP - minXP)
                <| (toFloat currentTime) / 1000
        in
            { level = showLevel
            , xp = xp
            , maxXp = realMaxXP
            }

updateData : Posix -> LevelData -> Level -> Level
updateData time data (Level level) =
    Level
        { data = data
        -- continue the animation
        , old = getData time <| Level level
        , changed = time
        }

isAnimating : Level -> Bool
isAnimating (Level level) =
    level.data /= level.old
