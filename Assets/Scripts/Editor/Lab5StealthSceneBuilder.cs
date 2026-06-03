using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class Lab5StealthSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Lab5StealthScene.unity";
    private const string DefaultScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/Lab 5/Rebuild Stealth Scene")]
    public static void RebuildStealthScene()
    {
        EnsureTag("Cover");
        EnsureLayer("Obstacle", 8);
        EnsureLayer("Cover", 9);
        EnsureLayer("Player", 10);
        EnsureLayer("Enemy", 11);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Lab5StealthScene";

        var materials = CreateMaterials();
        CreateLighting();
        CreateLevel(materials);
        var player = CreatePlayer(materials);
        CreateEnemies(player.transform, materials);
        CreateUi();
        BakeNavigation();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.SaveScene(scene, DefaultScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(DefaultScenePath, true),
            new EditorBuildSettingsScene(ScenePath, true)
        };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Lab 5 stealth scene rebuilt: {ScenePath}");
    }

    private static Dictionary<string, Material> CreateMaterials()
    {
        const string materialFolder = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(materialFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        var materials = new Dictionary<string, Material>
        {
            ["Floor"] = CreateMaterial($"{materialFolder}/M_Floor.mat", new Color(0.24f, 0.27f, 0.26f)),
            ["Wall"] = CreateMaterial($"{materialFolder}/M_Wall.mat", new Color(0.38f, 0.42f, 0.46f)),
            ["Cover"] = CreateMaterial($"{materialFolder}/M_Cover.mat", new Color(0.48f, 0.32f, 0.18f)),
            ["Player"] = CreateMaterial($"{materialFolder}/M_Player.mat", new Color(0.22f, 0.52f, 0.92f)),
            ["Enemy"] = CreateMaterial($"{materialFolder}/M_Enemy.mat", new Color(0.82f, 0.18f, 0.16f)),
            ["Waypoint"] = CreateMaterial($"{materialFolder}/M_Waypoint.mat", new Color(0.08f, 0.9f, 0.62f)),
            ["VisionCone"] = CreateTransparentMaterial($"{materialFolder}/M_VisionCone.mat", new Color(1f, 0.84f, 0.08f, 0.25f))
        };

        return materials;
    }

    private static Material CreateMaterial(string path, Color color)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }

        SetMaterialColor(material, color);
        return material;
    }

    private static Material CreateTransparentMaterial(string path, Color color)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }

        SetMaterialColor(material, color);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return material;
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private static void CreateLighting()
    {
        var lightObject = new GameObject("Directional Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.4f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

        var cameraObject = new GameObject("Overview Camera");
        var camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        camera.transform.SetPositionAndRotation(new Vector3(0f, 14f, -18f), Quaternion.Euler(55f, 0f, 0f));
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 62f;
    }

    private static void CreateLevel(IReadOnlyDictionary<string, Material> materials)
    {
        CreateCube("Floor_NavMesh", new Vector3(0f, -0.05f, 0f), new Vector3(24f, 0.1f, 22f), materials["Floor"], 0, true);

        CreateWall("North Wall", new Vector3(0f, 1.15f, 11f), new Vector3(24f, 2.3f, 0.35f), materials);
        CreateWall("South Wall", new Vector3(0f, 1.15f, -11f), new Vector3(24f, 2.3f, 0.35f), materials);
        CreateWall("West Wall", new Vector3(-12f, 1.15f, 0f), new Vector3(0.35f, 2.3f, 22f), materials);
        CreateWall("East Wall", new Vector3(12f, 1.15f, 0f), new Vector3(0.35f, 2.3f, 22f), materials);

        CreateWall("Central Long Wall", new Vector3(0f, 1.15f, 2.8f), new Vector3(13.5f, 2.3f, 0.35f), materials);
        CreateWall("Storage Divider", new Vector3(-4.2f, 1.15f, -4.2f), new Vector3(0.35f, 2.3f, 8f), materials);
        CreateWall("Right Room Divider", new Vector3(5.5f, 1.15f, -1.5f), new Vector3(0.35f, 2.3f, 7f), materials);
        CreateWall("Short Sight Blocker", new Vector3(3.2f, 1.15f, 6.5f), new Vector3(0.35f, 2.3f, 5.2f), materials);

        CreateCover("Cover_Crate_A", new Vector3(-7.4f, 0.55f, -6.6f), new Vector3(1.6f, 1.1f, 1.6f), materials);
        CreateCover("Cover_Crate_B", new Vector3(-1.4f, 0.55f, -1.6f), new Vector3(1.9f, 1.1f, 1.2f), materials);
        CreateCover("Cover_Crate_C", new Vector3(7.6f, 0.55f, 4.1f), new Vector3(1.5f, 1.1f, 2.2f), materials);
        CreateCover("Cover_Low_Barrier", new Vector3(1.6f, 0.45f, -7.3f), new Vector3(4f, 0.9f, 1f), materials);
    }

    private static GameObject CreatePlayer(IReadOnlyDictionary<string, Material> materials)
    {
        var player = new GameObject("Player");
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Player");
        player.transform.position = new Vector3(-8.6f, 0.05f, -8.2f);

        var controller = player.AddComponent<CharacterController>();
        controller.height = 1.85f;
        controller.radius = 0.35f;
        controller.center = new Vector3(0f, 0.92f, 0f);

        player.AddComponent<PlayerStealth>();
        player.AddComponent<FirstPersonStealthController>();

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Player Body";
        body.transform.SetParent(player.transform);
        body.transform.localPosition = new Vector3(0f, 0.92f, 0f);
        body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
        Object.DestroyImmediate(body.GetComponent<Collider>());
        body.GetComponent<Renderer>().sharedMaterial = materials["Player"];

        var cameraObject = new GameObject("Player Camera");
        cameraObject.transform.SetParent(player.transform);
        cameraObject.transform.localPosition = new Vector3(0f, 1.62f, 0.08f);
        cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();

        return player;
    }

    private static void CreateEnemies(Transform player, IReadOnlyDictionary<string, Material> materials)
    {
        CreateEnemy("Enemy_A_MainHall", new Vector3(-2f, 0f, 7.2f), 180f, new[]
        {
            new Vector3(-8.5f, 0f, 7.4f),
            new Vector3(-2f, 0f, 7.4f),
            new Vector3(2.5f, 0f, 4.6f)
        }, player, materials);

        CreateEnemy("Enemy_B_Storage", new Vector3(-8.6f, 0f, -1.4f), 35f, new[]
        {
            new Vector3(-8.6f, 0f, -1.4f),
            new Vector3(-7.9f, 0f, -8.1f),
            new Vector3(-2.1f, 0f, -8.1f)
        }, player, materials);

        CreateEnemy("Enemy_C_RightRoom", new Vector3(8.7f, 0f, -5.2f), -70f, new[]
        {
            new Vector3(8.7f, 0f, -5.2f),
            new Vector3(8.4f, 0f, 1.6f),
            new Vector3(4.2f, 0f, 1.4f)
        }, player, materials);
    }

    private static void CreateEnemy(string name, Vector3 position, float yaw, Vector3[] waypointPositions, Transform player, IReadOnlyDictionary<string, Material> materials)
    {
        var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemy.name = name;
        enemy.layer = LayerMask.NameToLayer("Enemy");
        enemy.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        enemy.GetComponent<Renderer>().sharedMaterial = materials["Enemy"];

        var agent = enemy.AddComponent<NavMeshAgent>();
        agent.speed = 2.2f;
        agent.angularSpeed = 220f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 0.15f;

        var stateMachine = enemy.AddComponent<EnemyStateMachine>();
        var vision = enemy.AddComponent<EnemyVision>();
        vision.Configure(player, LayerMask.GetMask("Obstacle", "Cover"), 8.5f, 94f);

        var waypoints = new Transform[waypointPositions.Length];
        for (var i = 0; i < waypointPositions.Length; i++)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"{name}_Waypoint_{i + 1}";
            marker.transform.position = waypointPositions[i] + Vector3.up * 0.12f;
            marker.transform.localScale = Vector3.one * 0.24f;
            marker.GetComponent<Renderer>().sharedMaterial = materials["Waypoint"];
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            waypoints[i] = marker.transform;
        }

        stateMachine.Configure(waypoints, 11f);

        var cone = new GameObject("Vision Cone");
        cone.transform.SetParent(enemy.transform);
        cone.AddComponent<MeshFilter>();
        var coneRenderer = cone.AddComponent<MeshRenderer>();
        coneRenderer.sharedMaterial = materials["VisionCone"];
        cone.AddComponent<VisionConeRenderer>().Configure(vision);
    }

    private static void CreateUi()
    {
        var canvasObject = new GameObject("Stealth HUD");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("Stealth Indicator");
        panel.transform.SetParent(canvasObject.transform, false);
        var image = panel.AddComponent<Image>();
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -24f);
        rect.sizeDelta = new Vector2(180f, 44f);

        var textObject = new GameObject("State Label");
        textObject.transform.SetParent(panel.transform, false);
        var text = textObject.AddComponent<Text>();
        text.text = "SAFE";
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.fontSize = 18;
        text.fontStyle = FontStyle.Bold;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var indicator = panel.AddComponent<StealthIndicator>();
        var serializedIndicator = new SerializedObject(indicator);
        serializedIndicator.FindProperty("indicatorImage").objectReferenceValue = image;
        serializedIndicator.FindProperty("label").objectReferenceValue = text;
        serializedIndicator.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BakeNavigation()
    {
        var surfaceObject = new GameObject("NavMesh Surface");
        var surface = surfaceObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.layerMask = ~0;
        surface.BuildNavMesh();
    }

    private static GameObject CreateWall(string name, Vector3 position, Vector3 scale, IReadOnlyDictionary<string, Material> materials)
    {
        return CreateCube(name, position, scale, materials["Wall"], LayerMask.NameToLayer("Obstacle"), true);
    }

    private static GameObject CreateCover(string name, Vector3 position, Vector3 scale, IReadOnlyDictionary<string, Material> materials)
    {
        var cover = CreateCube(name, position, scale, materials["Cover"], LayerMask.NameToLayer("Cover"), true);
        cover.tag = "Cover";
        return cover;
    }

    private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material, int layer, bool navigationStatic)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.position = position;
        cube.transform.localScale = scale;
        cube.layer = layer;
        cube.GetComponent<Renderer>().sharedMaterial = material;

        return cube;
    }

    private static void EnsureTag(string tagName)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = tagManager.FindProperty("tags");

        for (var i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tagName)
            {
                return;
            }
        }

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureLayer(string layerName, int index)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        var layer = layers.GetArrayElementAtIndex(index);

        if (string.IsNullOrEmpty(layer.stringValue))
        {
            layer.stringValue = layerName;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
