using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using SongCore.Utilities;

namespace Mediocre_Mapper_Pull_BSIPA
{
    public class StatusText : MonoBehaviour
    {
        private Canvas _canvas;
        private TMP_Text _statusText;

        private static readonly Vector3 Position = new Vector3(0, 0.5f, 2.5f);
        private static readonly Vector3 Rotation = new Vector3(0, 0, 0);
        private static readonly Vector3 Scale = new Vector3(0.01f, 0.01f, 0.01f);

        private static readonly Vector2 CanvasSize = new Vector2(100, 50);

        private static readonly Vector2 HeaderPosition = new Vector2(10, 15);
        private static readonly Vector2 HeaderSize = new Vector2(100, 20);
        private const float HeaderFontSize = 15f;

        private bool _showingMessage;

        public static StatusText Create()
        {
            return new GameObject("Status Text").AddComponent<StatusText>();
        }

        public void ShowMessage(string message, float time)
        {
            StopAllCoroutines();
            _showingMessage = true;
            _statusText.text = message;
            _canvas.enabled = true;
            StartCoroutine(DisableCanvasRoutine(time));
        }

        public void ShowMessage(string message)
        {
            StopAllCoroutines();
            _showingMessage = true;
            _statusText.text = message;
            _canvas.enabled = true;
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;
        }

        private void SceneManagerOnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            if (newScene.name == "MenuCore" && _showingMessage)
            {
                _canvas.enabled = true;
            }
            else
            {
                _canvas.enabled = false;
            }
        }

        private IEnumerator DisableCanvasRoutine(float time)
        {
            yield return new WaitForSecondsRealtime(time);
            _canvas.enabled = false;
            _showingMessage = false;
        }

        private void Awake()
        {
            var o = gameObject;
            o.transform.position = Position;
            o.transform.eulerAngles = Rotation;
            o.transform.localScale = Scale;

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.enabled = false;
            var rectTransform = _canvas.transform as RectTransform;
            rectTransform.sizeDelta = CanvasSize;

            _statusText = Utils.CreateText(_canvas.transform as RectTransform, "", HeaderPosition);
            rectTransform = _statusText.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.anchoredPosition = HeaderPosition;
            rectTransform.sizeDelta = HeaderSize;
            _statusText.fontSize = HeaderFontSize;
            _statusText.alignment = TextAlignmentOptions.Center;

            DontDestroyOnLoad(gameObject);
        }
    }
}