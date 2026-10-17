using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Picknick.EditorTools
{
    /// <summary>
    /// Steam needs steam_appid.txt next to the game's .exe when it isn't launched by Steam
    /// itself (true for test builds sent to friends). Copy it there after every build.
    /// </summary>
    public static class SteamBuildStep
    {
        [PostProcessBuild]
        static void CopyAppId(BuildTarget target, string buildPath)
        {
            string source = Path.Combine(Directory.GetCurrentDirectory(), "steam_appid.txt");
            if (!File.Exists(source)) return;

            string folder = File.Exists(buildPath) ? Path.GetDirectoryName(buildPath) : buildPath;
            File.Copy(source, Path.Combine(folder, "steam_appid.txt"), true);
            Debug.Log($"Picknick: copied steam_appid.txt into {folder}");
        }
    }
}
