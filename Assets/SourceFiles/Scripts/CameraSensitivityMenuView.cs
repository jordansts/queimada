using UnityEngine;
using UnityEngine.UI;

public class CameraSensitivityMenuView : MonoBehaviour
{
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Text valueLabel;
    [SerializeField] private Toggle invertYToggle;

    public Slider SensitivitySlider => sensitivitySlider;
    public Text ValueLabel => valueLabel;
    public Toggle InvertYToggle => invertYToggle;
}
