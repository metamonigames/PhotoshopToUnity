using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class RBTextMeshProUGUI : TextMeshProUGUI, IText
{
    public enum TextType
    {
        NONE,
        OutlineBlack,
        TYPE03,
        TYPE04,
        TYPE05,
    }

    private static readonly string EDITOR_FONT_FULL_PATH = "Assets/Framework/Fonts/Resources/NotoSans-ExtraBold SDF.asset";

    [SerializeField] private bool _isChangeLocale = false;
    [SerializeField] private TextType _textType = TextType.NONE;
    [SerializeField] private string _textTypeString = TextType.NONE.ToString();

    private bool _isSubscribed = false;

    public bool IsChangeLocale { get => _isChangeLocale; set => _isChangeLocale = value; }

    public TextType Type
    {
        get => _textType;
        set
        {
            _textType = value;
            _textTypeString = _textType.ToString();

#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                return;
            }
#endif
            ApplyMaterial();
        }
    }

    public string Text { get => text; set => text = value; }


    protected override void Awake()
    {
        float prevFontSize = m_fontSize;
        float prevFontSizeMax = m_fontSizeMax;
        float prevFontSizeMin = m_fontSizeMin;
        float prevFontSizeBase = m_fontSizeBase;

        base.Awake();

        TextType stringToEnum = TextType.NONE;
        if (System.Enum.TryParse<TextType>(_textTypeString, out stringToEnum) == true)
        {
            _textType = stringToEnum;
        }
        else
        {
            _textTypeString = _textType.ToString();
        }

        if (prevFontSize != m_fontSize)
        {
            m_fontSize = prevFontSize;
        }
        if (prevFontSizeMax != m_fontSizeMax)
        {
            m_fontSizeMax = prevFontSizeMax;
        }
        if (prevFontSizeMin != m_fontSizeMin)
        {
            m_fontSizeMin = prevFontSizeMin;
        }
        if (prevFontSizeBase != m_fontSizeBase)
        {
            m_fontSizeBase = prevFontSizeBase;
        }

        if (Application.isPlaying == false)
        {
            return;
        }

        if (FontHelper.Instance == null)
        {
            return;
        }

        var targetFont = FontHelper.Instance.GetTMPFont(FontHelper.Instance.CurrentCode);
        if (targetFont != null)
        {
            font = targetFont;
            ApplyMaterial();
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

#if UNITY_EDITOR
        if (Application.isPlaying == false)
        {
            return;
        }
#endif
        SubscribeToFontChange();
        ApplyFont();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        UnsubscribeFromFontChange();
    }

    protected override void OnDestroy()
    {
        UnsubscribeFromFontChange();
        base.OnDestroy();
    }

    private void SubscribeToFontChange()
    {
        if (_isSubscribed) return;
        if (FontHelper.Instance == null) return;

        FontHelper.Instance.Subscribe(OnFontChanged);
        _isSubscribed = true;
    }

    private void UnsubscribeFromFontChange()
    {
        if (!_isSubscribed) return;
        if (FontHelper.Instance == null) return;

        FontHelper.Instance.Unsubscribe(OnFontChanged);
        _isSubscribed = false;
    }

    /// <summary>
    /// 폰트 변경 이벤트 콜백
    /// </summary>
    private void OnFontChanged(ELocaleCode newLocaleCode)
    {
        ChangeFont(newLocaleCode);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (Application.isPlaying == false)
        {
            if (font != null)
            {
                string prevFullPath = UnityEditor.AssetDatabase.GetAssetPath(font.instanceID);
                // Editor 용이 아닐 경우 강제로 변경해줌
                if (prevFullPath != EDITOR_FONT_FULL_PATH)
                {
                    var targetFont = (TMP_FontAsset)UnityEditor.AssetDatabase.LoadAssetAtPath(EDITOR_FONT_FULL_PATH, typeof(TMP_FontAsset));
                    if (targetFont != null)
                    {
                        font = targetFont;
                        ApplyFont();
                    }
                }
            }

            ApplyMaterial();
        }
    }
#endif

    private void ApplyFont()
    {
        if (IsChangeLocale == true)
        {
            ChangeFont(FontHelper.Instance.CurrentCode);
        }
        else
        {
#if UNITY_EDITOR
            var material = materialForRendering;
            if (material != null)
            {
                Shader newShader = Shader.Find(material.shader.name);
                material.shader = newShader;
            }
#endif
        }
    }

    /// <summary>
    /// 폰트 변경
    /// </summary>
    /// <param name="language"></param>
    private void ChangeFont(ELocaleCode language)
    {
        if (FontHelper.Instance == null) return;

        var targetFont = FontHelper.Instance.GetTMPFont(language);
        if (targetFont == null) return;

        // 같은 폰트면 변경하지 않음
        if (font != null && font == targetFont)
        {
            return;
        }

        font = targetFont;
        ApplyMaterial();
    }

    /// <summary>
    /// 
    /// </summary>
    private void ApplyMaterial()
    {
        if (font == null)
        {
            return;
        }

        if (Type == TextType.NONE)
        {
            fontSharedMaterial = font.material;
            return;
        }
#if UNITY_EDITOR
        if (Application.isPlaying == false)
        {
            string url = $"Assets/Framework/Fonts/Resources/FontTypes/{Type}.mat";
            fontSharedMaterial = (Material)UnityEditor.AssetDatabase.LoadAssetAtPath(url, typeof(Material));
            return;
        }
#endif
        fontSharedMaterial = FontHelper.Instance.LoadMaterial($"FontTypes/{Type}");
    }
}