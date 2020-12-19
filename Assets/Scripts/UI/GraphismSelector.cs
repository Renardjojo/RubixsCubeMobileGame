using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GraphismSelector : MonoBehaviour
{
    [SerializeField] protected Button m_minusButton;
    [SerializeField] protected Text m_label;
    [SerializeField] protected Button m_plusButton;
    protected List<String> m_labels;
    protected int m_index = 0;
    
    protected virtual void Start()
    {
        m_index = QualitySettings.GetQualityLevel();

        String[] qualityNames = QualitySettings.names;

        m_labels = new List<string>();
        
        for (int i = 0; i < qualityNames.Length; i++)
        {
            m_labels.Add(qualityNames[i]);
        }
        
        m_minusButton.onClick.AddListener(DecrementLabel);
        m_plusButton.onClick.AddListener(IncrementLabel);

        m_label.text = m_labels[m_index];
    }
    
    public void IncrementLabel()
    {
        if (++m_index >= m_labels.Count)
        {
            m_index = m_labels.Count - 1;
        }
        else
        {
            m_label.text = m_labels[m_index];
            QualitySettings.IncreaseLevel();
            PlayerPrefs.SetInt("QualityLevel", m_index);
        }
    }

    public void DecrementLabel()
    {
        if (--m_index < 0)
        {
            m_index = 0;
        }
        else
        {
            m_label.text = m_labels[m_index];
            QualitySettings.DecreaseLevel();
            PlayerPrefs.SetInt("QualityLevel", m_index);
        }
    }
}
