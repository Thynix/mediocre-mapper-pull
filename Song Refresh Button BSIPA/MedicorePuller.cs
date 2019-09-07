using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Song_Refresh_Button_BSIPA
{
    public class MedicorePuller : MonoBehaviour
    {
        public static MedicorePuller Instance;
        private static bool _conversionDone;

        public static void OnLoad()
        {
            if (Instance != null)
            {
                return;
            }

            new GameObject("Mediocre Mapper puller").AddComponent<MedicorePuller>();
        }

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // TODO: Take MediocreMapper host and port as arguments, pulled from plugin config
        public void Pull()
        {
            if (SceneManager.GetActiveScene().name != "MenuCore")
            {
                return;
            }

            // TODO: Avoid running more than one at once?
            StartCoroutine(PullSong());
        }

        private IEnumerator PullSong()
        {
            var stopwatch = new Stopwatch();

            // TODO: How to display progress? text requires a transform.
            // TODO: Thread out for conversion and notify on completion - I'm seeing stuff with coroutines as well.
            // TODO: Maybe coroutine that waits for non-null message string to set?
            // TODO: Break into ReadWelcomeMessage()
            Logger.log.Debug("Opening TCP connection");
            stopwatch.Start();
            // TODO: Connection error notification
            var client = new TcpClient("localhost", 17425);
            var stream = client.GetStream();
            Logger.log.Debug($"TCP connected in {stopwatch.Elapsed}");

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
            Logger.log.Debug("Reading welcome message");
            stopwatch.Restart();
            while (message.ToString().Split(new[] {";;;"}, StringSplitOptions.None).Length < 5)
            {
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                message.Append(Encoding.UTF8.GetChars(new ArraySegment<byte>(buffer, 0, bytesRead).ToArray()));
            }
            client.Close();
            Logger.log.Debug($"Read welcome message in {stopwatch.Elapsed} seconds");

            // TODO: Go out to thread for network and wait as coroutine? Like with conversion.
            var components = message.ToString().Split(new[] {";;;"}, StringSplitOptions.None);

            var folderName = components[0].Split(new[] {"::"}, StringSplitOptions.None)[0];
            var difficultyFilename = components[2];
            var difficultyContent = components[3];
            Logger.log.Debug($"Folder: {folderName}\nDifficulty: {difficultyFilename}\nDifficulty size: {difficultyContent.Length} characters");

            var customSongsPath = $"{BeatSaber.InstallPath}\\Beat Saber_Data\\CustomWIPLevels";
            var songPath = $"{customSongsPath}\\{folderName}";
            var difficultyPath = $"{songPath}\\{difficultyFilename}";
            Logger.log.Debug($"Writing to {difficultyPath}");
            using (var outputDifficulty = File.CreateText(difficultyPath))
            {
                outputDifficulty.Write(difficultyContent);
            }
            Logger.log.Debug("Wrote file");

            var converterPath = BeatSaber.InstallPath + "\\songe-converter.exe";
            if (File.Exists(converterPath))
            {
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.FileName = converterPath;
                startInfo.Arguments = $"-k -a \"{customSongsPath}\"";
                startInfo.UseShellExecute = false;
                process.StartInfo = startInfo;
                process.EnableRaisingEvents = true;
                process.Exited += Process_Exited;

                stopwatch.Restart();
                process.Start();
                Logger.log.Debug($"Started conversion.");

                yield return new WaitUntil(() => _conversionDone);

                Logger.log.Debug($"Conversion complete in {stopwatch.Elapsed}; refreshing songs.");
                SongCore.Loader.Instance.RefreshSongs();
            }
            else
            {
                Logger.log.Info($"{converterPath} does not exit; can't convert.'");
                // TODO: Is there a way to exit a coroutine? Will this branch hang?
            }
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            // TODO: persist process handle and check exit code?
            _conversionDone = true;
        }
    }
}