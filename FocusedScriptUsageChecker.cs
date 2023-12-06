using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Text;

public class FocusedScriptUsageChecker : EditorWindow
{
    private bool findUnusedScripts = true;
    private Vector2 scrollPosition;
    private string searchDirectory = "Assets/Scripts";
    private Dictionary<string, List<string>> scriptUsage = new Dictionary<string, List<string>>();
    private string resultsText; // Declare resultsText here

    [MenuItem("Tools/Focused Script Usage Checker")]
    public static void ShowWindow()
    {
        GetWindow<FocusedScriptUsageChecker>("Focused Script Usage Checker");
    }

    private void OnGUI()
    {
        GUILayout.Label("Focused Script Usage Checker", EditorStyles.boldLabel);
        findUnusedScripts = EditorGUILayout.Toggle("Find Unused Scripts", findUnusedScripts);

        EditorGUILayout.BeginHorizontal();
        searchDirectory = EditorGUILayout.TextField("Search Directory", searchDirectory);
        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Folder", searchDirectory, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                searchDirectory = FileUtil.GetProjectRelativePath(selectedPath);
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Check Scripts"))
    {
        CheckScripts();
        CopyResultsToClipboard();
    }

    if (GUILayout.Button("Copy Results to Clipboard"))
    {
        CopyResultsToClipboard();
    }

    if (scriptUsage.Count > 0)
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Create a GUIStyle for left-aligned text
        GUIStyle leftAlignedButtonStyle = new GUIStyle(EditorStyles.miniButton);
        leftAlignedButtonStyle.alignment = TextAnchor.MiddleLeft;

        foreach (var kvp in scriptUsage)
        {
            if (GUILayout.Button(kvp.Key, EditorStyles.linkLabel))
            {
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(kvp.Key));
            }

            // Adjusted display logic based on the findUnusedScripts toggle
            if (!findUnusedScripts)
            {
                foreach (var gameObjectPath in kvp.Value)
                {
                    if (GUILayout.Button(gameObjectPath, leftAlignedButtonStyle))
                    {
                        GameObject go = GameObject.Find(gameObjectPath);
                        if (go != null)
                        {
                            EditorGUIUtility.PingObject(go);
                        }
                    }
                }
            }
            else if (kvp.Value.Count == 0) // Display script as unused
            {
                GUILayout.Label("Unused", leftAlignedButtonStyle);
            }
        }
        EditorGUILayout.EndScrollView();
    }
    }

    private void CheckScripts()
{
    scriptUsage.Clear();
    string[] allScripts = AssetDatabase.FindAssets("t:MonoScript", new[] { searchDirectory })
                                       .Select(AssetDatabase.GUIDToAssetPath)
                                       .Where(path => path.StartsWith(searchDirectory))
                                       .ToArray();

    // Filter for MonoBehaviour scripts
    allScripts = allScripts.Where(scriptPath => IsMonoBehaviourScript(scriptPath)).ToArray();

    // Initialize scriptUsage with all scripts and empty lists
    foreach (var script in allScripts)
    {
        scriptUsage[script] = new List<string>();
    }

    // Check the active scene
    Scene activeScene = SceneManager.GetActiveScene();
    foreach (GameObject go in activeScene.GetRootGameObjects())
    {
        CheckGameObject(go, allScripts, "");
    }

    // Check prefabs
    string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab").Select(AssetDatabase.GUIDToAssetPath).ToArray();
    foreach (var prefabPath in prefabPaths)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        CheckGameObject(prefab, allScripts, "");
    }

    // If finding unused scripts, remove used scripts from scriptUsage
    if (findUnusedScripts)
    {
        var usedScripts = scriptUsage.Where(kvp => kvp.Value.Count > 0).Select(kvp => kvp.Key).ToList();
        foreach (var usedScript in usedScripts)
        {
            scriptUsage.Remove(usedScript);
        }
    }
}


private void CheckScenesAndPrefabs(string[] allScripts)
{
    // Check scenes
    string[] scenePaths = AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath).ToArray();
    foreach (var scenePath in scenePaths)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            CheckGameObject(go, allScripts, "");
        }
        EditorSceneManager.CloseScene(scene, false);
    }

    // Check prefabs
    string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab").Select(AssetDatabase.GUIDToAssetPath).ToArray();
    foreach (var prefabPath in prefabPaths)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        CheckGameObject(prefab, allScripts, "");
    }
}

private bool IsMonoBehaviourScript(string scriptPath)
{
    var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
    if (scriptAsset != null)
    {
        var scriptType = scriptAsset.GetClass();
        return scriptType != null && scriptType.IsSubclassOf(typeof(MonoBehaviour)) && !scriptType.IsSubclassOf(typeof(EditorWindow));
    }
    return false;
}

private void CheckGameObject(GameObject go, string[] allScripts, string path)
{
    string currentPath = string.IsNullOrEmpty(path) ? go.name : path + "/" + go.name;

    foreach (var monoBehaviour in go.GetComponents<MonoBehaviour>())
    {
        MonoScript monoScript = MonoScript.FromMonoBehaviour(monoBehaviour);
        string scriptPath = AssetDatabase.GetAssetPath(monoScript);
        if (!string.IsNullOrEmpty(scriptPath) && allScripts.Contains(scriptPath))
        {
            if (!scriptUsage.ContainsKey(scriptPath))
            {
                scriptUsage[scriptPath] = new List<string>();
            }
            if (!scriptUsage[scriptPath].Contains(currentPath))
            {
                scriptUsage[scriptPath].Add(currentPath);
            }
        }
    }

    foreach (Transform child in go.transform)
    {
        CheckGameObject(child.gameObject, allScripts, currentPath);
    }
}


    private void CopyResultsToClipboard()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var kvp in scriptUsage)
        {
            sb.AppendLine(kvp.Key);
            foreach (var gameObjectPath in kvp.Value)
            {
                sb.AppendLine(" - " + gameObjectPath);
            }
        }
        resultsText = sb.ToString();
        GUIUtility.systemCopyBuffer = resultsText;
        Debug.Log("Results copied to clipboard.");
    }
}
