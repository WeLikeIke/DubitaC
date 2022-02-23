using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Asset-like class, introduces the possibility of having a <see cref="Slider"/> with a <see cref="TextMeshProUGUI"/> on its knob.
/// </summary>
[RequireComponent(typeof(Slider))]
public class SliderWithValueOnKnob : MonoBehaviour {
    public float farLeft;
    public float farRight;
    private float yPosition;
    private TextMeshProUGUI textOnKnob;
    private Slider slider;

    void Awake() {
        textOnKnob = GetComponentInChildren<TextMeshProUGUI>();
        slider = GetComponent<Slider>();

        yPosition = textOnKnob.rectTransform.anchoredPosition.y;

        UpdateKnob();
    }

    void Start() {
        slider.onValueChanged.AddListener(delegate{UpdateKnob();});   
    }

    /// <summary>
    /// Utility function that updates the position of the <see cref="TextMeshProUGUI"/> component to be exactly on the <see cref="Slider"/>'s knob.
    /// </summary>
    public void UpdateKnob() {
        //Set the text number as the slider's value
        textOnKnob.SetText(slider.value.ToString());

        //Starting from a baseline at the leftmost position
        Vector2 basePosition = new Vector2(farLeft, yPosition);
        textOnKnob.rectTransform.anchoredPosition = basePosition;

        //Sanity checks
        if (farLeft < farRight && slider.maxValue > 0) {

            //When the setup is correct, we calculate the new position of the text
            textOnKnob.rectTransform.anchoredPosition += new Vector2(slider.value * ((farRight - farLeft) / slider.maxValue), 0f);
        }

    }
}

