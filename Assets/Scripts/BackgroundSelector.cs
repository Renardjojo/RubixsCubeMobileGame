using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class BackgroundSelector : MonoBehaviour
{
    [SerializeField] private Sprite[] m_backgroundContainer;
    private Image m_currentImage;
    private int indexCurrentBackground = 0;
    
    // Start is called before the first frame update
    void Awake()
    {
        m_currentImage = GetComponent<Image>();
    }

    private void Start()
    {
        m_currentImage.sprite = m_backgroundContainer[0];
    }

    public void ChangeBackground()
    {
        indexCurrentBackground++;
        if (indexCurrentBackground > m_backgroundContainer.Length - 1)
            indexCurrentBackground = 0;
        
        m_currentImage.sprite = m_backgroundContainer[indexCurrentBackground];
    }
}
