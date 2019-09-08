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

namespace Mediocre_Mapper_Pull_BSIPA
{
    public class MediocrePuller : MonoBehaviour
    {
        public static MediocrePuller Instance;

        private bool _runningPull;
        private bool _welcomeDone;
        private bool _conversionDone;

        private string[] _components;
        private StatusText _statusText;
        private string _errorMessage;

        public static void OnLoad()
        {
            if (Instance != null)
            {
                return;
            }

            new GameObject("Mediocre Mapper puller").AddComponent<MediocrePuller>();
        }

        private void Awake()
        {
            Instance = this;
            _statusText = StatusText.Create();
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
                _statusText.ShowMessage($"Connecting to {host}:{port}...");
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
                    _statusText.ShowMessage(_errorMessage);
                    yield break;
                }

                var folderName = _components[0].Split(new[] {"::"}, StringSplitOptions.None)[0];
                var difficultyFilename = _components[2];
                var difficultyContent = _components[3];
                Logger.log.Debug($"Folder: {folderName}\nDifficulty: {difficultyFilename}\nDifficulty size: {difficultyContent.Length} characters");
                _statusText.ShowMessage($"Got song {folderName}");

                var customSongsPath = $"{BeatSaber.InstallPath}\\Beat Saber_Data\\CustomWIPLevels";
                var songPath = $"{customSongsPath}\\{folderName}";
                var difficultyPath = $"{songPath}\\{difficultyFilename}";

                Logger.log.Debug($"Writing to {difficultyPath}");
                using (var outputDifficulty = File.CreateText(difficultyPath))
                {
                    outputDifficulty.Write(difficultyContent);
                }
                Logger.log.Debug("Wrote file");

                var converterPath = $"{BeatSaber.InstallPath}\\songe-converter.exe";
                if (File.Exists(converterPath))
                {
                    // Checking the exit code of the process isn't helpful here because once the arguments are
                    // validated, songe-converter exits 0 regardless of whether it converted a song.
                    var process = new Process();
                    var startInfo = new ProcessStartInfo
                    {
                        WindowStyle = ProcessWindowStyle.Normal,
                        FileName = converterPath,
                        Arguments = $"-k -a \"{customSongsPath}\"",
                        UseShellExecute = false,
                    };
                    process.StartInfo = startInfo;
                    process.EnableRaisingEvents = true;
                    process.Exited += Process_Exited;
                    _conversionDone = false;

                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    process.Start();
                    Logger.log.Debug("Started conversion.");

                    yield return new WaitUntil(() => _conversionDone);

                    Logger.log.Debug($"Conversion complete in {stopwatch.Elapsed}; refreshing songs.");
                    _statusText.ShowMessage($"Conversion complete; refreshing {folderName}", 3);
                    SongCore.Loader.Instance.RefreshSongs();
                }
                else
                {
                    _errorMessage = $"{converterPath} does not exist; can't convert";
                    _statusText.ShowMessage(_errorMessage);
                    Logger.log.Info(_errorMessage);
                }
            }
            finally
            {
                _runningPull = false;
            }
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            _conversionDone = true;
        }

        private string[] ReadWelcomeMessage(string host, int port)
        {
            var stopwatch = new Stopwatch();
            var message = new StringBuilder();
            var buffer = new byte[4096];

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
                    //  5: audio file size
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
                _errorMessage = $"Failed to connect to {host}:{port}: {e.Message}";
                Logger.log.Error(_errorMessage);
                throw;
            }
            catch (IOException e)
            {
                _errorMessage = $"Networking error: {e.Message}";
                Logger.log.Error(_errorMessage);
                throw;
            }

            Logger.log.Debug($"Read welcome message in {stopwatch.Elapsed} seconds");

            return message.ToString().Split(new[] {";;;"}, StringSplitOptions.None);
        }
    }
}