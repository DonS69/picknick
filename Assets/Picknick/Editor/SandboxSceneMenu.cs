using System.Linq;
using Picknick.Rooms;
using Picknick.Sandbox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Picknick.EditorTools
{
    public static class SandboxSceneMenu
    {
        [MenuItem("Picknick/Create Sandbox Scene")]
        static void CreateSandboxScene() =>
            CreateScene<SandboxBootstrap>("Sandbox", "Assets/Picknick/Scenes/Sandbox.unity");

        [MenuItem("Picknick/Create Room 1 Scene (Kitchen)")]
        static void CreateKitchenScene() =>
            CreateScene<KitchenRoom>("Kitchen", "Assets/Picknick/Scenes/Room1_Kitchen.unity");

        static void CreateScene<T>(string objectName, string scenePath) where T : Component
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            Camera cam = Camera.main;
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.transform.position = new Vector3(2f, 2f, -10f);

            new GameObject(objectName).AddComponent<T>();

            EditorSceneManager.SaveScene(scene, scenePath);

            if (EditorBuildSettings.scenes.All(s => s.path != scenePath))
            {
                EditorBuildSettings.scenes = EditorBuildSettings.scenes
                    .Append(new EditorBuildSettingsScene(scenePath, true))
                    .ToArray();
            }

            Debug.Log($"Picknick: created {scenePath}. Press Play to try it.");
        }
    }
}
