# ROLL AND CASH: GROCERY LORDS

A game made in 48 hours for Global Game Jam 2024 using the [MoonWorks](https://github.com/MoonsideGames/MoonWorks) game framework.

Available [on itch.io](https://prophetgoddess.itch.io/roll-and-cash-grocery-lords).

In December 2024, Evan upgraded the code to use the latest version of MoonWorks. This now serves as a decent example of a small but complete game that uses the MoonWorks framework.

To compile this game, follow the following steps

1. First download the latest version of Moonlibs (prebuilt dlls) from [here](https://moonside.games/files/moonlibs.tar.gz) and unzip the moonlibs folder into the root directory of this repository.

- (Optional since the repo already comes with shadercross: You can download the relevant version of Shadercross from [here](https://nightly.link/libsdl-org/SDL_shadercross/workflows/main/main?preview) and unzip its contents. Then move the lib folder to ./ContentBuilder/ContentBuilderUI/bin/Debug and move the shadercross executable to ./ContentBuilder/ContentBuilderUI/bin/Debug/net9.0 )

2. You will then need to run the ContentBuilderUI project first. This will generate the content files necessary to build and run the game.

- In the process, you may encounter the following error message

- "ContentBuilderUI.csproj: Error NU1105 : Unable to find project information for '/Users/dev/Documents/GitHub/Moonworks/Tactician/ContentBuilder/ContentBuilderUI/lib/ImGui.NET/ImGui.NET.csproj'. If you are using Visual Studio, this may be because the project is unloaded or not part of the current solution so run a restore from the command-line. Otherwise, the project file may be invalid or missing targets required for restore."

- If you do, try using dotnet restore

- Then navigate to the ImGui.NET.csproj file (which is in ./ContentBuilder/ContentBuilderUI/lib/ImGui.NET) in your terminal or powershell and run:
dotnet build ImGui.NET.csproj

- Then try running the ContentBuilderUI project again.

3. Once you have that up and running, you'll need to copy the paths to two directories and past them into the application. NOTE: If you're on a Mac, the paste shortcut is actually CONTROL+V, not CMD+V for this application.

- The first path is should look something like whatever/directories/lead/to/GGJ2024/ContentBuilder/ContentSource

- The second path looks more like whatever/directories/lead/to/GGJ2024

3. Once you have them both correct, the text should turn green and a few buttons should appear. Click on the "Build Content" button to build all of the content you'll need to run the game. NOTE: You'll need to re-run this every time you change an asset in that ContentSource folder before running your game.

4. If you get errors complaining that the fonts aren't initialized, go back to the content builder and try rebuilding the fonts to make the errors go away

## Licenses

ROLL AND CASH: GROCERY LORDS is released under multiple licenses, contained in the licenses folder.

Code is released under the zlib license: code.LICENSE\
Assets are released under the CC-BY-SA 4.0 license: assets.LICENSE

Kosugi font is licensed under the Apache License, Version 2.0\
Pixeltype font was made by TheJman0205 with FontStruct. It can be used by anyone for free.
