using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class SliderScript : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private string format = "0";

    // Aparece no Inspector igual ao onClick. Use "Dynamic float" no método.
    public UnityEvent<float> onValueChanged;

    void Awake()
    {
        slider.onValueChanged.AddListener(OnSliderChanged);
    }

    void OnDestroy()
    {
        slider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    // Permite que o menu defina o valor sem disparar o evento (útil ao carregar/resetar)
    public void SetValueWithoutNotify(float v)
    {
        slider.SetValueWithoutNotify(v);
        UpdateText(v);
    }

    void OnSliderChanged(float v)
    {
        UpdateText(v);
        onValueChanged?.Invoke(v);
    }

    void UpdateText(float v)
    {
        if (valueText != null) valueText.text = v.ToString(format);
    }
}