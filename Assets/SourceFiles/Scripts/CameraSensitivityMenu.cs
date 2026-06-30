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
    private const string MenuPrefabPath = "UI/CameraSensitivityMenuCanvas";
    private const string SensitivityPrefKey = "CameraLookSensitivity";
    private const string InvertYPrefKey = "CameraInvertY";
    private const float MinSensitivity = 0.5f;
    private const float MaxSensitivity = 6f;

    private ThirdPersonController _controller;
    private StarterAssetsInputs _inputs;
    private GameObject _menuRoot;
    private CameraSensitivityMenuView _menuView;
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
        if (FindAnyObjectByType<CameraSensitivityMenu>() != null)
        {
            return;
        }

        GameObject obj = new GameObject("CameraSensitivityMenu");
        DontDestroyOnLoad(obj);
        obj.AddComponent<CameraSensitivityMenu>();
    }

    private void Awake()
    {
        BuildMenuFromPrefab();
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
            _controller = FindAnyObjectByType<ThirdPersonController>();
        }

        if (_inputs == null)
        {
            _inputs = FindAnyObjectByType<StarterAssetsInputs>();
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

    private void BuildMenuFromPrefab()
    {
        EnsureEventSystem();
        GameObject prefab = Resources.Load<GameObject>(MenuPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"CameraSensitivityMenu could not load menu prefab at Resources/{MenuPrefabPath}.", this);
            return;
        }

        _menuRoot = Instantiate(prefab);
        _menuRoot.name = prefab.name;
        DontDestroyOnLoad(_menuRoot);
        _menuView = _menuRoot.GetComponent<CameraSensitivityMenuView>();
        if (_menuView == null)
        {
            Debug.LogError("CameraSensitivityMenu prefab is missing CameraSensitivityMenuView.", _menuRoot);
            return;
        }

        _slider = _menuView.SensitivitySlider;
        _valueLabel = _menuView.ValueLabel;
        _invertYToggle = _menuView.InvertYToggle;

        if (_slider != null)
        {
            _slider.minValue = MinSensitivity;
            _slider.maxValue = MaxSensitivity;
            _slider.onValueChanged.AddListener(OnSliderChanged);
        }

        if (_invertYToggle != null)
        {
            _invertYToggle.onValueChanged.AddListener(OnInvertYChanged);
        }

        _menuRoot.SetActive(false);
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
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

    private void OnDestroy()
    {
        if (_isOpen)
        {
            Time.timeScale = 1f;
        }
    }
}
