/*
psd 파일을 프리팹으로 만들어주기 위해 만듦
*/

using SubjectNerd.PsdImporter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class PhotoshopToUnity : EditorWindow
{
    /// <summary>
    /// 레이어 타입
    /// - Txt  : txt_, stxt_ → RBTextMeshProUGUI
    /// - Image: 그 외 모든 이미지 레이어
    /// </summary>
    public enum LayerType { Txt, Image }

    private const string TEXT_PREFIX = "TXT_";
    private const string STXT_PREFIX = "STXT_";

    // 레이어명에서 제거할 특수문자 (정규식)
    internal const string INVALID_LAYER_NAME_CHARS = @"[:\;,`+*<>\[\]\(\)$@!&%=|?/]";

    private const string MENU_ASSET_IMPORT = "Assets/Tools/Create (PhotoshopToUnity)";

    private static Object importFile;
    private static ImportUserData importSettings;
    private static List<int[]> importLayersList;

    private static Object[] _selectionArray;
    private static int _currentIndex;

    private static PhotoshopToUnitySettings _settings;

    // anchor 중앙 고정값
    private static readonly Vector2 AnchorCenter = new Vector2(0.5f, 0.5f);

#if UNITY_EDITOR
    [MenuItem(MENU_ASSET_IMPORT, false, 1)]
    private static void CreatePhotoshopToPopup()
    {
        if (_settings == null)
        {
            EditorUtility.DisplayDialog("에러", "PhotoshopToUnitySettings 이 null 입니다.", "확인");
            return;
        }

        if (_settings.IsError())
            return;

        if (_selectionArray != null && _selectionArray.Length > 0)
            CreatePhotoshopToPopupNextItem();
    }

    [MenuItem(MENU_ASSET_IMPORT, true, 1)]
    private static bool ValidatePhotoshopToPopup()
    {
        _settings = Resources.Load<PhotoshopToUnitySettings>("PhotoshopToUnitySettings");
        _currentIndex = 0;
        _selectionArray = Selection.objects.OrderBy(o => o.name).ToArray();

        if (_selectionArray.Length == 0)
            return false;

        foreach (Object file in _selectionArray)
        {
            string path = AssetDatabase.GetAssetPath(file);
            if (!path.ToLower().EndsWith(".psd"))
                return false;

            if (path.IndexOf(' ') != -1)
            {
                Debug.LogError("[PhotoshopToUnity] .psd 파일명에 공백이 있으면 안됩니다.");
                return false;
            }
        }

        return true;
    }
#endif

    private static void CreatePhotoshopToPopupNextItem()
    {
        if (_selectionArray.Length <= _currentIndex)
        {
            AssetDatabase.Refresh();
            return;
        }

        Object file = _selectionArray[_currentIndex++];
        string path = AssetDatabase.GetAssetPath(file);
        if (path.ToLower().EndsWith(".psd"))
            CreatePhotoshopToPopup(file, path);
    }

    private static void CreatePhotoshopToPopup(Object inFile, string inPath)
    {
        importFile = inFile;
        importSettings = new ImportUserData
        {
            TargetDirectory = string.Format(_settings.SavePath, inFile.name),
            fileNaming = NamingConvention.CreateGroupFolders
        };
        importLayersList = new List<int[]>();

        PsdImporter.BuildImportLayerData(inFile, importSettings, (layerData, displayData) =>
        {
            importSettings.DocRoot = ResolveData(importSettings.DocRoot, layerData);

            importSettings.DocRoot.Iterate(
                layerCallback: layer =>
                {
                    var display = GetDisplayData(displayData, layer.indexId);
                    if (display.isVisible && layer.import && layer.Childs.Count == 0)
                        importLayersList.Add(layer.indexId);
                },
                canEnterGroup: layer => layer.import
            );

            PsdImporter.ImportLayersUI(inFile, importSettings, importLayersList, CreatePopup, null);
        });
    }

    private static void CreatePopup(List<Sprite> inSprites,
        List<SubjectNerd.PsdImporter.PsdParser.PsdLayer> inPsdLayers,
        List<ImportLayerData> inImportLayerDatas)
    {
        GameObject presetGameObject = (GameObject)GameObject.Instantiate(_settings.Preset);
        presetGameObject.name = importFile.name;
        CreatePopupChild(OnCallback, inSprites.Count - 1, presetGameObject, inSprites, inPsdLayers, inImportLayerDatas);
    }

    private static void OnCallback(int inIndex, GameObject inPresetGameObject,
        List<Sprite> inSprites,
        List<SubjectNerd.PsdImporter.PsdParser.PsdLayer> inPsdLayers,
        List<ImportLayerData> inImportLayerDatas)
    {
        if (inIndex < 0)
        {
            string prefabFileName = string.Format("{0}/{1}.prefab", importSettings.TargetDirectory, importFile.name);
            PrefabUtility.SaveAsPrefabAssetAndConnect(inPresetGameObject, prefabFileName, InteractionMode.AutomatedAction);
            Selection.activeGameObject = inPresetGameObject;
            EditorCoroutineRunner.KillAllCoroutines();
            CreatePhotoshopToPopupNextItem();
        }
        else
        {
            CreatePopupChild(OnCallback, inIndex, inPresetGameObject, inSprites, inPsdLayers, inImportLayerDatas);
        }
    }

    private static void CreatePopupChild(
        Action<int, GameObject,
            List<Sprite>,
            List<SubjectNerd.PsdImporter.PsdParser.PsdLayer>,
            List<ImportLayerData>> inCallback,
        int inIndex,
        GameObject inParentGameObject,
        List<Sprite> inSprites,
        List<SubjectNerd.PsdImporter.PsdParser.PsdLayer> inPsdLayers,
        List<ImportLayerData> inImportLayerDatas)
    {
        Sprite targetSprite = inSprites[inIndex];
        SubjectNerd.PsdImporter.PsdParser.PsdLayer psdLayer = inPsdLayers[inIndex];
        ImportLayerData targetImportLayerData = inImportLayerDatas[inIndex];

        if (targetSprite == null)
        {
            inCallback?.Invoke(inIndex - 1, inParentGameObject, inSprites, inPsdLayers, inImportLayerDatas);
            return;
        }

        GameObject container = inParentGameObject.transform.Find(_settings.ParentGameObjectName).gameObject;

        float halfWidth  = _settings.ReferanceResolution.x * 0.5f;
        float halfHeight = _settings.ReferanceResolution.y * 0.5f;

        // 레이어명 + 텍스트 데이터 파싱
        string[] names = targetImportLayerData.name.Split('^');
        var datas = new Queue<string>(names);

        string layerName = datas.Dequeue();
        layerName = System.Text.RegularExpressions.Regex.Replace(layerName, INVALID_LAYER_NAME_CHARS, "");
        layerName = PsdImporter.SanitizeString(layerName, Path.GetInvalidFileNameChars());
        if (layerName.Length > PsdImporter.MAX_LAYER_NAME_LEN)
            layerName = layerName.Substring(0, PsdImporter.MAX_LAYER_NAME_LEN - 1);

        // 포토샵 폴더 → 빈 GameObject 계층 생성
        Transform parentTransform = container.transform;
        string[] folders = null;
        if (_settings.IsFolder && targetImportLayerData.path.IndexOf('/') != -1)
        {
            folders = targetImportLayerData.path.Split('/');
            for (int i = 0; i < folders.Length - 1; i++)
            {
                if (GetLayerType(folders[i], datas.Count) == LayerType.Txt)
                    break;

                if (parentTransform.Find(folders[i]) == null)
                {
                    var obj = new GameObject(folders[i]);
                    obj.AddComponent<RectTransform>();
                    obj.transform.SetParent(parentTransform, false);

                    var rt = obj.GetComponent<RectTransform>();
                    rt.anchorMin        = AnchorCenter;
                    rt.anchorMax        = AnchorCenter;
                    rt.pivot            = AnchorCenter;
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta        = new Vector2(_settings.ReferanceResolution.x, _settings.ReferanceResolution.y);
                    rt.localScale       = Vector3.one;
                }

                parentTransform = parentTransform.Find(folders[i]);
            }
        }

        // 레이어 GameObject 생성
        // SetParent 전에 RectTransform 추가 → Unity null 문제 방지
        var child = new GameObject(layerName);
        child.AddComponent<RectTransform>();
        child.transform.SetParent(parentTransform, false);

        // x, y는 캔버스 중앙 기준 오프셋이므로 anchor도 중앙(0.5)으로 맞춤
        float x = (targetImportLayerData.originalX - halfWidth)  + (targetSprite.rect.size.x * 0.5f);
        float y = (halfHeight - targetImportLayerData.originalY) - (targetSprite.rect.size.y * 0.5f);

        var rectTransform = child.GetComponent<RectTransform>();
        rectTransform.anchorMin        = AnchorCenter;
        rectTransform.anchorMax        = AnchorCenter;
        rectTransform.pivot            = AnchorCenter;
        rectTransform.anchoredPosition = new Vector2(x, y);
        rectTransform.sizeDelta        = targetSprite.rect.size;
        rectTransform.localScale       = Vector3.one;

        switch (GetLayerType(layerName, datas.Count))
        {
            case LayerType.Txt:
                ApplyTextLayer(child, rectTransform, datas, folders, layerName);
                break;

            default: // LayerType.Image
                var img = child.AddComponent<UnityEngine.UI.Image>();
                img.sprite        = targetSprite;
                img.type          = UnityEngine.UI.Image.Type.Simple;
                img.raycastTarget = false;
                rectTransform.sizeDelta = new Vector2(psdLayer.Width, psdLayer.Height);

                if (psdLayer.Opacity != 1.0f)
                {
                    Color c = img.color;
                    c.a = psdLayer.Opacity;
                    img.color = c;
                }
                break;
        }

        inCallback?.Invoke(inIndex - 1, inParentGameObject, inSprites, inPsdLayers, inImportLayerDatas);
    }

    private static void ApplyTextLayer(GameObject child, RectTransform rectTransform,
        Queue<string> datas, string[] folders, string layerName)
    {
        var tmp = child.AddComponent<RBTextMeshProUGUI>();
        tmp.raycastTarget = false;
        if (datas.Count <= 4) return;

        string fontName  = datas.Dequeue();
        int    fontSize  = Mathf.RoundToInt(float.Parse(datas.Dequeue()));
        string fontText  = datas.Dequeue();
        string fontColor = datas.Dequeue();

        const string newLineStr = "<br>";
        int newLineCount = fontText.Split(new[] { newLineStr }, StringSplitOptions.None).Length;

        tmp.fontSize        = fontSize;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin     = 1;
        tmp.fontSizeMax     = fontSize;
        tmp.text            = fontText.Replace(newLineStr, "\n");
        tmp.alignment       = TMPro.TextAlignmentOptions.Center;
        ColorUtility.TryParseHtmlString(fontColor, out Color col);
        tmp.color = col;

        // 폰트 로드
        string targetFontName = fontName.ToUpper().Contains("RIFFIC")
            ? "RIFFICFREE-BOLD SDF"
            : "NotoSans-Bold SDF";

        foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(TMP_FontAsset), new[] { "Assets/Resources/Fonts/Editor" }))
        {
            TMP_FontAsset asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null && asset.name.Contains(targetFontName))
            {
                tmp.font = asset;
                break;
            }
        }

        rectTransform.sizeDelta = new Vector2(
            rectTransform.sizeDelta.x * 1.1f,
            fontSize * 1.5f * newLineCount);

        // 텍스트 레이어는 PNG 불필요 → 삭제
        string addParentFolder = (folders != null && folders.Length > 1)
            ? PsdImporter.SanitizeString(folders[folders.Length - 2], Path.GetInvalidFileNameChars())
            : null;

        string pngPath = addParentFolder != null
            ? string.Format("{0}/{1}/{2}.png", importSettings.TargetDirectory, addParentFolder, layerName)
            : string.Format("{0}/{1}.png", importSettings.TargetDirectory, layerName);

        if (File.Exists(pngPath))
        {
            File.Delete(pngPath);
            File.Delete(pngPath + ".meta");
        }
    }

    private static ImportLayerData ResolveData(ImportLayerData storedData, ImportLayerData builtData)
    {
        if (storedData == null) return builtData;

        var storedIndex = new Dictionary<string, ImportLayerData>();
        storedData.Iterate(layer =>
        {
            storedIndex[layer.path] = layer;
        });

        builtData.Iterate(layer =>
        {
            if (storedIndex.TryGetValue(layer.path, out ImportLayerData existing))
            {
                layer.useDefaults  = existing.useDefaults;
                layer.Alignment    = existing.Alignment;
                layer.Pivot        = existing.Pivot;
                layer.ScaleFactor  = existing.ScaleFactor;
                layer.import       = existing.import;
            }
        });

        return builtData;
    }

    private static DisplayLayerData GetDisplayData(DisplayLayerData inImportDisplay, int[] layerIdx)
    {
        if (inImportDisplay == null || layerIdx == null) return null;

        DisplayLayerData current = inImportDisplay;
        foreach (int idx in layerIdx)
        {
            if (idx < 0 || idx >= current.Childs.Count) return null;
            current = current.Childs[idx];
        }
        return current;
    }

    /// <summary>
    /// 레이어 타입 판별: txt_ / stxt_ 이면 Txt, 나머지는 Image
    /// </summary>
    public static LayerType GetLayerType(string layerName, int inQueueCount = 1)
    {
        string name = layerName.ToUpper();
        if ((name.Contains(TEXT_PREFIX) || name.Contains(STXT_PREFIX)) && inQueueCount > 4)
            return LayerType.Txt;
        return LayerType.Image;
    }
}
