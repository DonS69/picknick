using System.Linq;
using Picknick.Sandbox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Picknick.EditorTools
{
    public static class SandboxSceneMenu
    {
        const string ScenePath = "Assets/Picknick/Scenes/Sandbox.unity";

        [MenuItem("Picknick/Create Sandbox Scene")]
        static void CreateSandboxScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            Camera cam = Camera.main;
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.transform.position = new Vector3(2f, 2f, -10f);

            new GameObject("Sandbox").AddComponent<SandboxBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);

            if (EditorBuildSettings.scenes.All(s => s.path != ScenePath))
            {
                EditorBuildSettings.scenes = EditorBuildSettings.scenes
                    .Append(new EditorBuildSettingsScene(ScenePath, true))
                    .ToArray();
            }

            Debug.Log($"Picknick: created {ScenePath}. Press Play to try it.");
        }
    }
}
