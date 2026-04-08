using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

// 一次性初始化：输入系统、Addressables、基本 2D URP 资源、序列化设置与示例场景
namespace OneManJourney.Editor
{
    public static class ProjectInitializer
    {
        [MenuItem("Tools/OneManJourney/Run Project Init")]
        public static void Run()
        {
            // 基础项目设置
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";
            // 切换到新输入系统（兼容不同版本 API，反射处理）
            TrySetInputSystem();

            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Settings");

            SetupAddressables();
            var pipeline = SetupUrp2D();
            CreateDefaultScene();

            // 绑定渲染管线到 Graphics / Quality
            if (pipeline != null)
            {
                UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset = pipeline;
                for (int i = 0; i < QualitySettings.names.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                    QualitySettings.renderPipeline = pipeline;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Project initialization completed (Input System, Addressables, URP 2D, sample scene).");
        }

        private static void TrySetInputSystem()
        {
            var psType = typeof(PlayerSettings);
            // 尝试属性 activeInputHandler / activeInputHandling
            var prop = psType.GetProperty("activeInputHandler", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            prop ??= psType.GetProperty("activeInputHandling", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.CanWrite)
            {
                // 2 = 新输入系统
                prop.SetValue(null, 2, null);
                return;
            }

            // 尝试 SetPropertyInt 接口
            var setPropInt = psType.GetMethod(
                "SetPropertyInt",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(string), typeof(int), typeof(BuildTargetGroup) },
                modifiers: null);
            if (setPropInt != null)
            {
                // 参数签名: (string name, int value, BuildTargetGroup target)
                setPropInt.Invoke(null, new object[] { "activeInputHandler", 2, BuildTargetGroup.Standalone });
            }
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = "";
            for (int i = 0; i < parts.Length; i++)
            {
                current = i == 0 ? parts[i] : $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(current))
                {
                    var parent = Path.GetDirectoryName(current).Replace("\\", "/");
                    var leaf = Path.GetFileName(current);
                    AssetDatabase.CreateFolder(parent, leaf);
                }
            }
        }

        private static void SetupAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: true);
            AddressableAssetSettingsDefaultObject.Settings = settings;
            settings.BuildRemoteCatalog = false;
        }

        private static UniversalRenderPipelineAsset SetupUrp2D()
        {
            var rendererPath = "Assets/Settings/URP-2D-Renderer.asset";
            var pipelinePath = "Assets/Settings/URP-2D-Pipeline.asset";

            var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath) as Renderer2DData;
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(rendererData, rendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline == null)
            {
                pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }

            // 将 2D 渲染器挂到管线资产
            var so = new SerializedObject(pipeline);
            var rendererList = so.FindProperty("m_RendererDataList");
            if (rendererList != null)
            {
                if (rendererList.arraySize < 1)
                    rendererList.arraySize = 1;
                rendererList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;

                var defaultRenderer = so.FindProperty("m_DefaultRendererIndex");
                if (defaultRenderer != null)
                    defaultRenderer.intValue = 0;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            return pipeline;
        }

        private static void CreateDefaultScene()
        {
            var scenePath = "Assets/Scenes/SampleScene.unity";
            if (File.Exists(scenePath)) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 主相机
            var cameraGO = new GameObject("Main Camera");
            var cam = cameraGO.AddComponent<Camera>();
            cam.orthographic = true;
            cameraGO.tag = "MainCamera";
            cameraGO.transform.position = new Vector3(0, 0, -10f);
            cameraGO.AddComponent<UniversalAdditionalCameraData>();

            // 全局 2D 光
            var lightGO = new GameObject("Global Light 2D");
            var light2D = lightGO.AddComponent<Light2D>();
            light2D.lightType = Light2D.LightType.Global;
            light2D.intensity = 0.75f;

            EditorSceneManager.SaveScene(scene, scenePath);
        }
    }
}
