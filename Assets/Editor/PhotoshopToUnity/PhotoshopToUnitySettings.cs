using UnityEngine;
using System;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
[Serializable]
public class PhotoshopToUnitySettings : ScriptableObject
{
    [Header("기본 화면 크기")]
    [Tooltip("화면의 값을 적으면 됨.")]
    [SerializeField]
    private Vector2 _referanceResolution = new Vector2(1280, 720);

    [Header("팝업 프리셋")]
    [Tooltip("사용하기 위한 preset 셋팅")]
    [SerializeField]
    private GameObject _preset;

    [Header("생성되는 파일의 위치")]
    [Tooltip("생성된 파일의 상위 폴더 ({0} 는 파일명으로 자동치환)")]
    [SerializeField]
    private string _savePath = "Assets/AssetBundles/CommonPopup/{0}";

    [Header("부모의 이름")]
    [Tooltip("생성된 GameObject 의 부모의 이름 (이 하위에 들어감)")]
    [SerializeField]
    private string _parentGameObjectName = "Background";

    [Header("레이어 폴더를 GameObject 로 생성해서 넣어줄 것이라면 체크")]
    [Tooltip("생성될 GameObject 의 부모 그룹이 있을 경우 그 하위로 들어감")]
    [SerializeField]
    private bool _isFolder = true;

    [Header("공용 이미지 파일의 위치")]
    [SerializeField]
    private string _commonPath = "Assets/AssetBundles/CommonPopup/Common/PackSources";

    public Vector2 ReferanceResolution { get { return _referanceResolution; } }

    public GameObject Preset { get { return _preset; } }

    public string SavePath { get { return _savePath; } }

    public string ParentGameObjectName { get { return _parentGameObjectName; } }

    public bool IsFolder { get { return _isFolder; } }

    public string CommonPath { get { return _commonPath; } }

    /// <summary>
    /// 셋팅이 안된게 있는지 체크
    /// </summary>
    /// <returns></returns>
    public bool IsError()
    {
        bool isError = false;
        if(_preset == null)
        {
            EditorUtility.DisplayDialog("에러", "Preset 이 null 입니다.", "확인");
            isError = true;
        }

        if (_savePath == null || _savePath == "")
        {
            EditorUtility.DisplayDialog("에러", "SavePath 가 null 입니다.", "확인");
            isError = true;
        }

        if (_parentGameObjectName == null || _parentGameObjectName == "")
        {
            EditorUtility.DisplayDialog("에러", "ParentGameObjectName 이 null 입니다.", "확인");
            isError = true;
        }

        return isError;
    }

    const string configAssetName = "PhotoshopToUnitySettings";
    private static PhotoshopToUnitySettings instance;
    public static PhotoshopToUnitySettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load(configAssetName) as PhotoshopToUnitySettings;
                if (instance == null)
                {
                    // If not found, autocreate the asset object.
                    instance = CreateInstance<PhotoshopToUnitySettings>();
#if UNITY_EDITOR
                    string path = "Assets";
                    if (!Directory.Exists(path + "/Resources"))
                    {
                        AssetDatabase.CreateFolder(path, "Resources");
                    }

                    AssetDatabase.CreateAsset(instance, string.Format(path + "/Resources/{0}.asset", configAssetName));
#endif
                }
            }
            return instance;
        }
    }
}