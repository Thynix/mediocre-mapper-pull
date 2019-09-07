# Mediocre Mapper Pull

This Beat Saber plugin adds a button in-game to pull WIP map from a [Mediocre Mapper Multi-Mapper server](https://github.com/squeaksies/MediocreMapper/blob/master/ServerReadme.md#mediocre-mapper-multi-mapper-server-setup).

Choose whether to have the button on the main pane using the _MODS_ menu.

## Setup

0. Install this plugin. It requires the `SongCore` plugin and its copy of `songe-converter`, as well as `BS_Utils` and `BeatSaberCustomUI`.
1. Copy the folder with the song from the server's songs folder to `CustomWIPLevels` inside your Beat Saber installation folder. (For example, `C:\Program Files (x86)\Steam\steamapps\common\Beat Saber\Beat Saber_Data\CustomWIPLevels`.)
2. Start the server.
3. By default, the plugin will pull from a server running on the same computer on MMMM's default port of `17425`. To change this, open or create `mediocre-mapper-pull.ini` in `UserData`, and modify its contents accordingly:

```
[MMMM]
hostname = 127.0.0.1
port = 17425

```

4. Now to try it out: find your map in the "WIP Maps" level pack.
5. Connect to the server and make some obvious modifications, such as filling the start with easily deleted garbage blocks.
6. Return to the main menu pane, find the _MediocreMapper Pull_ button, possibly under the _MODS_ button, and try it!
7. Wait a few seconds for the plugin to join the server, convert the map, and refresh the songs. If there are any changes a song loading progress bar and messages will appear above the main pane, like it does when the game starts. If it works, congratulations and have fun!

If it doesn't work, check the logs under `Logs\Mediocre Mapper Pull` and `Logs\Unity Engine` in the Beat Saber installation directory for clues.

## Contributing to mediocre-mapper-pull

The project checks for the default Beat Saber installation folders for Oculus and Steam, and sets references relative to whichever exists. If your Beat Saber is installed elsewhere, create `Mediocre Mapper Pull BSIPA.csproj.user` in the project directory and modify it to reflect your setup:

```
<?xml version="1.0" encoding="utf-8"?>
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <BeatSaberPath>C:\Path\to\Beat Saber</BeatSaberPath>
  </PropertyGroup>
</Project>
```
