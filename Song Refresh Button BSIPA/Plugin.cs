using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using IPA;
using IPA.Config;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;
    

namespace Song_Refresh_Button_BSIPA
{
    public class Plugin : IBeatSaberPlugin
    {
        internal static Ref<PluginConfig> config;
        internal static IConfigProvider configProvider;

        public void Init(IPALogger logger, [Config.Prefer("json")] IConfigProvider cfgProvider)
        {
            Logger.log = logger;
            configProvider = cfgProvider;

            config = cfgProvider.MakeLink<PluginConfig>((p, v) =>
            {
                if (v.Value == null || v.Value.RegenerateConfig)
                    p.Store(v.Value = new PluginConfig() { RegenerateConfig = false });
                config = v;
            });
        }

        public void OnApplicationStart()
        {
            Logger.log.Debug("OnApplicationStart");
        }

        public void OnApplicationQuit()
        {
            Logger.log.Debug("OnApplicationQuit");
        }

        public void OnFixedUpdate()
        {

        }

        public void OnUpdate()
        {

        }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {

        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            if (scene.name == "MenuCore")
            {
                CustomUI.MenuButton.MenuButtonUI.AddButton("Refresh Songs", "Refreshes song library", delegate { SongCore.Loader.Instance.RefreshSongs(); });
                CustomUI.MenuButton.MenuButtonUI.AddButton("Refresh Level Packs", "Refreshes level packs", delegate { SongCore.Loader.Instance.RefreshLevelPacks(); });
                CustomUI.MenuButton.MenuButtonUI.AddButton("MediocreMapper Pull", "Pulls from Mediocre Mapper server (does not convert or refresh)", PullSong);
            }
        }
        
        public void PullSong()
        {
            // TODO: Pull from config:
            //         host and port
            //         JSON output directory (together with folder name from message?)
            //         conversion target directory
            // TODO: How to display progress? text requires a transform.
            // TODO: Thread out for conversion and notify on completion - I'm seeing stuff with coroutines as well.
            var outputDir = @"E:\Beat Saber Mapping\Working Repos\CustomSongs";
            Logger.log.Debug("Opening TCP connection");
            // TODO: Connection error notification
            var client = new TcpClient("localhost", 17425);
            var stream = client.GetStream();
            
            Logger.log.Debug("TCP connected");
            
            var username = Encoding.UTF8.GetBytes("BeatSaber refresh");
            stream.Write(username, 0, username.Length);
            Logger.log.Debug("Sent username");
            
            // Receive until difficulty contents field (index 3) is received, separated by ";;;":
            //  0: folder name::difficulty index
            //  1: contents of info.json
            //  2: path to relevant difficulty.json
            //  3: contents of difficulty.json
            //  4: audio filename
            //  5: audio filesize
            //  6: audio download url
            // TODO: Could optimize to not recheck for ;;; beyond uhhh up to last 2 previous characters (in case ;;; gets split between chunks) doesn't seem remotely worth it.
            // TODO: will this kill the server if it closes the stream during the welcome message? (it if prevents the welcome message from being entirely sent anyway)
            var message = new StringBuilder();
            var buffer = new byte[4096];
            var welcomeStart = Time.time;
            Logger.log.Debug("Reading welcome message");
            while (message.ToString().Split(new[] {";;;"}, StringSplitOptions.None).Length < 5)
            {
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                message.Append(Encoding.UTF8.GetChars(new ArraySegment<byte>(buffer, 0, bytesRead).ToArray()));
            }
            client.Close();

            var components = message.ToString().Split(new[] {";;;"}, StringSplitOptions.None);
            
            Logger.log.Debug($"Read welcome message in {Time.time - welcomeStart:f2} seconds");

            var folderName = components[0].Split(new[] {"::"}, StringSplitOptions.None)[0];
            var difficultyFilename = components[2];
            var difficultyContent = components[3];
            Logger.log.Debug($"Folder: {folderName}\nDifficulty: {difficultyFilename}\nDifficulty size: {difficultyContent.Length} characters");

            var outputPath = $"{outputDir}\\{folderName}\\{difficultyFilename}";
            Logger.log.Debug($"Writing to {outputPath}");
            using (var outputDifficulty = File.CreateText(outputPath))
            {
                outputDifficulty.Write(difficultyContent);
            }
            
            Logger.log.Debug("Wrote file");
        }

        public void OnSceneUnloaded(Scene scene)
        {

        }
    }
}
