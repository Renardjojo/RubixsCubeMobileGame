using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderInitlialiserWithPref : MonoBehaviour
{
    [SerializeField] private string m_prefName;
    [SerializeField] private TextSliderLink m_textSliderLinker;

    // Start is called before the first frame update
    void Start()
    {
        Slider slider = GetComponent<Slider>();
        slider.value = PlayerPrefs.GetFloat(m_prefName, slider.value);
        m_textSliderLinker?.SetTextWithRoundFloat(slider.value);
    }

    public void SetValuePref(float in_value)
    {
        PlayerPrefs.SetFloat(m_prefName, in_value);
    }
}
