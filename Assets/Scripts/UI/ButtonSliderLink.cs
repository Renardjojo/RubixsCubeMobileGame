using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class FlaotEvent : UnityEvent<float>
{}

[RequireComponent(typeof(Button))]
public class ButtonSliderLink : MonoBehaviour
{
    [SerializeField] private FlaotEvent m_onClickEvent;
    [SerializeField] private Slider m_slider;
    
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }

    void OnClick ()
    {
        m_onClickEvent?.Invoke(m_slider.value);
    }
    
    
}
