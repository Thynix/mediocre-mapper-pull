using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mediocre_Mapper_Pull_BSIPA
{
    public class MediocrePuller : MonoBehaviour
    {
        public static MediocrePuller Instance;

        private bool _runningPull;
        private StatusText _statusText;

        private struct SongFields
        {
            public string FolderName;
            public string DifficultyFilename;
            public string DifficultyContent;
        }

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
                _statusText.ShowMessage($"Connecting to {host}:{port}...");

                var readWelcome = Task.Run(() => ReadWelcomeMessage(host, port));
                yield return new WaitUntil(() => readWelcome.IsCompleted);
                if (readWelcome.IsFaulted)
                {
                    _statusText.ShowMessage(readWelcome.Exception.InnerException.Message);
                    Logger.log.Error(readWelcome.Exception);
                    yield break;
                }

                var songFields = readWelcome.Result;
                _statusText.ShowMessage($"Got song {songFields.FolderName}");

                var convertSong = Task.Run(() => ConvertSong(readWelcome.Result));
                yield return new WaitUntil(() => convertSong.IsCompleted);
                if (convertSong.IsFaulted)
                {
                    _statusText.ShowMessage(convertSong.Exception.InnerException.Message);
                    Logger.log.Error(convertSong.Exception);
                    yield break;
                }

                _statusText.ShowMessage($"Conversion complete; refreshing {songFields.FolderName}", 3);
                SongCore.Loader.Instance.RefreshSongs();
            }
            finally
            {
                _runningPull = false;
            }
        }

        private static SongFields ReadWelcomeMessage(string host, int port)
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
                throw new Exception($"Failed to connect to {host}:{port}: {e.Message}", e);
            }
            catch (IOException e)
            {
                throw new Exception($"Networking error: {e.Message}", e);
            }

            Logger.log.Debug($"Read welcome message in {stopwatch.Elapsed} seconds");

            var components = message.ToString().Split(new[] {";;;"}, StringSplitOptions.None);
            var fields = new SongFields
            {
                FolderName = components[0].Split(new[] {"::"}, StringSplitOptions.None)[0],
                DifficultyFilename = components[2],
                DifficultyContent = components[3],
            };

            Logger.log.Debug($"Difficulty size: {fields.DifficultyContent.Length} characters");

            return fields;
        }

        private static void ConvertSong(SongFields fields)
        {
                var customSongsPath = $"{BeatSaber.InstallPath}\\Beat Saber_Data\\CustomWIPLevels";
                var songPath = $"{customSongsPath}\\{fields.FolderName}";
                var difficultyPath = $"{songPath}\\{fields.DifficultyFilename}";

                Logger.log.Debug($"Writing to {difficultyPath}");
                using (var outputDifficulty = File.CreateText(difficultyPath))
                {
                    outputDifficulty.Write(fields.DifficultyContent);
                }
                Logger.log.Debug("Wrote file");

                // Checking the exit code of the process isn't helpful here because once the arguments are
                // validated, songe-converter exits 0 regardless of whether it converted a song.
                var process = new Process();
                var startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Normal,
                    FileName = $"{BeatSaber.InstallPath}\\songe-converter.exe",
                    Arguments = $"-k -a \"{customSongsPath}\"",
                    UseShellExecute = false,
                };
                process.StartInfo = startInfo;
                process.EnableRaisingEvents = true;

                Logger.log.Debug("Starting conversion.");
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                try
                {
                    process.Start();
                }
                catch (Win32Exception e)
                {
                    throw new Exception($"Failed to run songe-converter at\n{process.StartInfo.FileName}\n{new Win32Exception(e.NativeErrorCode).Message}", e);
                }

                process.WaitForExit();
                Logger.log.Debug($"Conversion complete in {stopwatch.Elapsed}; refreshing songs.");
        }
    }
}