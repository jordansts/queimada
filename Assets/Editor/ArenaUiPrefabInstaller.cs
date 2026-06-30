#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ArenaUiPrefabInstaller
{
    private const string UiFolderPath = "Assets/SourceFiles/Resources/UI";
    private const string HudPrefabPath = UiFolderPath + "/ArenaHudCanvas.prefab";
    private const string MenuPrefabPath = UiFolderPath + "/CameraSensitivityMenuCanvas.prefab";

    [MenuItem("Tools/Arena/Rebuild Arena UI Prefabs")]
    public static void Rebuild()
    {
        EnsureFolder("Assets/SourceFiles/Resources");
        EnsureFolder(UiFolderPath);

        BuildHudPrefab();
        BuildSensitivityMenuPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void BuildHudPrefab()
    {
        GameObject root = CreateCanvasRoot("ArenaHudCanvas", 900);
        ArenaHudView view = root.AddComponent<ArenaHudView>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Text controlsLabel = CreateText("ControlsLabel", root.transform, font, 20, TextAnchor.MiddleLeft, new Vector2(900f, 36f), "Controls");
        ConfigureAnchoredRect(controlsLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -24f));

        Text ballStatusLabel = CreateText("BallStatusLabel", root.transform, font, 20, TextAnchor.MiddleRight, new Vector2(320f, 36f), "Ball");
        ConfigureAnchoredRect(ballStatusLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -24f));

        CreateHealthBar("PlayerHealth", root.transform, font, new Vector2(20f, -86f), true, out Image playerFill, out Text playerLabel);
        CreateHealthBar("BotHealth", root.transform, font, new Vector2(-20f, -86f), false, out Image botFill, out Text botLabel);

        GameObject crosshair = new GameObject("Crosshair", typeof(RectTransform), typeof(Image));
        crosshair.transform.SetParent(root.transform, false);
        RectTransform crosshairRect = crosshair.GetComponent<RectTransform>();
        ConfigureAnchoredRect(crosshairRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        crosshairRect.sizeDelta = new Vector2(22f, 22f);
        Image crosshairImage = crosshair.GetComponent<Image>();
        crosshairImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        crosshairImage.color = Color.white;

        AssignSerializedField(view, "controlsLabel", controlsLabel);
        AssignSerializedField(view, "ballStatusLabel", ballStatusLabel);
        AssignSerializedField(view, "playerHealthLabel", playerLabel);
        AssignSerializedField(view, "botHealthLabel", botLabel);
        AssignSerializedField(view, "playerHealthFill", playerFill);
        AssignSerializedField(view, "botHealthFill", botFill);
        AssignSerializedField(view, "crosshairImage", crosshairImage);

        SavePrefab(root, HudPrefabPath);
    }

    private static void BuildSensitivityMenuPrefab()
    {
        GameObject root = CreateCanvasRoot("CameraSensitivityMenuCanvas", 1000);
        CameraSensitivityMenuView view = root.AddComponent<CameraSensitivityMenuView>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        ConfigureAnchoredRect(panelRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        panelRect.sizeDelta = new Vector2(420f, 220f);
        panel.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.94f);

        CreateText("Title", panel.transform, font, 28, TextAnchor.MiddleCenter, new Vector2(360f, 36f), "Sensibilidade da Camera", new Vector2(0f, 72f));
        Text valueLabel = CreateText("ValueLabel", panel.transform, font, 18, TextAnchor.MiddleCenter, new Vector2(360f, 36f), "Sensibilidade: 0.0", new Vector2(0f, 18f));
        CreateText("CloseHint", panel.transform, font, 18, TextAnchor.MiddleCenter, new Vector2(360f, 36f), "ESC para fechar", new Vector2(0f, -92f));

        Slider slider = CreateSlider(panel.transform, new Vector2(0f, -22f));
        Toggle toggle = CreateToggle(panel.transform, font, "Inverter eixo Y", new Vector2(0f, -56f));

        AssignSerializedField(view, "sensitivitySlider", slider);
        AssignSerializedField(view, "valueLabel", valueLabel);
        AssignSerializedField(view, "invertYToggle", toggle);

        SavePrefab(root, MenuPrefabPath);
    }

    private static GameObject CreateCanvasRoot(string name, int sortingOrder)
    {
        GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return root;
    }

    private static void CreateHealthBar(string name, Transform parent, Font font, Vector2 anchoredPosition, bool leftAligned, out Image fillImage, out Text label)
    {
        GameObject container = new GameObject(name, typeof(RectTransform), typeof(Image));
        container.transform.SetParent(parent, false);
        RectTransform rect = container.GetComponent<RectTransform>();
        Vector2 anchor = leftAligned ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
        ConfigureAnchoredRect(rect, anchor, anchor, leftAligned ? new Vector2(0f, 1f) : new Vector2(1f, 1f), anchoredPosition);
        rect.sizeDelta = new Vector2(320f, 30f);
        container.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.18f, 0.92f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(container.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        Stretch(fillRect, new Vector2(3f, 3f), new Vector2(-3f, -3f));
        fillImage = fill.GetComponent<Image>();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 1f;

        label = CreateText("Label", container.transform, font, 16, TextAnchor.MiddleCenter, rect.sizeDelta, name);
    }

    private static Text CreateText(string name, Transform parent, Font font, int fontSize, TextAnchor alignment, Vector2 size, string text, Vector2? anchoredPosition = null)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        Text label = obj.GetComponent<Text>();
        label.font = font;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        label.text = text;

        RectTransform rect = label.rectTransform;
        ConfigureAnchoredRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition ?? Vector2.zero);
        rect.sizeDelta = size;
        return label;
    }

    private static Slider CreateSlider(Transform parent, Vector2 anchoredPosition)
    {
        GameObject sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        ConfigureAnchoredRect(sliderRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition);
        sliderRect.sizeDelta = new Vector2(280f, 20f);

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(sliderObject.transform, false);
        Stretch(background.GetComponent<RectTransform>());
        background.GetComponent<Image>().color = new Color(0.2f, 0.24f, 0.3f, 1f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        Stretch(fillArea.GetComponent<RectTransform>(), new Vector2(10f, 5f), new Vector2(-10f, -5f));

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        Stretch(fillRect);
        fill.GetComponent<Image>().color = new Color(0.29f, 0.66f, 0.95f, 1f);

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObject.transform, false);
        Stretch(handleArea.GetComponent<RectTransform>(), new Vector2(10f, 0f), new Vector2(-10f, 0f));

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(18f, 28f);
        Image handleImage = handle.GetComponent<Image>();
        handleImage.color = new Color(0.95f, 0.97f, 1f, 1f);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private static Toggle CreateToggle(Transform parent, Font font, string labelText, Vector2 anchoredPosition)
    {
        GameObject toggleObject = new GameObject("InvertYToggle", typeof(RectTransform), typeof(Toggle));
        toggleObject.transform.SetParent(parent, false);
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        ConfigureAnchoredRect(toggleRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPosition);
        toggleRect.sizeDelta = new Vector2(280f, 28f);

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(toggleObject.transform, false);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        ConfigureAnchoredRect(backgroundRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f));
        backgroundRect.sizeDelta = new Vector2(20f, 20f);
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = new Color(0.2f, 0.24f, 0.3f, 1f);

        GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkmark.transform.SetParent(background.transform, false);
        RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
        ConfigureAnchoredRect(checkmarkRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        checkmarkRect.sizeDelta = new Vector2(12f, 12f);
        Image checkmarkImage = checkmark.GetComponent<Image>();
        checkmarkImage.color = new Color(0.29f, 0.66f, 0.95f, 1f);

        Text label = CreateText("Label", toggleObject.transform, font, 18, TextAnchor.MiddleLeft, new Vector2(244f, 36f), labelText, new Vector2(22f, 0f));
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(1f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.offsetMin = new Vector2(36f, -18f);
        labelRect.offsetMax = new Vector2(0f, 18f);

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = backgroundImage;
        toggle.graphic = checkmarkImage;
        return toggle;
    }

    private static void SavePrefab(GameObject root, string assetPath)
    {
        PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        Object.DestroyImmediate(root);
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
        {
            return;
        }

        int slashIndex = assetPath.LastIndexOf('/');
        if (slashIndex <= 0)
        {
            return;
        }

        string parent = assetPath.Substring(0, slashIndex);
        string folderName = assetPath.Substring(slashIndex + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void ConfigureAnchoredRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
    }

    private static void Stretch(RectTransform rect, Vector2? offsetMin = null, Vector2? offsetMax = null)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin ?? Vector2.zero;
        rect.offsetMax = offsetMax ?? Vector2.zero;
    }

    private static void AssignSerializedField(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
