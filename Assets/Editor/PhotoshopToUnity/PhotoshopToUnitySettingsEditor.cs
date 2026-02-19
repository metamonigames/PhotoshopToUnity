using UnityEngine;
using UnityEditor;
using System.Collections;
#if !UNITY_WEBPLAYER
using System.IO;
#endif

[CustomEditor(typeof(PhotoshopToUnitySettings))]
public class PhotoshopToUnitySettingsEditor : Editor
{
    private PhotoshopToUnitySettings instance;

    public override void OnInspectorGUI()
    {
        instance = (PhotoshopToUnitySettings)target;
        base.OnInspectorGUI();
        if (EditorApplication.isCompiling)
        {
            GUILayout.TextArea("\n\n내용 반영 중 입니다.", GUILayout.Height(100));
            return;
        }

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("저장하기", GUILayout.Height(50)))
        {
            Save(instance);
        }

        GUILayout.EndHorizontal();
        GUILayout.Label("v.1.0.0");
    }

    [MenuItem("BaliGames/Framework/PhotoshopToUnity/CreateSettingsAsset")]
    public static void CreateSettingsAsset()
    {
        var value = PhotoshopToUnitySettings.Instance;
    }

    public static void Save(PhotoshopToUnitySettings inSpec)
    {
#if UNITY_EDITOR
        EditorUtility.SetDirty(inSpec);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
#endif
    }
}