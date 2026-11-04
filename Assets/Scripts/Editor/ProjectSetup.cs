#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ZSP.Core;
using ZSP.Player;
using ZSP.Narrative;
using ZSP.AI;

namespace ZSP.EditorTools
{
    /// <summary>
    /// One-click project scaffolding so the clean project is usable immediately
    /// without hand-building prefabs or scenes.
    /// </summary>
    public static class ProjectSetup
    {
        [MenuItem("ZSP/Create Bootstrap Scene")]
        public static void CreateBootstrapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var boot = new GameObject("GameBootstrap");
            boot.AddComponent<AssetBundleLoader>();
            var bootstrap = boot.AddComponent<GameBootstrap>();
            bootstrap.autoLoadScene = false;

            const string path = "Assets/Scenes/Bootstrap.unity";
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.Refresh();
            Debug.Log("[ZSP] Created " + path + ". Press Play to load the asset bundle.");
        }

        [MenuItem("ZSP/Create Player Rig")]
        public static void CreatePlayerRig()
        {
            var player = new GameObject("Player");
            player.transform.position = Vector3.zero;

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            var move = player.AddComponent<ThirdPersonController>();

            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.05f;
            var orbit = camGo.AddComponent<ThirdPersonCamera>();
            orbit.target = player.transform;
            move.cameraTransform = camGo.transform;

            var weapons = player.AddComponent<WeaponSystem>();
            weapons.aimCamera = cam;

            var progression = player.AddComponent<ProgressionSystem>();

            var stealth = player.AddComponent<StealthSystem>();
            stealth.playerRoot = player.transform;
            stealth.controller = move;
            stealth.progression = progression;

            var melee = player.AddComponent<MeleeSystem>();
            melee.aimCamera = cam;
            melee.controller = move;

            var interrogation = player.AddComponent<InterrogationSystem>();
            interrogation.aimCamera = cam;

            weapons.progression = progression;

            var audio = player.AddComponent<AudioSource>();
            audio.playOnAwake = false;

            System.IO.Directory.CreateDirectory("Assets/Prefabs");
            PrefabUtility.SaveAsPrefabAsset(player, "Assets/Prefabs/Player.prefab");
            AssetDatabase.Refresh();

            Selection.activeGameObject = player;
            Debug.Log("[ZSP] Player rig created and saved to Assets/Prefabs/Player.prefab. " +
                      "Add weapon stats and a muzzle transform in the inspector.");
        }

        [MenuItem("ZSP/Create Demo Mission Chain")]
        public static void CreateDemoMissionChain()
        {
            var go = new GameObject("MissionChainDemo");
            go.AddComponent<ZSP.Narrative.MissionChainDemo>();
            Selection.activeGameObject = go;
            Debug.Log("[ZSP] MissionChainDemo added. Press Play and check the Console.");
        }

        [MenuItem("ZSP/Create SIS Demo Scene")]
        public static void CreateSISDemoScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var demo = new GameObject("ShadowIntelligenceDemo");
            var component = demo.AddComponent<ShadowIntelligenceDemo>();
            component.runOnStart = true;

            const string path = "Assets/Scenes/SISDemo.unity";
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.Refresh();
            Selection.activeGameObject = demo;
            Debug.Log("[ZSP] SIS demo scene created at " + path + ". Press Play; the console will print the full three-mission run.");
        }

        [MenuItem("ZSP/Create Vertical Slice Scene")]
        public static void CreateVerticalSliceScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var boot = new GameObject("GameBootstrap");
            boot.AddComponent<AssetBundleLoader>();
            var bootstrap = boot.AddComponent<GameBootstrap>();
            bootstrap.autoLoadScene = false;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "BlockoutGround";
            ground.transform.localScale = new Vector3(12f, 1f, 12f);
            var mat = new Material(Shader.Find("Standard")) { color = new Color(0.32f, 0.30f, 0.26f) };
            ground.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var player = new GameObject("Player");
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            var move = player.AddComponent<ThirdPersonController>();
            var progression = player.AddComponent<ProgressionSystem>();
            var weapons = player.AddComponent<WeaponSystem>();
            weapons.progression = progression;
            var health = player.AddComponent<PlayerHealth>();
            var stealth = player.AddComponent<StealthSystem>();
            stealth.playerRoot = player.transform;
            stealth.controller = move;
            stealth.progression = progression;
            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.05f;
            var orbit = camGo.AddComponent<ThirdPersonCamera>();
            orbit.target = player.transform;
            move.cameraTransform = camGo.transform;
            weapons.aimCamera = cam;

            var canvas = new GameObject("Canvas");
            var uiCanvas = canvas.AddComponent<Canvas>();
            uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<CanvasScaler>();
            canvas.AddComponent<GraphicRaycaster>();

            var spawner = new GameObject("EnemySpawner");
            spawner.AddComponent<EnemySpawner>();

            var director = new GameObject("MissionDirector").AddComponent<MissionDirector>();
            director.hud = null; // wire to a HUDController when the real UI exists
            director.stealthSystem = stealth;
            director.enemySpawner = spawner.GetComponent<EnemySpawner>();
            director.autoStartFirstMission = false;

            var dossierText = new GameObject("DossierText", typeof(RectTransform), typeof(Text));
            dossierText.transform.SetParent(canvas.transform, false);
            var rect = dossierText.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.02f, 0.56f);
            rect.anchorMax = new Vector2(0.46f, 0.96f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var dt = dossierText.GetComponent<Text>();
            dt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dt.text = "dossier";
            dt.fontSize = 17;
            dt.color = new Color(0.93f, 0.86f, 0.60f, 0.95f);
            var dossier = new GameObject("DossierUI").AddComponent<ZSP.UI.DossierUI>();
            dossier.dossierText = dt;
            dossier.panelRoot = dossierText;
            dossier.identity = director.identity;

            const string path = "Assets/Scenes/VerticalSlice.unity";
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.Refresh();
            Selection.activeGameObject = player;
            Debug.Log("[ZSP] Vertical slice scene created at " + path + ". Add HUD, spawn points and content hooks to finish the slice.");
        }

        [MenuItem("ZSP/Apply Project Identity")]
        public static void ApplyProjectIdentity()
        {
            PlayerSettings.productName = "Zimbabwe: The Shadow Protocol";
            PlayerSettings.companyName = "ZSP";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.zsp.shadowprotocol");
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, "com.zsp.shadowprotocol");
            AssetDatabase.SaveAssets();
            Debug.Log("[ZSP] Product identity set to 'Zimbabwe: The Shadow Protocol'.");
        }
    }
}
#endif
