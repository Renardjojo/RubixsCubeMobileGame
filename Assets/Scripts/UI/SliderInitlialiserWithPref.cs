using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderInitlialiserWithPref : MonoBehaviour
{
    [SerializeField] private string m_prefName;
    [SerializeField] private float m_defaultValue;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Slider>().value = PlayerPrefs.GetFloat(m_prefName, m_defaultValue);
    }
}
