using StarterAssets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CameraSensitivityMenu : MonoBehaviour
{
    private const string SensitivityPrefKey = "CameraLookSensitivity";
    private const string InvertYPrefKey = "CameraInvertY";
    private const float MinSensitivity = 0.5f;
    private const float MaxSensitivity = 6f;

    [SerializeField] private CameraSensitivityMenuView menuView;

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
        ArenaCombatant playerCombatant = MiniGameManager.Instance != null ? MiniGameManager.Instance.PlayerCombatant : null;
        _controller = playerCombatant != null ? playerCombatant.Controller : null;
        _inputs = _controller != null ? _controller.GetComponent<StarterAssetsInputs>() : null;

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
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Toggle();
        }
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
        if (menuView != null)
        {
            _menuRoot = menuView.gameObject;
            CacheViewReferences();
            _menuRoot.SetActive(false);
            return;
        }

        Debug.LogError("CameraSensitivityMenu requires a CameraSensitivityMenuView reference from the scene.", this);
    }

    private void CacheViewReferences()
    {
        if (menuView == null)
        {
            return;
        }

        _slider = menuView.SensitivitySlider;
        _valueLabel = menuView.ValueLabel;
        _invertYToggle = menuView.InvertYToggle;

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
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        Debug.LogError("CameraSensitivityMenu could not find an EventSystem in the scene. Configure it directly in the scene.", this);
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
