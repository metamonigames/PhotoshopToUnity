using UnityEngine.UI;
using UnityEngine;

public class RBText : Text, IText
{
    public string Text { get => text; set => text = value; }

    protected override void OnEnable()
    {
        base.OnEnable();

#if UNITY_EDITOR
        if (Application.isPlaying == false)
        {
            return;
        }
#endif
        ApplyFont();
    }

    private void ApplyFont()
    {
        ChangeFont(FontHelper.Instance.CurrentCode);
    }
    private void ChangeFont(ELocaleCode language)
    {
        if (FontHelper.LOCALE_FONT_URL.TryGetValue(language, out var fontAssetName))
        {
            if (font != null && font.name.IndexOf(fontAssetName) != -1)
            {
                return;
            }

            var targetFont = FontHelper.Instance.GetFont(FontHelper.Instance.CurrentCode);
            if (targetFont != null)
            {
                font = targetFont;
            }
        }
    }

    private void OnChangeTextColor()
    {
        //color = DevEditor.rbTextColor;
    }
}