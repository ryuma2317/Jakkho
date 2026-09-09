using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public class CopyUserThumbnails
{
    static CopyUserThumbnails()
    {
        EditorApplication.delayCall += ProcessUserThumbnails;
    }

    [MenuItem("Tools/Import Uploaded User Thumbnails")]
    public static void ProcessUserThumbnails()
    {
        string targetDir = "Assets/Textures/Thumbnails";
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        string source1 = @"C:\Users\USER\.gemini\antigravity\brain\c61c057f-21e6-4160-9335-65b1ce5a9752\.user_uploaded\media_1787851591840.png";
        string source2 = @"C:\Users\USER\.gemini\antigravity\brain\c61c057f-21e6-4160-9335-65b1ce5a9752\free_wifi_custom_art_1787851786352.jpg";
        string source3 = @"C:\Users\USER\.gemini\antigravity\brain\c61c057f-21e6-4160-9335-65b1ce5a9752\.user_uploaded\media_1787851596794.png";

        string dest1 = "Assets/Textures/Thumbnails/Scenario_SuspiciousFile.png";
        string dest2 = "Assets/Textures/Thumbnails/Scenario_FreeWifi.jpg";
        string dest3 = "Assets/Textures/Thumbnails/Scenario_ScanOrSkip.png";

        bool copied = false;
        if (File.Exists(source1)) { File.Copy(source1, dest1, true); copied = true; }
        if (File.Exists(source2)) { File.Copy(source2, dest2, true); copied = true; }
        if (File.Exists(source3)) { File.Copy(source3, dest3, true); copied = true; }

        if (copied)
        {
            AssetDatabase.Refresh();

            ConfigureAsSprite(dest1);
            ConfigureAsSprite(dest2);
            ConfigureAsSprite(dest3);

            // Re-run ScenarioSelectBuilder to assign sprites
            ScenarioSelectBuilder.BuildScenarioSelectScreen();

            Debug.Log("<b>[CopyUserThumbnails]</b> Successfully assigned custom artwork to 2nd card (Free WiFi Trap)!");
        }
    }

    private static void ConfigureAsSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }
}
