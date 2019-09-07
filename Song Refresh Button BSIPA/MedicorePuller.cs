using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Song_Refresh_Button_BSIPA
{
    public class MedicorePuller : MonoBehaviour
    {
        public static MedicorePuller Instance;

        private static bool _runningPull;
        private static bool _conversionDone;

        private static string[] _components;

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
            _runningPull = false;
            DontDestroyOnLoad(gameObject);
        }

        // TODO: Take MediocreMapper host and port as arguments, pulled from plugin config
        public void Pull()
        {
            if (SceneManager.GetActiveScene().name != "MenuCore" || _runningPull)
            {
                return;
            }

            _runningPull = true;
            StartCoroutine(PullSong());
        }

        private IEnumerator PullSong()
        {
            try
            {
                _components = null;
                new Thread(delegate(object o) { _components = ReadWelcomeMessage(); }).Start();

                yield return new WaitUntil(() => _components != null);

                var folderName = _components[0].Split(new[] {"::"}, StringSplitOptions.None)[0];
                var difficultyFilename = _components[2];
                var difficultyContent = _components[3];
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

                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    process.Start();
                    Logger.log.Debug($"Started conversion.");

                    yield return new WaitUntil(() => _conversionDone);

                    Logger.log.Debug($"Conversion complete in {stopwatch.Elapsed}; refreshing songs.");
                    SongCore.Loader.Instance.RefreshSongs();
                }
                else
                {
                    Logger.log.Info($"{converterPath} does not exist; can't convert.'");
                }
            }
            finally
            {
                _runningPull = false;
            }
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            // TODO: persist process handle and check exit code?
            _conversionDone = true;
        }

        private string[] ReadWelcomeMessage()
        {
            var stopwatch = new Stopwatch();
            var message = new StringBuilder();
            var buffer = new byte[4096];

            // TODO: How to display progress? text requires a transform.
            Logger.log.Debug("Opening TCP connection");
            stopwatch.Start();
            // TODO: Connection error notification
            using (var client = new TcpClient("localhost", 17425))
            using (var stream = client.GetStream())
            {
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
                Logger.log.Debug("Reading welcome message");
                stopwatch.Restart();
                while (Regex.Matches(message.ToString(), ";;;").Count < 4)
                {
                    var bytesRead = stream.Read(buffer, 0, buffer.Length);
                    message.Append(Encoding.UTF8.GetChars(new ArraySegment<byte>(buffer, 0, bytesRead).ToArray()));
                }
            }

            Logger.log.Debug($"Read welcome message in {stopwatch.Elapsed} seconds");

            return message.ToString().Split(new[] {";;;"}, StringSplitOptions.None);
        }
    }
}