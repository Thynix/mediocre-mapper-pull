using BS_Utils.Utilities;
using IPA;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;
    

namespace Mediocre_Mapper_Pull_BSIPA
{
    public class Plugin : IBeatSaberPlugin
    {
        private Config _config;

        public void Init(IPALogger logger)
        {
            Logger.log = logger;

            _config = new Config("mediocre-mapper-pull");
        }

        public void OnApplicationStart()
        {
        }

        public void OnApplicationQuit()
        {
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
            if (scene.name != "MenuCore") return;

            MediocrePuller.OnLoad();
            CustomUI.MenuButton.MenuButtonUI.AddButton("MediocreMapper Pull", "Pulls from Mediocre Mapper server",
                delegate
                {
                    MediocrePuller.Instance.Pull(
                        _config.GetString("MMMM", "hostname", "127.0.0.1", true),
                        _config.GetInt("MMMM", "port", 17425, true));
                });
        }

        public void OnSceneUnloaded(Scene scene)
        {
        }
    }
}
