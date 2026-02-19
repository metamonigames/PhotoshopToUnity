using TMPro;
using TMPro.EditorUtilities;
using UnityEditor;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TextType = RBTextMeshProUGUI.TextType;

[UnityEditor.CustomEditor(typeof(RBTextMeshProUGUI))]
public class RBTextMeshProUGUIEditor : TMP_EditorPanelUI
{
    //static readonly GUIContent k_IsChangeLocaleLabel = new GUIContent("Change Locale", "나라에 따라서 변하는 폰트.");
    static readonly GUIContent k_TextTypeLabel = new GUIContent("Text Type", "BaliGames 에서 사용하는 Text Type");

    protected bool m_RBPropertiesChanged;
    protected SerializedProperty m_IsChangeLocaleProp;
    protected SerializedProperty m_TextTypeProp;
    protected SerializedProperty m_TextTypeStringProp;
    protected RBTextMeshProUGUI.TextType m_TextTypeEnum;

    protected bool m_NeedRepaint = false;

    protected override void OnEnable()
    {
        base.OnEnable();
        m_IsChangeLocaleProp = serializedObject.FindProperty("_isChangeLocale");
        m_TextTypeProp = serializedObject.FindProperty("_textType");
        m_TextTypeStringProp = serializedObject.FindProperty("_textTypeString");
        m_TextTypeEnum = (RBTextMeshProUGUI.TextType)m_TextTypeProp.enumValueIndex;

        TextType stringToEnum = TextType.NONE;
        if (System.Enum.TryParse<TextType>(m_TextTypeStringProp.stringValue, out stringToEnum) == true)
        {
            if ((int)stringToEnum == -1 || ((int)stringToEnum > 5))
            {
                m_TextTypeEnum = TextType.OutlineBlack;
            }
            else
            {
                m_TextTypeEnum = stringToEnum;
            }
        }
    }

    public override void OnInspectorGUI()
    {
        if (IsMixSelectionTypes()) return;

        if (m_NeedRepaint == true)
        {
            Repaint();
            m_NeedRepaint = false;
        }


        serializedObject.Update();
        DrawTextType();

        if (m_RBPropertiesChanged)
        {
            m_RBPropertiesChanged = false;
            if (m_TextTypeProp.enumNames.Length > 0)
            {
                string typeName = m_TextTypeProp.enumNames[m_TextTypeProp.enumValueIndex];
                int index = -1;
                for (int i = 0; i < m_MaterialPresets.Length; i++)
                {
                    Material m = m_MaterialPresets[i];
                    if (m.name.Contains(typeName))
                    {
                        index = i;
                        break;
                    }
                }

                if (index != -1)
                {
                    m_MaterialPresetSelectionIndex = index;
                    m_TargetMaterial = m_MaterialPresets[m_MaterialPresetSelectionIndex];
                    m_FontSharedMaterialProp.objectReferenceValue = m_TargetMaterial;
                    m_HavePropertiesChanged = true;

                    EditorUtility.SetDirty(target);
                    m_NeedRepaint = true;
                }
            }
        }
        serializedObject.ApplyModifiedProperties();

        base.OnInspectorGUI();
    }

    protected override void DrawExtraSettings()
    {
        base.DrawExtraSettings();
        DrawDontLocale();

    }
    protected void DrawTextType()
    {
        EditorGUI.BeginChangeCheck();

        m_TextTypeEnum = (RBTextMeshProUGUI.TextType)EditorGUILayout.EnumPopup("TextType : ", m_TextTypeEnum);
        if ((int)m_TextTypeEnum == -1 || ((int)m_TextTypeEnum > 5))
        {
            m_TextTypeProp.enumValueIndex = 1;
        }
        else
        {
            m_TextTypeProp.enumValueIndex = (int)m_TextTypeEnum;
        }

        m_TextTypeStringProp.stringValue = m_TextTypeEnum.ToString();

        if (EditorGUI.EndChangeCheck())
        {
            m_HavePropertiesChanged = true;
            m_RBPropertiesChanged = true;
        }
    }
    protected void DrawDontLocale()
    {
        EditorGUI.BeginChangeCheck();
        //EditorGUILayout.PropertyField(m_IsChangeLocaleProp, k_IsChangeLocaleLabel);
        if (EditorGUI.EndChangeCheck())
        {
            m_HavePropertiesChanged = true;
            m_RBPropertiesChanged = true;
        }
    }

    [MenuItem("GameObject/UI/Text - RBTextMeshPro", false, 2001)]
    private static void CreateRBTextMeshPro(MenuCommand menuCommand)
    {
        GameObject go = ObjectFactory.CreateGameObject("RBText (TMP)");
        RBTextMeshProUGUI textComponent = ObjectFactory.AddComponent<RBTextMeshProUGUI>(go);
        textComponent.fontSize = 36;
        textComponent.color = Color.white;
        textComponent.text = "New Text";

        PlaceUIElementRoot(go, menuCommand);
    }

    [MenuItem("GameObject/UI/Button - RBTextMeshPro", false, 2031)]
    private static void CreateButtonRBTextMeshPro(MenuCommand menuCommand)
    {
        GameObject go = new GameObject("Button");
        RectTransform rectTransform = go.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(160, 30);

        GameObject childText = new GameObject("RBText (TMP)");
        childText.AddComponent<RectTransform>();
        GameObjectUtility.SetParentAndAlign(childText, go);

        go.AddComponent<Image>();
        go.AddComponent<Button>();

        RBTextMeshProUGUI text = childText.AddComponent<RBTextMeshProUGUI>();
        text.text = "Button";
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;

        RectTransform textRectTransform = childText.GetComponent<RectTransform>();
        textRectTransform.anchorMin = Vector2.zero;
        textRectTransform.anchorMax = Vector2.one;
        textRectTransform.sizeDelta = Vector2.zero;

        // Override font size
        TMP_Text textComponent = go.GetComponentInChildren<TMP_Text>();
        textComponent.fontSize = 24;

        PlaceUIElementRoot(go, menuCommand);
    }

    private static void PlaceUIElementRoot(GameObject element, MenuCommand menuCommand)
    {
        GameObject parent = menuCommand.context as GameObject;
        if (parent == null)
        {
            parent = TMPro_CreateObjectMenu.GetOrCreateCanvasGameObject();

            // If in Prefab Mode, Canvas has to be part of Prefab contents,
            // otherwise use Prefab root instead.
            UnityEditor.SceneManagement.PrefabStage prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && !prefabStage.IsPartOfPrefabContents(parent))
                parent = prefabStage.prefabContentsRoot;
        }
        if (parent.GetComponentInParent<Canvas>() == null)
        {
            // Create canvas under context GameObject,
            // and make that be the parent which UI element is added under.
            GameObject canvas = TMPro_CreateObjectMenu.CreateNewUI();
            canvas.transform.SetParent(parent.transform, false);
            parent = canvas;
        }

        // Setting the element to be a child of an element already in the scene should
        // be sufficient to also move the element to that scene.
        // However, it seems the element needs to be already in its destination scene when the
        // RegisterCreatedObjectUndo is performed; otherwise the scene it was created in is dirtied.
        SceneManager.MoveGameObjectToScene(element, parent.scene);

        if (element.transform.parent == null)
        {
            Undo.SetTransformParent(element.transform, parent.transform, "Parent " + element.name);
        }

        GameObjectUtility.EnsureUniqueNameForSibling(element);

        // We have to fix up the undo name since the name of the object was only known after reparenting it.
        Undo.SetCurrentGroupName("Create " + element.name);

        GameObjectUtility.SetParentAndAlign(element, parent);

        Undo.RegisterCreatedObjectUndo(element, "Create " + element.name);

        Selection.activeGameObject = element;
    }
}
