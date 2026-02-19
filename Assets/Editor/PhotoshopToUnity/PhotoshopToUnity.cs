/*
psd 파일을 프리팹으로 만들어주기 위해 만듦
자세한 사항은 전경문에게 물어보세용.
*/

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
    /// btn_ -> 버튼
    /// icon_ -> 아이콘(이미지)
    /// deco_ -> 데코 이미지(특정한 이미지 패턴을 반복 할 때 복사해서 사용)
    /// img_ -> UI 이미지(게이지, 자주 사용하는 이미지)
    /// bg_ -> BG,타이틀 이미지
    /// inner_ -> 팝업 이너
    /// item_ -> 아이템 이미지
    /// </summary>
    public enum LayerType
    {
        None,
        Button,
        IconImage,
        DecoImage,
        UIImage,
        BGImage,
        ItemImage,
        InnerImage,
        Txt,
        STxt,
    }

    /// <summary>
    /// 접두사 리스트
    /// </summary>
    public static List<string> PrefixList = new List<string>()
    {
        "IMG_",
        "BTN_",
        "ICON_",
        "DECO_",
        "UI_",
        "BG_",
        "ITEM_",
        "INNER_",
        "STXT_",
        "TXT_",
    };

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

    /// <summary>
    /// 이름에 replace 해줘야 할 녀석들
    /// txt_tjsxor_ehddlf_qmffhr_6ro_^NotoSansCJKjp-Bold^18^닉네임닉네임^#FFFFFF^null^null^null^null^null^null
    /// </summary>
    public static List<string> ReplaceCharList = new List<string>
        {
            ":",
            ";",
            ",",
            //".",
            "`",
            "+",
            //"-", // NotoSansCJKjp-Bold 로 사용 중
            "*",
            "<",
            ">",
            "[",
            "]",
            "(",
            ")",
            "$",
            "@",
            "!",
            "&",
            "%",
            "=",
            "|",
            "?",
            //"\\",
            "/",
            //"#", //#FFFFFF 로 사용 중
        };

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

        // 이름에 : 가 들어가 있는 경우 멋대로 _ 로 바꿔버리기 때문에 강제로 변경 기능 추가
        for (int i = 0; i < ReplaceCharList.Count; i++)
        {
            layerName = System.Text.RegularExpressions.Regex.Replace(layerName, "([" + PhotoshopToUnity.ReplaceCharList[i] + "])", "");
        }

        layerName = PsdImporter.SanitizeString(layerName, Path.GetInvalidFileNameChars());

        // 길이가 너무 길어서 수정
        if (layerName.Length > PsdImporter.MAX_LAYER_NAME_LEN)
        {
            layerName = layerName.Substring(0, PsdImporter.MAX_LAYER_NAME_LEN - 1);
        }

        //List<string> prefixList = new List<string>
        //{
        //    "img_",
        //    "txt_",
        //    "stxt_",
        //    "btn_",
        //    "_bt_",
        //};

        //for (int i = 0; i < prefixList.Count; i++)
        //{
        //    string layerPrefixName = prefixList[i];
        //    string gameobjectPrefixName = prefixList[i].ToUpper();

        //    if (layerName.IndexOf(layerPrefixName) != -1)
        //    {
        //        layerName = layerName.Replace(layerPrefixName, gameobjectPrefixName);
        //    }
        //}

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
                    string prefix = folders[i].ToLower();

                    LayerType prefixLayerType = GetLayerType(prefix, datas.Count);
                    if (prefixLayerType != LayerType.None)
                    {
                        break;
                    }
                    else
                    {
                        if (parentTransform.Find(folders[i]) == null)
                        {
                            GameObject obj = new GameObject(folders[i]);
                            obj.transform.SetParent(parentTransform);

                            RectTransform rtObj;
                            if (obj.GetComponent<RectTransform>() == null)
                            {
                                rtObj = obj.AddComponent<RectTransform>();
                            }
                            else
                            {
                                rtObj = obj.GetComponent<RectTransform>();
                            }

                            rtObj.localPosition = new Vector3(0, 0);
                            rtObj.sizeDelta = new Vector2(_settings.ReferanceResolution.x, _settings.ReferanceResolution.y);
                            rtObj.localScale = Vector3.one;
                        }

                        parentTransform = parentTransform.Find(folders[i]);
                    }
                }
            }
        }

        // 레이어를 게임오브젝트로 생성
        GameObject child = new GameObject(layerName);
        child.transform.SetParent(parentTransform);

        float x = (targetImportLayerData.originalX - halfWidth) + (targetSprite.rect.size.x * 0.5f);
        float y = (halfHeight - targetImportLayerData.originalY) - (targetSprite.rect.size.y * 0.5f);

        RectTransform rectTransform;
        if (child.GetComponent<RectTransform>() == null)
        {
            rectTransform = child.AddComponent<RectTransform>();
        }
        else
        {
            rectTransform = child.GetComponent<RectTransform>();
        }

        rectTransform.localPosition = new Vector3(x, y);
        rectTransform.sizeDelta = targetSprite.rect.size;
        rectTransform.localScale = Vector3.one;

        Thread.Sleep(10);

        UnityEngine.UI.Image targetImage;
        UnityEngine.UI.Button targetButton;
        RBText targetText;
        RBTextMeshProUGUI targetTextMeshProUGUI;

        LayerType layerType = GetLayerType(layerName, datas.Count);
        switch (layerType)
        {
            case LayerType.Txt:
                targetText = child.AddComponent<RBText>();

                if (datas.Count > 4)
                {
                    string fontName = datas.Dequeue();
                    int fontSize = Mathf.RoundToInt(float.Parse(datas.Dequeue()));
                    string fontText = datas.Dequeue();
                    string fontColor = datas.Dequeue();

                    string newLineStr = "<br>";
                    int newLineCount = fontText.Split(new[] { newLineStr }, StringSplitOptions.None).Length;

                    targetText.fontSize = fontSize;
                    targetText.resizeTextForBestFit = true;
                    targetText.resizeTextMinSize = 1;
                    targetText.resizeTextMaxSize = fontSize;
                    targetText.text = fontText.Replace(newLineStr, "\n");
                    targetText.alignment = TextAnchor.MiddleCenter;
                    ColorUtility.TryParseHtmlString(fontColor, out Color newCol);
                    targetText.color = newCol;

                    string targetFontName = "NotoSansCJK-Bold";
                    if (fontName.ToUpper().IndexOf("RIFFIC") != -1)
                    {
                        targetFontName = "RIFFICFREE-BOLD";
                    }

                    string[] files = UnityEditor.AssetDatabase.FindAssets($"t:font", new string[] { "Assets/Resources/Fonts/Editor" });
                    foreach (string guid in files)
                    {
                        string guidToPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                        Font asset = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>(guidToPath);
                        if (asset != null)
                        {
                            if (targetFontName == asset.name)
                            {
                                targetText.font = asset;
                            }
                        }
                    }

                    RectTransform tText = targetText.GetComponent<RectTransform>();
                    tText.sizeDelta = new Vector2(targetText.GetComponent<RectTransform>().sizeDelta.x * 1.1f, (fontSize * 1.5f) * newLineCount);

                    // Stroke 옵션이 있을 시 처리
                    {
                        // 여긴 int.Parse 나 파싱을 하면 안됨 string 으로 받아야 함 (null 일 경우가 있어서)
                        string isStroke = datas.Dequeue();
                        string fontStrokeColor = datas.Dequeue();
                        if (isStroke != null && isStroke != "")
                        {
                            // strokeSize, strokeColor
                            float fontStrokeSize = Mathf.RoundToInt(float.Parse(isStroke));
                            // 포토샵에서 사용된 값을 그대로 넣었더니 너무 이상해서 2로 들어오면 1.2f 로 수정.
                            fontStrokeSize = 1.0f + (fontStrokeSize * 0.1f);
                            Color strokeColor;
                            ColorUtility.TryParseHtmlString(fontStrokeColor, out strokeColor);

                            // 유니티에 적용
                            UnityEngine.UI.Outline targetStroke = child.AddComponent<UnityEngine.UI.Outline>();
                            targetStroke.effectColor = strokeColor;
                            targetStroke.effectDistance = new Vector2(fontStrokeSize, fontStrokeSize * -1);
                        }
                    }

                    // DropShadow 옵션이 있을 시 처리
                    {
                        // 여긴 int.Parse 나 파싱을 하면 안됨 string 으로 받아야 함 (null 일 경우가 있어서)
                        string isDropShadow = datas.Dequeue();
                        string fontDropShadowDistance = datas.Dequeue();
                        string fontDropShadowOpacity = datas.Dequeue();
                        string fontDropShadowColor = datas.Dequeue();
                        if (isDropShadow != null && isDropShadow != "")
                        {
                            // dropShadowLocalLightingAngle, dropShadowDistance, dropShadowOpacity, dropShadowColor
                            int fontDropShadowLocalLightingAngle = Mathf.RoundToInt(float.Parse(isDropShadow));
                            Color dropShadowColor;
                            ColorUtility.TryParseHtmlString(fontDropShadowColor, out dropShadowColor);
                            dropShadowColor.a = (int.Parse(fontDropShadowOpacity) / 100f);

                            // 유니티에 적용 (아래는 예제)
                            UnityEngine.UI.Shadow targetShadow = child.AddComponent<UnityEngine.UI.Shadow>();
                            targetShadow.effectColor = dropShadowColor;


                            //targetShadow.effectDistance = new Vector2(int.Parse(fontDropShadowDistance), int.Parse(fontDropShadowDistance) * -1);
                            ///디즈니팝에서는 x값은 무조건 0으로 세팅
                            targetShadow.effectDistance = new Vector2(0, int.Parse(fontDropShadowDistance) * -1);
                        }
                    }

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


            case LayerType.STxt:
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

            case LayerType.Button:
                targetImage = child.AddComponent<UnityEngine.UI.Image>();
                targetImage.sprite = targetSprite;

                rectTransform.sizeDelta = new Vector2(psdLayer.Width, psdLayer.Height);

                // 나인슬라이스인지 체크
                if (targetSprite.border != Vector4.zero)
                {
                    targetImage.type = UnityEngine.UI.Image.Type.Sliced;

                    x = (targetImportLayerData.originalX - halfWidth) + (rectTransform.rect.size.x * 0.5f);
                    y = (halfHeight - targetImportLayerData.originalY) - (rectTransform.rect.size.y * 0.5f);

                    rectTransform.localPosition = new Vector3(x, y);
                }
                else
                {
                    targetImage.type = UnityEngine.UI.Image.Type.Simple;
                }

                targetButton = child.AddComponent<UnityEngine.UI.Button>();
                targetButton.transition = UnityEngine.UI.Selectable.Transition.None;

                UnityEngine.Animator targetAnimator = child.AddComponent<Animator>();
                UnityEditor.Animations.AnimatorController targetController = Resources.Load<UnityEditor.Animations.AnimatorController>("Animations/ButtonAniController");
                targetAnimator.runtimeAnimatorController = targetController;
                //child.AddComponent<ButtonEventPlayer>();
                break;

            default:
                targetImage = child.AddComponent<UnityEngine.UI.Image>();
                targetImage.sprite = targetSprite;

                rectTransform.sizeDelta = new Vector2(psdLayer.Width, psdLayer.Height);

                // 나인슬라이스인지 체크
                //if (targetSprite.border != Vector4.zero)
                //{
                //    targetImage.type = UnityEngine.UI.Image.Type.Sliced;

                //    x = (targetImportLayerData.originalX - halfWidth) + (rectTransform.rect.size.x * 0.5f);
                //    y = (halfHeight - targetImportLayerData.originalY) - (rectTransform.rect.size.y * 0.5f);

                //    rectTransform.localPosition = new Vector3(x, y);
                //}
                //else
                {
                    targetImage.type = UnityEngine.UI.Image.Type.Simple;
                }

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
    /// btn_ -> 버튼
    /// icon_ -> 아이콘(이미지)
    /// deco_ -> 데코 이미지(특정한 이미지 패턴을 반복 할 때 복사해서 사용)
    /// img_ -> UI 이미지(게이지, 자주 사용하는 이미지)
    /// bg_ -> BG,타이틀 이미지
    /// inner_ -> 팝업 이너
    /// item_ -> 아이템 이미지
    /// </summary>
    /// <param name="layerName"></param>
    /// <param name="inQueueCount">1 인 경우 txt 가 아님 적어도 4 이상</param>
    /// <returns></returns>
    public static LayerType GetLayerType(string layerName, int inQueueCount = 1)
    {
        string name = layerName.ToUpper();

        if (name.Contains(PrefixList[(int)LayerType.STxt]) && inQueueCount > 4)
        {
            return LayerType.STxt;
        }
        else if (name.Contains(PrefixList[(int)LayerType.Txt]) && inQueueCount > 4)
        {
            return LayerType.Txt;
        }
        else if (name.Contains(PrefixList[(int)LayerType.Button]))
        {
            return LayerType.Button;
        }
        else if (name.Contains(PrefixList[(int)LayerType.IconImage]))
        {
            return LayerType.IconImage;
        }
        else if (name.Contains(PrefixList[(int)LayerType.DecoImage]))
        {
            return LayerType.DecoImage;
        }
        else if (name.Contains(PrefixList[(int)LayerType.UIImage]))
        {
            return LayerType.UIImage;
        }
        else if (name.Contains(PrefixList[(int)LayerType.BGImage]))
        {
            return LayerType.BGImage;
        }
        else if (name.Contains(PrefixList[(int)LayerType.InnerImage]))
        {
            return LayerType.InnerImage;
        }
        else if (name.Contains(PrefixList[(int)LayerType.ItemImage]))
        {
            return LayerType.ItemImage;
        }

        return LayerType.None;
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