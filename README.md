# song-refresh-button

Adds various "refresh" buttons in-game, so you don't have to take off your HMD to refresh your songs!

1. Refresh songs
2. Refresh level packs
3. Pull WIP map from a [Mediocre Mapper server](https://github.com/squeaksies/MediocreMapper/blob/master/ServerReadme.md#mediocre-mapper-multi-mapper-server-setup)

Choose which buttons, if any, to have on the main pane using the _MODS_ menu.

## Set up with Mediocre Mapper Multi-Mapper server pull

0. Install this plugin. It requires the `SongCore` plugin and its copy of `songe-converter`, as well as `BS_Utils` and `BeatSaberCustomUI`.
1. Copy the folder with the song from the server's songs folder to `CustomWIPLevels` inside your Beat Saber installation folder. (For example, `C:\Program Files (x86)\Steam\steamapps\common\Beat Saber\Beat Saber_Data\CustomWIPLevels`.)
2. Start the server.
3. By default, the plugin will pull from a server running on the same computer on MMMM's default port of `17425`. To change this, open or create `song-refresh-button.ini` in `UserData`, and modify its contents accordingly:

```
[MMMM]
hostname = 127.0.0.1
port = 17425

```

4. Now to try it out: find your map in the "WIP Maps" level pack.
5. Connect to the server and make some obvious modifications, such as filling the start with easily deleted garbage blocks.
6. Return to the main menu pane, find the _MediocreMapper Pull_ button, possibly under the _MODS_ button, and try it!
7. Wait a few seconds for the plugin to join the server, convert the map, and refresh the songs. If there are any changes a song loading progress bar and messages will appear above the main pane, like it does when the game starts. If it works, congratulations and have fun!

If it doesn't work, check the logs under `Logs\Song Refresh Button` and `Logs\Unity Engine` in the Beat Saber installation directory for clues.
