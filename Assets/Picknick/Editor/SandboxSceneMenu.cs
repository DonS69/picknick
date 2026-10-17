using System.Linq;
using Picknick.Game;
using Picknick.Rooms;
using Picknick.Sandbox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Picknick.EditorTools
{
    public static class SandboxSceneMenu
    {
        const string Folder = "Assets/Picknick/Scenes/";

        /// <summary>
        /// (Re)creates the menu, every room and the sandbox, and lists them in Build Settings
        /// with the menu first, so a built game starts at the menu.
        /// </summary>
        [MenuItem("Picknick/Create All Scenes")]
        static void CreateAllScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            string[] paths =
            {
                CreateScene<MainMenu>(GameFlow.MenuScene, "Menu"),
                CreateScene<KitchenRoom>(GameFlow.Rooms[0], "Kitchen"),
                CreateScene<BathroomRoom>(GameFlow.Rooms[1], "Bathroom"),
                CreateScene<BookshelfRoom>(GameFlow.Rooms[2], "Living Room"),
                CreateScene<SandboxBootstrap>("Sandbox", "Sandbox"),
            };
            EditorBuildSettings.scenes = paths.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();

            EditorSceneManager.OpenScene(paths[0]);
            Debug.Log("Picknick: created the menu, 3 rooms and the sandbox. Press Play to start at the menu.");
        }

        static string CreateScene<T>(string sceneName, string objectName) where T : Component
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            Camera cam = Camera.main;
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.transform.position = new Vector3(2f, 2f, -10f);

            new GameObject(objectName).AddComponent<T>();

            string path = Folder + sceneName + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }
    }
}
