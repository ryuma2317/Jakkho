using UnityEngine;
using UnityEditor;
using System.IO;
using Kannko.Scenario2;

namespace Kannko.Editor
{
    public static class ReorganizeToTargetStructure
    {
        [MenuItem("Kannko/Reorganize to Target Structure")]
        public static void Reorganize()
        {
            Debug.Log("[Reorganizer] Beginning project reorganization to target 3-Scenario structure...");

            // 1. Ensure target directory tree exists
            string[] directoriesToCreate = new string[]
            {
                "Assets/_Project",
                "Assets/_Project/Core",
                "Assets/_Project/Core/Scripts",
                "Assets/_Project/Core/Editor",
                "Assets/_Project/VR",
                "Assets/_Project/UI",
                "Assets/_Project/Audio",
                "Assets/_Project/Materials",
                "Assets/_Project/Templates",

                "Assets/Scenarios",
                "Assets/Scenarios/Scenario1",

                "Assets/Scenarios/Scenario2",
                "Assets/Scenarios/Scenario2/Scenes",
                "Assets/Scenarios/Scenario2/Scripts",
                "Assets/Scenarios/Scenario2/Scripts/Editor",
                "Assets/Scenarios/Scenario2/Prefabs",
                "Assets/Scenarios/Scenario2/Models",
                "Assets/Scenarios/Scenario2/Materials",
                "Assets/Scenarios/Scenario2/Textures",
                "Assets/Scenarios/Scenario2/Data",

                "Assets/Scenarios/Scenario3"
            };

            foreach (var dir in directoriesToCreate)
            {
                EnsureDirectoryExists(dir);
            }

            // 2. Relocate Scene
            if (File.Exists("Assets/Scenarios/Scenario2/Scenes/Scenario2.unity"))
            {
                MoveAssetSafely("Assets/Scenarios/Scenario2/Scenes/Scenario2.unity", "Assets/Scenarios/Scenario2/Scenes/Scenario2.unity");
            }
            if (Directory.Exists("Assets/Scenes"))
            {
                MoveDirectoryContents("Assets/Scenes", "Assets/Scenarios/Scenario2/Scenes");
            }

            // 3. Relocate Runtime Scripts
            MoveDirectoryContents("Assets/Scripts/Phone", "Assets/Scenarios/Scenario2/Scripts/Phone");
            MoveDirectoryContents("Assets/Scripts/Scenario", "Assets/Scenarios/Scenario2/Scripts/Scenario");
            MoveDirectoryContents("Assets/Scripts/DesktopTesting_TEMP", "Assets/Scenarios/Scenario2/Scripts/DesktopTesting_TEMP");

            MoveAssetSafely("Assets/Scripts/XRDesktopTestingController.cs", "Assets/_Project/Core/Scripts/XRDesktopTestingController.cs");
            MoveAssetSafely("Assets/Scripts/XRSimulatorPanelManager.cs", "Assets/_Project/Core/Scripts/XRSimulatorPanelManager.cs");
            MoveAssetSafely("Assets/Scripts/XRSimulatorTabToggle.cs", "Assets/_Project/Core/Scripts/XRSimulatorTabToggle.cs");
            MoveAssetSafely("Assets/Scripts/Kannko.Runtime.asmdef", "Assets/_Project/Core/Scripts/Kannko.Runtime.asmdef");

            if (Directory.Exists("Assets/Scripts"))
            {
                MoveDirectoryContents("Assets/Scripts", "Assets/Scenarios/Scenario2/Scripts");
            }

            // 4. Relocate Editor Scripts
            MoveAssetSafely("Assets/Editor/Kannko.Editor.asmdef", "Assets/_Project/Core/Editor/Kannko.Editor.asmdef");
            MoveDirectoryContents("Assets/Editor", "Assets/Scenarios/Scenario2/Scripts/Editor");

            // 5. Relocate Prefabs, Models, Materials, Textures, Audio
            MoveDirectoryContents("Assets/Prefabs", "Assets/Scenarios/Scenario2/Prefabs");
            MoveDirectoryContents("Assets/Models", "Assets/Scenarios/Scenario2/Models");
            MoveDirectoryContents("Assets/Materials", "Assets/Scenarios/Scenario2/Materials");
            MoveDirectoryContents("Assets/Textures", "Assets/Scenarios/Scenario2/Textures");
            if (Directory.Exists("Assets/Audio"))
            {
                MoveDirectoryContents("Assets/Audio", "Assets/Scenarios/Scenario2/Audio");
            }

            // 6. Clean up empty top-level directories that were moved
            string[] cleanupList = new string[]
            {
                "Assets/Scenes",
                "Assets/Scripts",
                "Assets/Editor",
                "Assets/Prefabs",
                "Assets/Models",
                "Assets/Materials",
                "Assets/Textures",
                "Assets/Audio",
                "Assets/Screenshots"
            };

            foreach (var oldFolder in cleanupList)
            {
                RemoveDirectoryIfEmpty(oldFolder);
            }

            // 7. Generate Data Assets in Assets/Scenarios/Scenario2/Data/
            GenerateScenario2DataAssets();

            AssetDatabase.Refresh();
            Debug.Log("[Reorganizer] Project reorganization complete!");
        }

        private static void EnsureDirectoryExists(string dirPath)
        {
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
                AssetDatabase.Refresh();
            }
        }

        private static void MoveAssetSafely(string sourcePath, string targetPath)
        {
            sourcePath = sourcePath.Replace('\\', '/');
            targetPath = targetPath.Replace('\\', '/');

            if (File.Exists(sourcePath) || Directory.Exists(sourcePath))
            {
                string targetDir = Path.GetDirectoryName(targetPath).Replace('\\', '/');
                EnsureDirectoryExists(targetDir);

                if (sourcePath != targetPath)
                {
                    string error = AssetDatabase.MoveAsset(sourcePath, targetPath);
                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogWarning($"[Reorganizer] Error moving {sourcePath} -> {targetPath}: {error}");
                    }
                    else
                    {
                        Debug.Log($"[Reorganizer] Moved: {sourcePath} -> {targetPath}");
                    }
                }
            }
        }

        private static void MoveDirectoryContents(string sourceFolder, string targetFolder)
        {
            sourceFolder = sourceFolder.Replace('\\', '/');
            targetFolder = targetFolder.Replace('\\', '/');

            if (!Directory.Exists(sourceFolder)) return;
            EnsureDirectoryExists(targetFolder);

            string[] files = Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                if (file.EndsWith(".meta")) continue;

                string normFile = file.Replace('\\', '/');
                string relativePath = normFile.Substring(sourceFolder.Length).TrimStart('/');
                string destPath = (targetFolder + "/" + relativePath).Replace('\\', '/');

                MoveAssetSafely(normFile, destPath);
            }
        }

        private static void RemoveDirectoryIfEmpty(string dirPath)
        {
            dirPath = dirPath.Replace('\\', '/');
            if (Directory.Exists(dirPath))
            {
                string[] files = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);
                if (files.Length == 0)
                {
                    AssetDatabase.DeleteAsset(dirPath);
                }
            }
        }

        private static void GenerateScenario2DataAssets()
        {
            string dataFolder = "Assets/Scenarios/Scenario2/Data";
            EnsureDirectoryExists(dataFolder);

            // Red Flags
            RedFlagData rf1 = CreateOrLoadRedFlag(dataFolder + "/RedFlag_01.asset", "RF-01", "Open Unencrypted Wi-Fi", "Wi-Fi network asks for no password and broadcasts an unencrypted open SSID.", true);
            RedFlagData rf2 = CreateOrLoadRedFlag(dataFolder + "/RedFlag_02.asset", "RF-02", "Duplicate Network Name", "Two identical Wi-Fi SSIDs broadcasted simultaneously with different signal strengths.", true);
            RedFlagData rf3 = CreateOrLoadRedFlag(dataFolder + "/RedFlag_03.asset", "RF-03", "Mismatched Captive Portal URL", "Captive portal URL domain does not match the coffee shop official web portal domain.", true);
            RedFlagData rf4 = CreateOrLoadRedFlag(dataFolder + "/RedFlag_04.asset", "RF-04", "Invalid SSL Certificate Warning", "Browser alerts user to invalid self-signed HTTPS certificate on login portal.", true);

            // Config asset
            string configPath = dataFolder + "/Scenario2_Config.asset";
            Scenario2Config config = AssetDatabase.LoadAssetAtPath<Scenario2Config>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<Scenario2Config>();
                config.scenarioName = "Scenario 2: Evil Twin Wi-Fi Attack";
                config.description = "Educational VR scenario simulating a malicious rogue access point (Evil Twin) attack in a coffee shop setting.";
                config.timeLimitSeconds = 300f;
                config.redFlags = new RedFlagData[] { rf1, rf2, rf3, rf4 };
                AssetDatabase.CreateAsset(config, configPath);
                Debug.Log($"[Reorganizer] Created Data Asset: {configPath}");
            }
        }

        private static RedFlagData CreateOrLoadRedFlag(string path, string id, string title, string description, bool isClue)
        {
            RedFlagData rf = AssetDatabase.LoadAssetAtPath<RedFlagData>(path);
            if (rf == null)
            {
                rf = ScriptableObject.CreateInstance<RedFlagData>();
                rf.flagId = id;
                rf.title = title;
                rf.description = description;
                rf.isEvilTwinClue = isClue;
                AssetDatabase.CreateAsset(rf, path);
                Debug.Log($"[Reorganizer] Created Data Asset: {path}");
            }
            return rf;
        }
    }
}
