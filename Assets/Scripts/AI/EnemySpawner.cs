using System.Collections.Generic;
using UnityEngine;
using ZSP.Player;

namespace ZSP.AI
{
    /// <summary>
    /// Creates the required number of EnemyAI units at the assigned spawn points.
    /// Falls back to placeholder capsule enemies if no prefab is provided, so the
    /// vertical slice runs before the art pass exists.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Prefab")]
        public GameObject enemyPrefab;

        [Header("Spawn Points")]
        public Transform[] spawnPoints;

        [Header("Fallback")]
        public Material fallbackMaterial;

        private readonly List<GameObject> _spawnedEnemies = new();

        public int Spawn(int count)
        {
            // A new mission replaces the previous mission's live enemy set.
            foreach (var previous in _spawnedEnemies)
                if (previous != null) Destroy(previous);
            _spawnedEnemies.Clear();

            var spawned = 0;
            for (var i = 0; i < count; i++)
            {
                var point = spawnPoints != null && spawnPoints.Length > 0
                    ? spawnPoints[i % spawnPoints.Length]
                    : null;
                var position = point != null
                    ? point.position
                    : transform.position + new Vector3((i * 2.5f) - count, 0f, 6f + (i * 1.5f));

                var enemy = enemyPrefab != null ? Instantiate(enemyPrefab, position, Quaternion.identity) : CreateFallback(position);
                enemy.name = $"Meridian-{i + 1}";
                enemy.tag = "Enemy";
                if (enemy.GetComponentInChildren<EnemyHealth>() == null)
                    enemy.AddComponent<EnemyHealth>();
                _spawnedEnemies.Add(enemy);
                spawned++;
            }
            Debug.Log($"[ZSP] EnemySpawner created {spawned} enemies.");
            return spawned;
        }

        private GameObject CreateFallback(Vector3 position)
        {
            var capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.transform.position = position + Vector3.up * 0.9f;
            capsule.transform.localScale = new Vector3(0.8f, 0.95f, 0.8f);
            if (fallbackMaterial != null)
            {
                var renderer = capsule.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.sharedMaterial = fallbackMaterial;
            }
            else
            {
                var renderer = capsule.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    var material = new Material(Shader.Find("Standard")) { color = new Color(0.16f, 0.19f, 0.17f) };
                    renderer.sharedMaterial = material;
                }
            }
            capsule.AddComponent<EnemyAI>();
            return capsule;
        }
    }
}
