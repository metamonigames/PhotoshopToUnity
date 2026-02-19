using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class FontHelper : SingletonMono<FontHelper>
{
    // private static LoaderObjectAssetBundleV3<Font> _loader;
    // private static LoaderObjectAssetBundleV3<TMP_FontAsset> _loaderTmpFont;
    // private static LoaderObjectAssetBundleV3<Material> _loaderTmpFontMaterial;

    /// <summary>
    /// 언어 폰트 에셋명
    /// </summary>
    public static readonly Dictionary<ELocaleCode, string> LOCALE_FONT_URL = new Dictionary<ELocaleCode, string>() {
        { ELocaleCode.en, "Fonts/En/" },
        { ELocaleCode.ko, "Fonts/Ko/" },
        { ELocaleCode.ja, "Fonts/Ja/" },
        { ELocaleCode.zh_trad, "Fonts/Tc/" },
        { ELocaleCode.zh_simp, "Fonts/Sc/" },
        { ELocaleCode.th, "Fonts/Th/" },
    };

    /// <summary>
    /// 언어 폰트 번들명
    /// </summary>
    public static readonly Dictionary<ELocaleCode, string> LOCALE_FONT_BUNDLE_NAME = new Dictionary<ELocaleCode, string>() {
        { ELocaleCode.en, "fonts_en" },
        { ELocaleCode.ko, "fonts_ko" },
        { ELocaleCode.ja, "fonts_ja" },
        { ELocaleCode.zh_trad, "fonts_tc" },
        { ELocaleCode.zh_simp, "fonts_sc" },
        { ELocaleCode.th, "fonts_th" },
    };

    /// <summary>
    /// 폰트 변경 이벤트
    /// </summary>
    public event Action<ELocaleCode> OnFontChanged;

    private Dictionary<ELocaleCode, TMP_FontAsset> _loadedFontList = new Dictionary<ELocaleCode, TMP_FontAsset>();
    private Dictionary<ELocaleCode, Font> _loadedOriginFontList = new Dictionary<ELocaleCode, Font>();
    private bool _isInitialize = false;
    private ELocaleCode _currentCode = ELocaleCode.en;

    /// <summary>
    ///
    /// </summary>
    public async UniTask InitializeAsync()
    {
        if (_isInitialize == true)
        {
            return;
        }

        //string bundleName = LOCALE_FONT_BUNDLE_NAME[CurrentCode];
        //if (AssetBundleLoader.Instance.HasAssetBundle(bundleName) == false
        //        && AssetBundleLoader.Instance.HasStreamingAssetBundle(bundleName) == false)
        //{
        //    assetbundle staticAssetbundle = null;
        //    foreach (var item in GSSheetDataManager.Instance.data.assetbundle)
        //    {
        //        if (item.Value.name == bundleName)
        //        {
        //            staticAssetbundle = item.Value;
        //            break;
        //        }
        //    }

        //    if (staticAssetbundle == null)
        //    {
        //        Logger.Error($"[FontHelper.Co_Initialize] bundleName: {bundleName} 가 없습니다.");
        //        bundleName = LOCALE_FONT_BUNDLE_NAME[ELocaleCode.ja];
        //    }
        //    else
        //    {
        //        // 해당 번들이 없다면 다운로드 받고 진행해야 함.
        //        yield return AssetBundleDownloadManager.Instance.Co_AssetbundleDownload(staticAssetbundle);
        //    }
        //}

        //yield return AssetBundleLoader.Instance.Co_LoadOrGetAssetBundleFile(bundleName, (inAssetBundle) =>
        //{
        //    _loader = new LoaderObjectAssetBundleV3<Font>();
        //    _loader.SetAssetBundle(inAssetBundle);

        //    _loaderTmpFont = new LoaderObjectAssetBundleV3<TMP_FontAsset>();
        //    _loaderTmpFont.SetAssetBundle(inAssetBundle);

        //    _loaderTmpFontMaterial = new LoaderObjectAssetBundleV3<Material>();
        //    _loaderTmpFontMaterial.SetAssetBundle(inAssetBundle);

        //    /// 기본적으로 현재 국가 폰트의 Fallback을 우선 적용
        //    var currentFontAsset = GetCurrentTMPFont();
        //    if (currentFontAsset == null)
        //    {
        //        return;
        //    }

        //    currentFontAsset.fallbackFontAssetTable.Clear();
        //});
    }

    public bool IsExistAssetBundle(ELocaleCode inCode)
    {
        //string bundleName = LOCALE_FONT_BUNDLE_NAME[inCode];
        //bool hasAssetBundle = AssetBundleLoader.Instance.HasAssetBundle(bundleName);
        //bool hasStreamingAssetBundle = AssetBundleLoader.Instance.HasStreamingAssetBundle(bundleName);

        //if(hasAssetBundle == true)
        //{
        //    Logger.Log($"[FontHelper.IsExistAssetBundle] inCode: {inCode}, hasAssetBundle: {hasAssetBundle}");
        //    return true;
        //}
        //else if (hasStreamingAssetBundle == true)
        //{
        //    Logger.Log($"[FontHelper.IsExistAssetBundle] inCode: {inCode}, hasStreamingAssetBundle: {hasAssetBundle}");
        //    return true;
        //}

        //Logger.Warning($"[FontHelper.IsExistAssetBundle] inCode: {inCode}, hasAssetBundle: {hasAssetBundle}, hasStreamingAssetBundle: {hasAssetBundle}");
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        if (_isInitialize == false)
            return;

        _isInitialize = false;

        // if(_loader != null)
        // {
        //     _loader.Release();
        //     _loader = null;
        // }

        // if(_loaderTmpFont != null)
        // {
        //     _loaderTmpFont.Release();
        //     _loaderTmpFont = null;
        // }

        foreach (var keyvalue in _loadedFontList)
        {
            keyvalue.Value.fallbackFontAssetTable.Clear();
        }

        _loadedFontList.Clear();
        _loadedOriginFontList.Clear();

        if (Instance != null && Instance.gameObject != null)
        {
            GameObject.Destroy(Instance.gameObject);
        }
    }

    /// <summary>
    /// TMP에 사용되는 FontAsset
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    public TMP_FontAsset GetTMPFont(ELocaleCode code)
    {
        string fontUrl = $"{GetFontUrl(code)}NotoSans-Bold SDF";
        string url = $"Assets/AssetBundles/{fontUrl}.asset";

#if UNITY_EDITOR
        if (Application.isPlaying == false)
            return (TMP_FontAsset)UnityEditor.AssetDatabase.LoadAssetAtPath(url, typeof(TMP_FontAsset));
#endif
        // if (_loaderTmpFont != null)
        // {
        //     if (_loadedFontList.TryGetValue(code, out var result) == false || result == null)
        //     {
        //         result = _loaderTmpFont.Load(url);
        //         if (result != null)
        //         {
        //             // 기존에 할당된 키가 있으면 
        //             if (_loadedFontList.ContainsKey(code))
        //             {
        //                 _loadedFontList[code] = result;
        //             }
        //             else
        //             {
        //                 _loadedFontList.Add(code, result);
        //             }
        //         }
        //     }
        //     return result;
        // }

        return null;
    }

    /// <summary>
    /// Font에 사용되는 Font 파일
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    public Font GetFont(ELocaleCode code)
    {
        // if (_loader != null)
        // {
        //     if (_loadedOriginFontList.TryGetValue(code, out var result) == false)
        //     {
        //         string fontUrl = $"{GetFontUrl(code)}NotoSans-Bold";

        //         result = _loader.Load($"Assets/AssetBundles/{fontUrl}.otf");

        //         // null 인 경우 ttf 일 수 있으니 한번 더 로드
        //         if (result == null)
        //         {
        //             result = _loader.Load($"Assets/AssetBundles/{fontUrl}.ttf");
        //         }

        //         if (result != null)
        //         {
        //             _loadedOriginFontList.Add(code, result);
        //         }
        //     }
        //     return result;
        // }

        return null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="inCode"></param>
    /// <returns></returns>
    public string GetFontUrl(ELocaleCode inCode)
    {
        if (LOCALE_FONT_URL.ContainsKey(inCode) == true)
        {
            return LOCALE_FONT_URL[inCode];
        }

        return LOCALE_FONT_URL[ELocaleCode.ja];
    }


    public TMP_FontAsset GetCurrentTMPFont()
    {
        return GetTMPFont(CurrentCode);
    }

    /// <summary>
    /// 
    /// var systemFont = FontHelper.Instance.GetSystemFont("Arial", _txtFont.fontSize);
    /// if (systemFont != null)
    /// _txtFont.font = systemFont;
    /// </summary>
    /// <param name="name"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public Font GetSystemFont(string name, int size)
    {
        if (name == null || name == "")
            return null;

        Font result = null;
        var fontNames = Font.GetOSInstalledFontNames();

        for (int i = 0; i < fontNames.Length; ++i)
        {
            if (fontNames[i] != null && fontNames[i].Equals(name))
            {
                result = Font.CreateDynamicFontFromOSFont(name, size);
                break;
            }
        }

        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="inName"></param>
    /// <returns></returns>
    public Material LoadMaterial(string inMaterialUrl)
    {
        string url = $"Assets/AssetBundles/{inMaterialUrl}.mat";
#if UNITY_EDITOR
        if (Application.isPlaying == false)
        {
            return (Material)UnityEditor.AssetDatabase.LoadAssetAtPath(url, typeof(Material));
        }
#endif
        // return _loaderTmpFontMaterial?.Load(url);

        return null;
    }

    /// <summary>
    /// 현재 폰트
    /// </summary>
    public ELocaleCode CurrentCode
    {
        get => _currentCode;
    }

    /// <summary>
    /// 폰트 변경
    /// </summary>
    /// <param name="inCode">변경할 언어 코드</param>
    public void SetCurrentFont(ELocaleCode inCode)
    {
        if (_currentCode == inCode)
        {
            return;
        }

        _currentCode = inCode;
        OnFontChanged?.Invoke(_currentCode);
    }

    /// <summary>
    /// 폰트 변경 이벤트 구독
    /// </summary>
    public void Subscribe(Action<ELocaleCode> callback)
    {
        if (callback == null) return;
        OnFontChanged += callback;
    }

    /// <summary>
    /// 폰트 변경 이벤트 구독 해제
    /// </summary>
    public void Unsubscribe(Action<ELocaleCode> callback)
    {
        if (callback == null) return;
        OnFontChanged -= callback;
    }
}