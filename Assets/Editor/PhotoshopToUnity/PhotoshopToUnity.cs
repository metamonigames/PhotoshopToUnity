/*
psd 파일을 프리팹으로 만들어주기 위해 만듦
자세한 사항은 전경문에게 물어보세용.
*/

using Cysharp.Threading.Tasks;
using SubjectNerd.PsdImporter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class PhotoshopToUnity : EditorWindow
{
    /// <summary>
    /// 레이어 타입 (단순화)
    /// - Txt: txt_, stxt_ 텍스트 레이어 -> RBTextMeshProUGUI
    /// - Image: 나머지 모든 이미지 레이어
    /// </summary>
    public enum LayerType
    {
        Txt,
        Image,
    }

    private const string TEXT_PREFIX = "TXT_";
    private const string STXT_PREFIX = "STXT_";

    private const string MENU_ASSET_IMPORT = "Assets/Tools/Create (PhotoshopToUnity)";
    //private const string MENU_ASSET_COMMON_IMPORT = "Assets/Tools/Create (PhotoshopToUnity-Common)";

    private static Object importFile;
    private static ImportUserData importSettings;
    private static List<int[]> importLayersList;

    private static Object[] _selectionArray;
    private static int _currentIndex;

    private static PhotoshopToUnitySettings _settings;

    /// <summary>
    /// 공용 이미지를 사용한다면 path 가 null 이 아니어야 함
    /// </summary>
    private static string _commonImagePath = null;

    // 레이어명에서 제거할 특수문자 (정규식)
    internal const string INVALID_LAYER_NAME_CHARS = @"[:\;,`+*<>\[\]\(\)$@!&%=|?/]";

#if UNITY_EDITOR
    /// <summary>
    /// visible on 되어 있는 레이어들을 기준으로 Prefab 만듦.
    /// </summary>
    [MenuItem(MENU_ASSET_IMPORT, false, 1)]
    private static void CreatePhotoshopToPopup()
    {
        if (_settings == null)
        {
            EditorUtility.DisplayDialog("에러", "PhotoshopToUnitySettings 이 null 입니다.", "확인");
            return;
        }

        if (_settings.IsError() == true)
        {
            return;
        }

        _commonImagePath = null;

        if (_selectionArray != null && _selectionArray.Length > 0)
        {
            CreatePhotoshopToPopupNextItem();
        }
    }

    [MenuItem(MENU_ASSET_IMPORT, true, 1)]
    private static bool ValidatePhotoshopToPopup()
    {
        _settings = Resources.Load<PhotoshopToUnitySettings>("PhotoshopToUnitySettings");
        _currentIndex = 0;
        _selectionArray = Selection.objects.OrderBy(o => o.name).ToArray();

        if (_selectionArray.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < _selectionArray.Count(); i++)
        {
            Object file = _selectionArray[i];
            string path = AssetDatabase.GetAssetPath(file);
            if (path.ToLower().EndsWith(".psd") == false)
            {
                return false;
            }

            if (path.IndexOf(' ') != -1)
            {
                Debug.LogError("[PhotoshopToUnity] .psd 파일명에 공백이 있으면 안됩니다.");
                return false;
            }
        }

        return true;
    }

    ///// <summary>
    ///// visible on 되어 있는 레이어들을 기준으로 Prefab 만듦.
    ///// </summary>
    //[MenuItem(MENU_ASSET_COMMON_IMPORT, false, 1)]
    //private static void CreatePhotoshopToPopupCommon()
    //{
    //    if (_settings == null)
    //    {
    //        EditorUtility.DisplayDialog("에러", "PhotoshopToUnitySettings 이 null 입니다.", "확인");
    //        return;
    //    }

    //    if (_settings.IsError() == true)
    //    {
    //        return;
    //    }

    //    _commonImagePath = _settings.CommonPath;

    //    if (_selectionArray != null && _selectionArray.Length > 0)
    //    {
    //        CreatePhotoshopToPopupNextItem();
    //    }
    //}

    //[MenuItem(MENU_ASSET_COMMON_IMPORT, true, 1)]
    //private static bool ValidatePhotoshopToPopupCommon()
    //{
    //    _settings = Resources.Load<PhotoshopToUnitySettings>("PhotoshopToUnitySettings");
    //    _currentIndex = 0;
    //    _selectionArray = Selection.objects.OrderBy(o => o.name).ToArray();

    //    if (_selectionArray.Length == 0)
    //    {
    //        return false;
    //    }

    //    for (int i = 0; i < _selectionArray.Count(); i++)
    //    {
    //        Object file = _selectionArray[i];
    //        string path = AssetDatabase.GetAssetPath(file);
    //        if (path.ToLower().EndsWith(".psd") == false)
    //        {
    //            return false;
    //        }

    //        if (path.IndexOf(' ') != -1)
    //        {
    //            Logger.Error("[PhotoshopToUnity] .psd 파일명에 공백이 있으면 안됩니다.");
    //            return false;
    //        }
    //    }

    //    return true;
    //}
#endif

    /// <summary>
    /// 다음 생성할 팝업이 있을 시에 처리
    /// </summary>
    private static void CreatePhotoshopToPopupNextItem()
    {
        if (_selectionArray.Length <= _currentIndex)
        {
            AssetDatabase.Refresh();
            return;
        }

        Object file = _selectionArray[_currentIndex];
        _currentIndex++;
        string path = AssetDatabase.GetAssetPath(file);
        if (path.ToLower().EndsWith(".psd"))
        {
            CreatePhotoshopToPopup(file, path);
        }
    }

    /// <summary>
    /// 프리팹과 png 파일, cs 파일을 자동으로 생성해줌
    /// </summary>
    /// <param name="inFile"></param>
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
                    if (display.isVisible == true)
                    {
                        if (layer.import && layer.Childs.Count == 0)
                        {
                            importLayersList.Add(layer.indexId);
                        }
                    }
                },
                canEnterGroup: layer => layer.import
            );

            PsdImporter.ImportLayersUI(inFile, importSettings, importLayersList, CreatePopup, _commonImagePath);
        });
    }

    /// <summary>
    /// 팝업 생성
    /// </summary>
    /// <param name="inSprites"></param>
    private static void CreatePopup(List<Sprite> inSprites, List<SubjectNerd.PsdImporter.PsdParser.PsdLayer> inPsdLayers,
        List<ImportLayerData> inImportLayerDatas)
    {
        GameObject presetGameObject = GameObject.Instantiate(_settings.Preset) as GameObject;
        presetGameObject.name = importFile.name;

        CreatePopupChild(OnCallback, inSprites.Count - 1, presetGameObject, inSprites, inPsdLayers, inImportLayerDatas);
    }

    /// <summary>
    /// 더 생성할게 있는지 체크 후 처리 없으면 다음 팝업으로
    /// </summary>
    /// <param name="inIndex"></param>
    /// <param name="inPresetGameObject"></param>
    /// <param name="inSprites"></param>
    /// <param name="inImportLayerDatas"></param>
    private static void OnCallback(int inIndex, GameObject inPresetGameObject, List<Sprite> inSprites,
        List<SubjectNerd.PsdImporter.PsdParser.PsdLayer> inPsdLayers, List<ImportLayerData> inImportLayerDatas)
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

    /// <summary>
    /// GameObject 생성 및 컴포넌트 생성
    /// </summary>
    /// <param name="inCallback"></param>
    /// <param name="inIndex"></param>
    /// <param name="inParentGameObject"></param>
    /// <param name="inSprites"></param>
    /// <param name="inImportLayerDatas"></param>
    private static void CreatePopupChild(
        Action<int, GameObject, List<Sprite>, List<SubjectNerd.PsdImporter.PsdParser.PsdLayer>, List<ImportLayerData>> inCallback,
        int inIndex, GameObject inParentGameObject, List<Sprite> inSprites, List<SubjectNerd.PsdImporter.PsdParser.PsdLayer> inPsdLayers,
        List<ImportLayerData> inImportLayerDatas)
    {
        Sprite targetSprite = inSprites[inIndex];
        SubjectNerd.PsdImporter.PsdParser.PsdLayer psdLayer = inPsdLayers[inIndex];
        ImportLayerData targetImportLayerData = inImportLayerDatas[inIndex];

        if (targetSprite == null)
        {
            return;
        }

        GameObject container = inParentGameObject.transform.Find(_settings.ParentGameObjectName).gameObject;

        float halfWidth = _settings.ReferanceResolution.x * 0.5f;
        float halfHeight = _settings.ReferanceResolution.y * 0.5f;

        // 이름,폰트명,폰트 사이즈,폰트 내용,색, strokeSize, strokeColor, dropShadowAngle, dropShadowDistance, dropShadowOpacity, dropShadowColor
        string[] names = targetImportLayerData.name.Split('^');
        Queue<string> datas = new Queue<string>();
        for (int i = 0; i < names.Length; ++i)
        {
            datas.Enqueue(names[i]);
        }

        string layerName = datas.Dequeue();

        // 특수문자 제거
        layerName = System.Text.RegularExpressions.Regex.Replace(layerName, INVALID_LAYER_NAME_CHARS, "");
        layerName = PsdImporter.SanitizeString(layerName, Path.GetInvalidFileNameChars());

        // 길이가 너무 길어서 수정
        if (layerName.Length > PsdImporter.MAX_LAYER_NAME_LEN)
        {
            layerName = layerName.Substring(0, PsdImporter.MAX_LAYER_NAME_LEN - 1);
        }

        // 포토샵 폴더를 처리
        Transform parentTransform = container.transform;
        string[] folders = null;
        if (_settings.IsFolder == true)
        {
            if (targetImportLayerData.path.IndexOf('/') != -1)
            {
                folders = targetImportLayerData.path.Split('/');
                for (int i = 0; i < folders.Length - 1; i++)
                {
                    // 폴더명으로 레이어 타입 판별 (텍스트 폴더는 생성하지 않음)
                    if (GetLayerType(folders[i], datas.Count) == LayerType.Txt)
                        break;

                    if (parentTransform.Find(folders[i]) == null)
                    {
                        GameObject obj = new GameObject(folders[i]);
                        obj.AddComponent<RectTransform>();
                        obj.transform.SetParent(parentTransform, false);

                        var rtObj = obj.GetComponent<RectTransform>();
                        rtObj.anchorMin = Vector2.zero;
                        rtObj.anchorMax = Vector2.zero;
                        rtObj.pivot = new Vector2(0.5f, 0.5f);
                        rtObj.anchoredPosition = Vector2.zero;
                        rtObj.sizeDelta = new Vector2(_settings.ReferanceResolution.x, _settings.ReferanceResolution.y);
                        rtObj.localScale = Vector3.one;
                    }

                    parentTransform = parentTransform.Find(folders[i]);
                }
            }
        }

        // 레이어를 게임오브젝트로 생성
        // SetParent 전에 RectTransform을 먼저 추가해야 Canvas 계층 여부와 무관하게 안전하게 처리됨
        GameObject child = new GameObject(layerName);
        child.AddComponent<RectTransform>();
        child.transform.SetParent(parentTransform, false);

        float x = (targetImportLayerData.originalX - halfWidth) + (targetSprite.rect.size.x * 0.5f);
        float y = (halfHeight - targetImportLayerData.originalY) - (targetSprite.rect.size.y * 0.5f);

        var rectTransform = child.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(x, y);
        rectTransform.sizeDelta = targetSprite.rect.size;
        rectTransform.localScale = Vector3.one;

        RBTextMeshProUGUI targetTextMeshProUGUI;

        LayerType layerType = GetLayerType(layerName, datas.Count);
        switch (layerType)
        {
            case LayerType.Txt:
                targetTextMeshProUGUI = child.AddComponent<RBTextMeshProUGUI>();

                if (datas.Count > 4)
                {
                    string fontName = datas.Dequeue();
                    int fontSize = Mathf.RoundToInt(float.Parse(datas.Dequeue()));
                    string fontText = datas.Dequeue();
                    string fontColor = datas.Dequeue();

                    string newLineStr = "<br>";
                    int newLineCount = fontText.Split(new[] { newLineStr }, StringSplitOptions.None).Length;

                    targetTextMeshProUGUI.fontSize = fontSize;
                    targetTextMeshProUGUI.enableAutoSizing = true;
                    targetTextMeshProUGUI.fontSizeMin = 1;
                    targetTextMeshProUGUI.fontSizeMax = fontSize;
                    targetTextMeshProUGUI.text = fontText.Replace(newLineStr, "\n");
                    targetTextMeshProUGUI.alignment = TMPro.TextAlignmentOptions.Center;
                    ColorUtility.TryParseHtmlString(fontColor, out Color newCol);
                    targetTextMeshProUGUI.color = newCol;

                    string targetFontName = "NotoSans-Bold SDF";
                    if (fontName.ToUpper().IndexOf("RIFFIC") != -1)
                    {
                        targetFontName = "RIFFICFREE-BOLD SDF";
                    }

                    string[] files = UnityEditor.AssetDatabase.FindAssets($"t:" + typeof(TMP_FontAsset), new string[] { "Assets/Resources/Fonts/Editor" });
                    foreach (string guid in files)
                    {
                        string guidToPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        TMP_FontAsset asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(guidToPath);
                        if (asset != null)
                        {
                            if (asset.name.IndexOf(targetFontName) != -1)
                            {
                                targetTextMeshProUGUI.font = asset;
                            }
                        }
                    }

                    RectTransform tText = targetTextMeshProUGUI.GetComponent<RectTransform>();
                    tText.sizeDelta = new Vector2(targetTextMeshProUGUI.GetComponent<RectTransform>().sizeDelta.x * 1.1f, (fontSize * 1.5f) * newLineCount);

                    //// Stroke 옵션이 있을 시 처리
                    //{
                    //    // 여긴 int.Parse 나 파싱을 하면 안됨 string 으로 받아야 함 (null 일 경우가 있어서)
                    //    string isStroke = datas.Dequeue();
                    //    string fontStrokeColor = datas.Dequeue();
                    //    if (isStroke.IsNullOrEmpty() == false)
                    //    {
                    //        // strokeSize, strokeColor
                    //        float fontStrokeSize = Mathf.RoundToInt(float.Parse(isStroke));
                    //        // 포토샵에서 사용된 값을 그대로 넣었더니 너무 이상해서 2로 들어오면 1.2f 로 수정.
                    //        fontStrokeSize = 1.0f + (fontStrokeSize * 0.1f);
                    //        Color strokeColor;
                    //        ColorUtility.TryParseHtmlString(fontStrokeColor, out strokeColor);

                    //        // 유니티에 적용
                    //        UnityEngine.UI.Outline targetStroke = child.AddComponent<UnityEngine.UI.Outline>();
                    //        targetStroke.effectColor = strokeColor;
                    //        targetStroke.effectDistance = new Vector2(fontStrokeSize, fontStrokeSize * -1);
                    //    }
                    //}

                    //// DropShadow 옵션이 있을 시 처리
                    //{
                    //    // 여긴 int.Parse 나 파싱을 하면 안됨 string 으로 받아야 함 (null 일 경우가 있어서)
                    //    string isDropShadow = datas.Dequeue();
                    //    string fontDropShadowDistance = datas.Dequeue();
                    //    string fontDropShadowOpacity = datas.Dequeue();
                    //    string fontDropShadowColor = datas.Dequeue();
                    //    if (isDropShadow.IsNullOrEmpty() == false)
                    //    {
                    //        // dropShadowLocalLightingAngle, dropShadowDistance, dropShadowOpacity, dropShadowColor
                    //        int fontDropShadowLocalLightingAngle = Mathf.RoundToInt(float.Parse(isDropShadow));
                    //        Color dropShadowColor;
                    //        ColorUtility.TryParseHtmlString(fontDropShadowColor, out dropShadowColor);
                    //        dropShadowColor.a = (int.Parse(fontDropShadowOpacity) / 100f);

                    //        // 유니티에 적용 (아래는 예제)
                    //        UnityEngine.UI.Shadow targetShadow = child.AddComponent<UnityEngine.UI.Shadow>();
                    //        targetShadow.effectColor = dropShadowColor;


                    //        //targetShadow.effectDistance = new Vector2(int.Parse(fontDropShadowDistance), int.Parse(fontDropShadowDistance) * -1);
                    //        ///디즈니팝에서는 x값은 무조건 0으로 세팅
                    //        targetShadow.effectDistance = new Vector2(0, int.Parse(fontDropShadowDistance) * -1);
                    //    }
                    //}

                    // 상위 폴더를 알아야 Delete 할 수 있어서 추가
                    string addParentFolder = null;
                    if (folders != null && folders.Length > 1)
                    {
                        addParentFolder = folders[folders.Length - 2];
                    }

                    string path = null;
                    if (addParentFolder != null)
                    {
                        // 띄어쓰기 언더바로 치환
                        addParentFolder = PsdImporter.SanitizeString(addParentFolder, Path.GetInvalidFileNameChars());

                        path = string.Format("{0}/{1}/{2}.png", importSettings.TargetDirectory, addParentFolder, layerName);
                    }
                    else
                    {
                        path = string.Format("{0}/{1}.png", importSettings.TargetDirectory, layerName);
                    }

                    if (path != null && File.Exists(path))
                    {
                        File.Delete(path);
                        File.Delete(path + ".meta");
                    }
                }
                break;

            default: // LayerType.Image
                var targetImage = child.AddComponent<UnityEngine.UI.Image>();
                targetImage.sprite = targetSprite;
                targetImage.type = UnityEngine.UI.Image.Type.Simple;

                rectTransform.sizeDelta = new Vector2(psdLayer.Width, psdLayer.Height);

                // 투명도 적용
                if (psdLayer.Opacity != 1.0f)
                {
                    Color tempColor = targetImage.color;
                    tempColor.a = psdLayer.Opacity;
                    targetImage.color = tempColor;
                }
                break;
        }

        int nextIndex = inIndex - 1;
        inCallback?.Invoke(nextIndex, inParentGameObject, inSprites, inPsdLayers, inImportLayerDatas);
    }

    /// <summary>
    /// Resolve setting differences from stored data and built data
    /// </summary>
    /// <param name="storedData"></param>
    /// <param name="builtData"></param>
    /// <returns></returns>
    private static ImportLayerData ResolveData(ImportLayerData storedData, ImportLayerData builtData)
    {
        // Nothing was stored, used built data
        if (storedData == null)
            return builtData;

        // Flatten out stored to a dictionary, using the path as keys
        Dictionary<string, ImportLayerData> storedIndex = new Dictionary<string, ImportLayerData>();
        storedData.Iterate(
            layerCallback: layer =>
            {
                if (storedIndex.ContainsKey(layer.path) == false)
                    storedIndex.Add(layer.path, layer);
                else
                    storedIndex[layer.path] = layer;
            }
        );

        // Iterate through the built data now, checking for settings from storedIndex
        builtData.Iterate(
            layerCallback: layer =>
            {
                ImportLayerData existingSettings;
                if (storedIndex.TryGetValue(layer.path, out existingSettings))
                {
                    layer.useDefaults = existingSettings.useDefaults;
                    layer.Alignment = existingSettings.Alignment;
                    layer.Pivot = existingSettings.Pivot;
                    layer.ScaleFactor = existingSettings.ScaleFactor;
                    layer.import = existingSettings.import;
                }
            }
        );

        return builtData;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="inImportDisplay"></param>
    /// <param name="layerIdx"></param>
    /// <returns></returns>
    private static DisplayLayerData GetDisplayData(DisplayLayerData inImportDisplay, int[] layerIdx)
    {
        if (inImportDisplay == null || layerIdx == null)
            return null;

        DisplayLayerData currentLayer = inImportDisplay;
        foreach (int idx in layerIdx)
        {
            if (idx < 0 || idx >= currentLayer.Childs.Count)
                return null;
            currentLayer = currentLayer.Childs[idx];
        }
        return currentLayer;
    }

    /// <summary>
    /// 레이어 타입 판별
    /// - txt_, stxt_: 텍스트 레이어 (충분한 데이터 필요)
    /// - 나머지: 이미지 레이어
    /// </summary>
    public static LayerType GetLayerType(string layerName, int inQueueCount = 1)
    {
        string name = layerName.ToUpper();

        // 텍스트 레이어 판별 (txt_, stxt_로 시작하고 충분한 데이터 있음)
        if ((name.Contains(TEXT_PREFIX) || name.Contains(STXT_PREFIX)) && inQueueCount > 4)
        {
            return LayerType.Txt;
        }

        // 나머지는 모두 이미지
        return LayerType.Image;
    }

    const string configAssetName = "PhotoshopToUnity";

    private static PhotoshopToUnity instance;
    public static PhotoshopToUnity Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load(configAssetName) as PhotoshopToUnity;
                if (instance == null)
                {
                    // If not found, autocreate the asset object.
                    instance = CreateInstance<PhotoshopToUnity>();
                }
            }
            return instance;
        }
    }
}