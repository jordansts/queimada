using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class MarpaStudioUrpImporter
{
    private const string PrefKey = "MarpaStudio.URP.PackageImported";

    static MarpaStudioUrpImporter()
    {
        EditorApplication.delayCall += TryImport;
    }

    private static void TryImport()
    {
        if (EditorPrefs.GetBool(PrefKey, false))
        {
            return;
        }

        string packagePath = Path.Combine(Application.dataPath, "MarpaStudio/URP/ArenaURP.unitypackage");
        if (!File.Exists(packagePath))
        {
            Debug.LogWarning($"MarpaStudio URP package not found at '{packagePath}'.");
            return;
        }

        EditorPrefs.SetBool(PrefKey, true);
        AssetDatabase.ImportPackage(packagePath, false);
        Debug.Log("MarpaStudio URP package import started.");
    }
}
