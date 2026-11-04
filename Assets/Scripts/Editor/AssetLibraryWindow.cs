#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ZSP.EditorTools
{
    /// <summary>
    /// Quick view of the extracted asset library, with buttons to ping folders.
    /// </summary>
    public class AssetLibraryWindow : EditorWindow
    {
        private Vector2 scroll;
        private string root = "Assets/Extracted";

        [MenuItem("ZSP/Asset Library")]
        public static void Open()
        {
            GetWindow<AssetLibraryWindow>("ZSP Assets");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Extracted Asset Library", EditorStyles.boldLabel);
            if (GUILayout.Button("Ping Extracted Folder")) Ping(root);
            if (GUILayout.Button("Ping StreamingAssets Bundle")) Ping("Assets/StreamingAssets");

            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (Directory.Exists(root))
            {
                foreach (string dir in Directory.GetDirectories(root))
                {
                    string[] files = Directory.GetFiles(dir);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(Path.GetFileName(dir), GUILayout.Width(140));
                    EditorGUILayout.LabelField(files.Length + " files");
                    if (GUILayout.Button("Ping", GUILayout.Width(60))) Ping(dir);
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No Assets/Extracted folder found.", MessageType.Warning);
            }
            EditorGUILayout.EndScrollView();
        }

        private static void Ping(string path)
        {
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (obj != null) EditorGUIUtility.PingObject(obj);
            else Debug.Log("ZSP: " + path);
        }
    }
}
#endif
