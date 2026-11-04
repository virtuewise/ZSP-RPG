#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
            player.tag = "Player";
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
            player.AddComponent<PlayerHealth>();

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
            player.tag = "Player";
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

            // Legacy UGUI controls require an EventSystem to receive pointer/keyboard input.
            if (Object.FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var spawner = new GameObject("EnemySpawner");
            spawner.AddComponent<EnemySpawner>();

            var director = new GameObject("MissionDirector").AddComponent<MissionDirector>();
            director.hud = null; // wire to a HUDController when the real UI exists
            director.stealthSystem = stealth;
            director.enemySpawner = spawner.GetComponent<EnemySpawner>();
            director.autoStartFirstMission = false;
            director.playerHealth = health;
            director.identity = new OperativeIdentity();

            // Identity registrar (OIS) - first-launch callsign/display/nickname entry.
            var registrarPanel = new GameObject("IdentityRegistrarPanel", typeof(RectTransform), typeof(Image));
            registrarPanel.transform.SetParent(canvas.transform, false);
            var rp = registrarPanel.GetComponent<RectTransform>();
            rp.anchorMin = new Vector2(0.55f, 0.02f);
            rp.anchorMax = new Vector2(0.98f, 0.98f);
            rp.offsetMin = Vector2.zero;
            rp.offsetMax = Vector2.zero;
            var rpImage = registrarPanel.GetComponent<Image>();
            rpImage.color = new Color(0.06f, 0.09f, 0.08f, 0.85f);

            var registrar = registrarPanel.AddComponent<IdentityRegistrar>();
            registrar.director = director;
            registrar.previewText = CreateUIText(registrarPanel.transform, "Preview", new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.55f), 16, new Color(0.93f, 0.86f, 0.60f));
            registrar.errorText = CreateUIText(registrarPanel.transform, "Error", new Vector2(0.04f, 0.55f), new Vector2(0.96f, 0.62f), 14, new Color(0.95f, 0.35f, 0.30f));
            registrar.displayNameField = CreateInputField(registrarPanel.transform, "DisplayName", "Display name", new Vector2(0.04f, 0.64f), new Vector2(0.96f, 0.72f));
            registrar.callsignField = CreateInputField(registrarPanel.transform, "Callsign", "Callsign (SHADOW-07)", new Vector2(0.04f, 0.74f), new Vector2(0.96f, 0.82f));
            registrar.nicknameField = CreateInputField(registrarPanel.transform, "Nickname", "Nickname (optional)", new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.92f));
            registrar.confirmButton = CreateButton(registrarPanel.transform, "Confirm", new Vector2(0.04f, 0.93f), new Vector2(0.96f, 0.99f));
            // IdentityRegistrar.Start() owns the confirmation listener; do not register it twice.
            registrar.RefreshPreview();

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
        private static Text CreateUIText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = name;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
            return text;
        }

        private static InputField CreateInputField(Transform parent, string name, string placeholder, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = new Color(0.12f, 0.15f, 0.14f, 0.9f);
            var field = go.GetComponent<InputField>();
            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 0f);
            textRect.offsetMax = new Vector2(-8f, 0f);
            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            field.textComponent = text;
            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderGo.transform.SetParent(go.transform, false);
            var phRect = placeholderGo.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = new Vector2(8f, 0f);
            phRect.offsetMax = new Vector2(-8f, 0f);
            var ph = placeholderGo.GetComponent<Text>();
            ph.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ph.fontSize = 16;
            ph.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
            ph.text = placeholder;
            field.placeholder = ph;
            return field;
        }

        private static Button CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.color = new Color(0.78f, 0.62f, 0.28f, 0.9f);
            var button = go.GetComponent<Button>();
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(go.transform, false);
            var lr = label.GetComponent<RectTransform>();
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = Vector2.zero;
            lr.offsetMax = Vector2.zero;
            var text = label.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = name;
            text.fontSize = 18;
            text.color = new Color(0.12f, 0.09f, 0.04f);
            text.alignment = TextAnchor.MiddleCenter;
            return button;
        }
    }
}
#endif
