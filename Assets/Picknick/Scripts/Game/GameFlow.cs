using System;
using Picknick.Net;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Picknick.Game
{
    /// <summary>
    /// Lives for the whole game (it survives scene loads): owns the online session and
    /// moves between the menu and the rooms. Every scene calls Ensure(), so pressing Play
    /// in any scene works.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        public const string MenuScene = "Menu";

        /// <summary>Room scenes in play order. "Next room" walks this list.</summary>
        public static readonly string[] Rooms = { "Room1_Kitchen", "Room2_Bathroom", "Room3_Bookshelf" };
        public static readonly string[] RoomTitles = { "The Kitchen Counter", "The Bathroom", "The Bookshelf" };

        public static GameFlow Instance { get; private set; }

        public NetSession Net { get; private set; }

        public static GameFlow Ensure()
        {
            if (Instance == null)
            {
                var go = new GameObject("Picknick Game");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<GameFlow>();
            }
            return Instance;
        }

        void Awake()
        {
            Net = gameObject.AddComponent<NetSession>();
            // The session closes itself; just go back to the menu, which shows why.
            Net.LostHost += () =>
            {
                if (SceneManager.GetActiveScene().name != MenuScene)
                    SceneManager.LoadScene(MenuScene);
            };
        }

        /// <summary>Index into Rooms of the open scene, or -1 (menu, sandbox).</summary>
        public static int CurrentRoom => Array.IndexOf(Rooms, SceneManager.GetActiveScene().name);

        public static bool HasNextRoom => CurrentRoom + 1 < Rooms.Length;

        /// <summary>Offline, or hosting. Online guests follow the host's room.</summary>
        public bool CanChangeRoom => Net.CanChangeRoom;

        public void LoadRoom(int index)
        {
            if (!CanChangeRoom || index < 0 || index >= Rooms.Length) return;
            Net.RoomLoading();
            SceneManager.LoadScene(Rooms[index]);
        }

        public void RestartRoom()
        {
            if (!CanChangeRoom) return;
            Net.RoomLoading();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void NextRoom()
        {
            if (HasNextRoom) LoadRoom(CurrentRoom + 1);
            else ToMenu();
        }

        /// <summary>Leaves any online session (hosting ends it for everyone) and opens the menu.</summary>
        public void ToMenu()
        {
            Net.Leave();
            if (SceneManager.GetActiveScene().name != MenuScene)
                SceneManager.LoadScene(MenuScene);
        }

        public void PlayOffline(int room)
        {
            Net.Leave();
            LoadRoom(room);
        }

        public void HostSteam(int room)
        {
            if (Net.HostSteam()) LoadRoom(room);
        }

        public void HostLan(int room)
        {
            if (Net.HostLan()) LoadRoom(room);
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
