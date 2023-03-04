
:: figure out where the batch file is
set workingDir=%~dp0
set pathToMod=%workingDir%bin\Debug\net46

:: lol
set pathToModLocation=F:\Games\SteamGames\steamapps\common\PlagueInc\ScenarioCreator\BepInEx\plugins\SCUnshackled

:: create mod folder if it doesn't exist
if not exist "%pathToModLocation%" mkdir "%pathToModLocation%"

:: copy other files that may be required
robocopy "%workingDir%Files" "%pathToModLocation%"

:: copy mod dll
echo F|xcopy "%pathToMod%\SCUnshackled.dll" "%pathToModLocation%\SCUnshackled.dll" /y