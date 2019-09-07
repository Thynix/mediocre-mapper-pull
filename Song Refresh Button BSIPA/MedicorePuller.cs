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

        private bool _runningPull;
        private bool _welcomeDone;
        private bool _conversionDone;

        private string[] _components;

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

        public void Pull(string host, int port)
        {
            if (SceneManager.GetActiveScene().name != "MenuCore" || _runningPull)
            {
                return;
            }

            _runningPull = true;
            StartCoroutine(PullSong(host, port));
        }

        private IEnumerator PullSong(string host, int port)
        {
            try
            {
                _components = null;
                _welcomeDone = false;
                new Thread(delegate(object o)
                {
                    try
                    {
                        _components = ReadWelcomeMessage(host, port);
                    }
                    finally
                    {
                        _welcomeDone = true;
                    }
                }).Start();

                // If _components is not set, there was a problem.
                yield return new WaitUntil(() => _welcomeDone);
                if (_components == null)
                {
                    yield break;
                }

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
                    var process = new Process();
                    var startInfo = new ProcessStartInfo();
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

        private string[] ReadWelcomeMessage(string host, int port)
        {
            var stopwatch = new Stopwatch();
            var message = new StringBuilder();
            var buffer = new byte[4096];

            // TODO: How to display progress? text requires a transform.
            Logger.log.Debug($"Connecting with TCP to {host}:{port}");
            stopwatch.Start();

            try
            {
                using (var client = new TcpClient(host, port))
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
            }
            catch (SocketException e)
            {
                Logger.log.Error($"Failed to connect to {host}:{port}: {e.Message}");
                throw;
            }
            catch (IOException e)
            {
                Logger.log.Error($"Networking error: {e.Message}");
                throw;
            }

            Logger.log.Debug($"Read welcome message in {stopwatch.Elapsed} seconds");

            return message.ToString().Split(new[] {";;;"}, StringSplitOptions.None);
        }
    }
}