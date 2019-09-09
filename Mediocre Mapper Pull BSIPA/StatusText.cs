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

        private static readonly Vector3 Position = new Vector3(0, 0.1f, 2.5f);
        private static readonly Vector3 Rotation = new Vector3(0, 0, 0);
        private static readonly Vector3 Scale = new Vector3(0.01f, 0.01f, 0.01f);

        private static readonly Vector2 CanvasSize = new Vector2(325, 100);
        private static readonly Vector2 TextPosition = new Vector2(0, 0);
        private const float HeaderFontSize = 15f;

        public static StatusText Create()
        {
            return new GameObject("Status Text").AddComponent<StatusText>();
        }

        public void ShowMessage(string message, float time)
        {
            StopAllCoroutines();
            _statusText.text = message;
            _canvas.enabled = true;
            StartCoroutine(DisableCanvasRoutine(time));
        }

        public void ShowMessage(string message)
        {
            StopAllCoroutines();
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
            // Clear the message, if any, when changing away. It's not useful to persist.
            if (newScene.name != "MenuCore")
            {
                _canvas.enabled = false;
            }
        }

        private IEnumerator DisableCanvasRoutine(float time)
        {
            yield return new WaitForSecondsRealtime(time);
            _canvas.enabled = false;
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

            _statusText = Utils.CreateText(_canvas.transform as RectTransform, "", TextPosition);
            rectTransform = _statusText.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.anchoredPosition = TextPosition;
            rectTransform.sizeDelta = CanvasSize;
            _statusText.fontSize = HeaderFontSize;
            _statusText.alignment = TextAlignmentOptions.Top;
            _statusText.enableWordWrapping = true;

#if false
            var canvasBackground = new GameObject("canvas background").AddComponent<Image>();
            rectTransform = canvasBackground.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.sizeDelta = CanvasSize;
            var tex = Texture2D.whiteTexture;
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, 100, 1);
            canvasBackground.sprite = sprite;
            canvasBackground.type = Image.Type.Filled;
            canvasBackground.fillMethod = Image.FillMethod.Horizontal;
            canvasBackground.color = new Color(0.5f, 0.5f, 0.5f, 0.0f);
#endif

            DontDestroyOnLoad(gameObject);
        }
    }
}