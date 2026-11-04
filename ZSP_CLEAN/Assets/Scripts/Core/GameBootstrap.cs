using System.Collections;
using UnityEngine;

namespace ZSP.Core
{
    /// <summary>
    /// Entry point for a fresh scene. Loads the asset bundle and optionally
    /// streams one of the original environment scenes (Mirage / Mountain).
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Bundle")]
        public bool autoLoadScene = false;
        [Tooltip("Optional substring match, e.g. 'level3' or 'mountain'. Takes priority over Scene Index.")]
        public string sceneNameContains = "";
        [Tooltip("0 = level0 (bootstrap), 3/4/5 = Mirage, 6/7 = Mountain. Use the console log to confirm.")]
        public int sceneIndex = 3;

        [Header("Player")]
        public GameObject playerPrefab;
        public Transform playerSpawn;

        private IEnumerator Start()
        {
            AssetBundleLoader loader = AssetBundleLoader.Instance;
            if (loader == null)
            {
                var go = new GameObject("AssetBundleLoader");
                loader = go.AddComponent<AssetBundleLoader>();
            }

            yield return loader.LoadBundle();

            if (!loader.IsLoaded)
            {
                Debug.LogError("[ZSP] No asset bundle. Game cannot start.");
                yield break;
            }

            if (autoLoadScene && loader.ScenePaths.Length > 0)
            {
                if (!string.IsNullOrEmpty(sceneNameContains))
                    yield return loader.LoadSceneByName(sceneNameContains);
                else
                    yield return loader.LoadSceneAsync(sceneIndex);
                yield break;
            }

            if (playerPrefab != null)
            {
                Vector3 pos = playerSpawn != null ? playerSpawn.position : Vector3.zero;
                Quaternion rot = playerSpawn != null ? playerSpawn.rotation : Quaternion.identity;
                Instantiate(playerPrefab, pos, rot);
            }
        }
    }
}
