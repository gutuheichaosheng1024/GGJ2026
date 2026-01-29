using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Manager
{
    public class SceneTransitionManager : Singleton<SceneTransitionManager>
    {


        [Header("渐变效果")]
        [SerializeField] private Image fadeImage;
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("自动初始化")]
        [SerializeField] private bool autoCreateFadeUI = true;
        [SerializeField] private Color fadeColor = Color.black;


        private bool isTransitioning = false;
        private Canvas fadeCanvas;

        // 重写 Awake 来添加自定义初始化
        protected override void Awake()
        {
            base.Awake();
        }

        // 重写 InitializeSingleton 来添加自定义初始化
        protected override void InitializeSingleton()
        {
            base.InitializeSingleton();

            if (autoCreateFadeUI && fadeImage == null)
            {
                CreateFadeUI();
            }

            RegisterSceneEvents();
        }

        #region 场景事件注册

        /// <summary>
        /// 注册场景事件
        /// </summary>
        private void RegisterSceneEvents()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        /// <summary>
        /// 场景加载完成
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"场景加载完成: {scene.name}");
        }

        /// <summary>
        /// 场景卸载完成
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            Debug.Log($"场景卸载完成: {scene.name}");
        }

        /// <summary>
        /// 活动场景变化
        /// </summary>
        private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            Debug.Log($"活动场景变化: {oldScene.name} -> {newScene.name}");
        }

        #endregion

        #region 渐变UI创建

        /// <summary>
        /// 创建渐变UI
        /// </summary>
        private void CreateFadeUI()
        {
            // 创建Canvas
            GameObject canvasObj = new GameObject("FadeCanvas");
            canvasObj.transform.SetParent(transform);

            fadeCanvas = canvasObj.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 9999;

            // 创建Image
            GameObject imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(canvasObj.transform);

            fadeImage = imageObj.AddComponent<Image>();
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0);
            fadeImage.raycastTarget = false;

            // 设置RectTransform
            RectTransform rect = imageObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            // 初始隐藏
            fadeCanvas.gameObject.SetActive(false);
        }

        #endregion

        #region 场景跳转方法

        /// <summary>
        /// 加载场景（带渐变效果）
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (isTransitioning || !SceneExists(sceneName)) return;

            StartCoroutine(TransitionCoroutine(sceneName));
        }

        /// <summary>
        /// 重新加载当前场景
        /// </summary>
        public void ReloadCurrentScene()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            LoadScene(currentScene);
        }

        /// <summary>
        /// 加载下一个场景
        /// </summary>
        public void LoadNextScene()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            int nextIndex = (currentScene.buildIndex + 1) % SceneManager.sceneCountInBuildSettings;

            if (nextIndex < SceneManager.sceneCountInBuildSettings)
            {
                string nextScenePath = SceneUtility.GetScenePathByBuildIndex(nextIndex);
                string nextSceneName = System.IO.Path.GetFileNameWithoutExtension(nextScenePath);
                LoadScene(nextSceneName);
            }
        }

        #endregion

        #region 渐变效果协程

        /// <summary>
        /// 场景过渡协程
        /// </summary>
        private IEnumerator TransitionCoroutine(string sceneName)
        {
            isTransitioning = true;

            // 淡出
            yield return FadeOut();

            // 加载场景
            SceneManager.LoadScene(sceneName);

            // 淡入
            yield return FadeIn();

            isTransitioning = false;
        }



        /// <summary>
        /// 淡出效果（从透明到不透明）
        /// </summary>
        private IEnumerator FadeOut()
        {
            if (fadeCanvas != null)
            {
                fadeCanvas.gameObject.SetActive(true);

                float elapsed = 0f;
                Color startColor = fadeImage.color;
                Color endColor = fadeImage.color;

                startColor.a = 0;
                endColor.a = 1;

                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / fadeDuration;
                    fadeImage.color = Color.Lerp(startColor, endColor, t);
                    yield return null;
                }

                fadeImage.color = endColor;
            }
        }

        /// <summary>
        /// 淡入效果（从不透明到透明）
        /// </summary>
        private IEnumerator FadeIn()
        {
            if (fadeCanvas != null)
            {
                float elapsed = 0f;
                Color startColor = fadeImage.color;
                Color endColor = fadeImage.color;

                startColor.a = 1;
                endColor.a = 0;

                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / fadeDuration;
                    fadeImage.color = Color.Lerp(startColor, endColor, t);
                    yield return null;
                }

                fadeImage.color = endColor;
                fadeCanvas.gameObject.SetActive(false);
            }
        }

        #endregion

        #region 实用方法

        /// <summary>
        /// 检查场景是否存在
        /// </summary>
        private bool SceneExists(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string name = System.IO.Path.GetFileNameWithoutExtension(scenePath);

                if (name == sceneName)
                {
                    return true;
                }
            }

            return false;
        }


        #endregion

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // 注销场景事件
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }
    }

}