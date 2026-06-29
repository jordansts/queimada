using StarterAssets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;

public class CameraSensitivityMenu : MonoBehaviour
{
    private const string SensitivityPrefKey = "CameraLookSensitivity";
    private const string InvertYPrefKey = "CameraInvertY";
    private const float MinSensitivity = 0.5f;
    private const float MaxSensitivity = 6f;

    private ThirdPersonController _controller;
    private StarterAssetsInputs _inputs;
    private GameObject _menuRoot;
    private Slider _slider;
    private Text _valueLabel;
    private Toggle _invertYToggle;
    private bool _isOpen;
    private bool _previousCursorLocked = true;
    private bool _previousCursorInputForLook = true;
    private ThirdPersonController _appliedController;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<CameraSensitivityMenu>() != null)
        {
            return;
        }

        GameObject obj = new GameObject("CameraSensitivityMenu");
        DontDestroyOnLoad(obj);
        obj.AddComponent<CameraSensitivityMenu>();
    }

    private void Awake()
    {
        BuildMenu();
        _menuRoot.SetActive(false);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        TryBindController();
        HandleToggle();
    }

    private void TryBindController()
    {
        if (_controller == null)
        {
            _controller = FindFirstObjectByType<ThirdPersonController>();
        }

        if (_inputs == null)
        {
            _inputs = FindFirstObjectByType<StarterAssetsInputs>();
        }

        if (_controller == null || _controller == _appliedController)
        {
            return;
        }

        float saved = PlayerPrefs.GetFloat(SensitivityPrefKey, _controller.LookSensitivity.x);
        ApplySensitivity(saved, false);
        ApplyInvertY(PlayerPrefs.GetInt(InvertYPrefKey, 0) == 1, false);
        _appliedController = _controller;
    }

    private void HandleToggle()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Toggle();
        }
#else
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Toggle();
        }
#endif
    }

    private void Toggle()
    {
        _isOpen = !_isOpen;
        _menuRoot.SetActive(_isOpen);
        Time.timeScale = _isOpen ? 0f : 1f;

        if (_inputs != null)
        {
            if (_isOpen)
            {
                _previousCursorLocked = _inputs.cursorLocked;
                _previousCursorInputForLook = _inputs.cursorInputForLook;
                _inputs.cursorLocked = false;
                _inputs.cursorInputForLook = false;
            }
            else
            {
                _inputs.cursorLocked = _previousCursorLocked;
                _inputs.cursorInputForLook = _previousCursorInputForLook;
            }

            _inputs.ApplyCursorState();
        }

        if (_inputs == null)
        {
            Cursor.lockState = _isOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = _isOpen;
        }

        if (_isOpen && _slider != null && _controller != null)
        {
            _slider.SetValueWithoutNotify(_controller.LookSensitivity.x);
            if (_invertYToggle != null)
            {
                _invertYToggle.SetIsOnWithoutNotify(_controller.InvertLookY);
            }
            UpdateValueLabel(_controller.LookSensitivity.x);
            EventSystem.current?.SetSelectedGameObject(_slider.gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _controller = null;
        _inputs = null;
        _appliedController = null;
        _isOpen = false;
        if (_menuRoot != null)
        {
            _menuRoot.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    private void BuildMenu()
    {
        EnsureEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        _menuRoot = new GameObject("CameraSensitivityCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(_menuRoot);

        Canvas canvas = _menuRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = _menuRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject panel = new GameObject("Panel", typeof(Image));
        panel.transform.SetParent(_menuRoot.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(420f, 220f);
        panel.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.94f);

        CreateLabel(panel.transform, font, "Sensibilidade da Camera", new Vector2(0f, 72f), 28);
        _valueLabel = CreateLabel(panel.transform, font, string.Empty, new Vector2(0f, 18f), 18);
        CreateLabel(panel.transform, font, "ESC para fechar", new Vector2(0f, -92f), 18);

        GameObject sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(panel.transform, false);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = sliderRect.anchorMax = sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.sizeDelta = new Vector2(280f, 20f);
        sliderRect.anchoredPosition = new Vector2(0f, -22f);

        GameObject background = new GameObject("Background", typeof(Image));
        background.transform.SetParent(sliderObject.transform, false);
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        background.GetComponent<Image>().color = new Color(0.2f, 0.24f, 0.3f, 1f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(10f, 5f);
        fillAreaRect.offsetMax = new Vector2(-10f, -5f);

        GameObject fill = new GameObject("Fill", typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.29f, 0.66f, 0.95f, 1f);

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        GameObject handle = new GameObject("Handle", typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(18f, 28f);
        Image handleImage = handle.GetComponent<Image>();
        handleImage.color = new Color(0.95f, 0.97f, 1f, 1f);

        _slider = sliderObject.GetComponent<Slider>();
        _slider.fillRect = fillRect;
        _slider.handleRect = handleRect;
        _slider.targetGraphic = handleImage;
        _slider.direction = Slider.Direction.LeftToRight;
        _slider.minValue = MinSensitivity;
        _slider.maxValue = MaxSensitivity;
        _slider.onValueChanged.AddListener(OnSliderChanged);

        _invertYToggle = CreateToggle(panel.transform, font, "Inverter eixo Y", new Vector2(0f, -56f));
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        DontDestroyOnLoad(eventSystemObject);
    }

    private Text CreateLabel(Transform parent, Font font, string text, Vector2 position, int fontSize)
    {
        GameObject obj = new GameObject("Label", typeof(Text));
        obj.transform.SetParent(parent, false);
        Text label = obj.GetComponent<Text>();
        label.font = font;
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = text;

        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(360f, 36f);
        rect.anchoredPosition = position;
        return label;
    }

    private void OnSliderChanged(float value)
    {
        ApplySensitivity(value, true);
    }

    private void OnInvertYChanged(bool isOn)
    {
        ApplyInvertY(isOn, true);
    }

    private void ApplySensitivity(float value, bool persist)
    {
        float clamped = Mathf.Clamp(value, MinSensitivity, MaxSensitivity);
        if (_controller != null)
        {
            _controller.LookSensitivity = new Vector2(clamped, clamped);
        }

        UpdateValueLabel(clamped);
        if (!persist)
        {
            return;
        }

        PlayerPrefs.SetFloat(SensitivityPrefKey, clamped);
        PlayerPrefs.Save();
    }

    private void ApplyInvertY(bool invertY, bool persist)
    {
        if (_controller != null)
        {
            _controller.InvertLookY = invertY;
        }

        if (!persist)
        {
            return;
        }

        PlayerPrefs.SetInt(InvertYPrefKey, invertY ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void UpdateValueLabel(float value)
    {
        if (_valueLabel != null)
        {
            _valueLabel.text = $"Sensibilidade: {value:0.0}";
        }
    }

    private Toggle CreateToggle(Transform parent, Font font, string labelText, Vector2 position)
    {
        GameObject toggleObject = new GameObject("InvertYToggle", typeof(RectTransform), typeof(Toggle));
        toggleObject.transform.SetParent(parent, false);
        RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
        toggleRect.anchorMin = toggleRect.anchorMax = toggleRect.pivot = new Vector2(0.5f, 0.5f);
        toggleRect.sizeDelta = new Vector2(280f, 28f);
        toggleRect.anchoredPosition = position;

        GameObject background = new GameObject("Background", typeof(Image));
        background.transform.SetParent(toggleObject.transform, false);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.5f);
        backgroundRect.anchorMax = new Vector2(0f, 0.5f);
        backgroundRect.pivot = new Vector2(0f, 0.5f);
        backgroundRect.sizeDelta = new Vector2(20f, 20f);
        backgroundRect.anchoredPosition = new Vector2(8f, 0f);
        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = new Color(0.2f, 0.24f, 0.3f, 1f);

        GameObject checkmark = new GameObject("Checkmark", typeof(Image));
        checkmark.transform.SetParent(background.transform, false);
        RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkmarkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkmarkRect.pivot = new Vector2(0.5f, 0.5f);
        checkmarkRect.sizeDelta = new Vector2(12f, 12f);
        Image checkmarkImage = checkmark.GetComponent<Image>();
        checkmarkImage.color = new Color(0.29f, 0.66f, 0.95f, 1f);

        Text label = CreateLabel(toggleObject.transform, font, labelText, new Vector2(38f, 0f), 18);
        label.alignment = TextAnchor.MiddleLeft;
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(1f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.offsetMin = new Vector2(36f, -18f);
        labelRect.offsetMax = new Vector2(0f, 18f);

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = backgroundImage;
        toggle.graphic = checkmarkImage;
        toggle.onValueChanged.AddListener(OnInvertYChanged);
        return toggle;
    }

    private void OnDestroy()
    {
        if (_isOpen)
        {
            Time.timeScale = 1f;
        }
    }
}
