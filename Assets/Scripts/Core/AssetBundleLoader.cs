using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace ZSP.Core
{
    /// <summary>
    /// Loads the original ZSP asset bundle (zsp_assets.unity3d) at runtime.
    /// This is the bridge that lets the fresh project reuse the environments,
    /// weapons, vehicles, characters and animations without any ad/Firebase code.
    /// </summary>
    public class AssetBundleLoader : MonoBehaviour
    {
        public static AssetBundleLoader Instance { get; private set; }

        [SerializeField] private string bundleFileName = "zsp_assets.unity3d";
        [SerializeField] private bool dontDestroyOnLoad = true;

        public AssetBundle Bundle { get; private set; }
        public bool IsLoaded => Bundle != null;
        public string[] ScenePaths { get; private set; } = new string[0];

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
        }

        public IEnumerator LoadBundle()
        {
            if (Bundle != null) yield break;

            string path = Path.Combine(Application.streamingAssetsPath, bundleFileName);

#if UNITY_ANDROID && !UNITY_EDITOR
            using (UnityWebRequest req = UnityWebRequestAssetBundle.GetAssetBundle(path))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[ZSP] AssetBundle load failed: {req.error}");
                    yield break;
                }
                Bundle = DownloadHandlerAssetBundle.GetContent(req);
            }
#else
            var request = AssetBundle.LoadFromFileAsync(path);
            yield return request;
            Bundle = request.assetBundle;
#endif

            if (Bundle == null)
            {
                Debug.LogError($"[ZSP] Could not load asset bundle at {path}");
                yield break;
            }

            ScenePaths = Bundle.GetAllScenePaths();
            Debug.Log($"[ZSP] Bundle loaded. Scenes: {ScenePaths.Length}");
            foreach (string s in ScenePaths) Debug.Log("  scene: " + s);
        }

        public IEnumerator LoadSceneAsync(int index, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (!IsLoaded || ScenePaths.Length == 0) yield break;
            index = Mathf.Clamp(index, 0, ScenePaths.Length - 1);
            yield return SceneManager.LoadSceneAsync(ScenePaths[index], mode);
        }

        public string FindScene(string nameContains)
        {
            if (!IsLoaded || string.IsNullOrEmpty(nameContains)) return null;
            string needle = nameContains.ToLowerInvariant();
            foreach (string path in ScenePaths)
                if (path.ToLowerInvariant().Contains(needle)) return path;
            return null;
        }

        public IEnumerator LoadSceneByName(string nameContains, LoadSceneMode mode = LoadSceneMode.Single)
        {
            string path = FindScene(nameContains);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"[ZSP] No bundle scene matches '{nameContains}'.");
                yield break;
            }
            yield return SceneManager.LoadSceneAsync(path, mode);
        }

        public T LoadAsset<T>(string assetName) where T : Object
        {
            if (!IsLoaded) return null;
            return Bundle.LoadAsset<T>(assetName);
        }

        public string[] GetAllAssetNames()
        {
            return IsLoaded ? Bundle.GetAllAssetNames() : new string[0];
        }

        private void OnDestroy()
        {
            if (Bundle != null)
            {
                Bundle.Unload(false);
                Bundle = null;
            }
        }
    }
}
