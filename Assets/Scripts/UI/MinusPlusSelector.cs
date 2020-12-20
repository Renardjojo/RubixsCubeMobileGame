using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public struct EventSelectedLabel
{
    public EventSelectedLabel(string newLabel)
    {
        m_labelName = newLabel;
        m_event = null;
    }
    
    public string m_labelName;    
    public UnityEvent m_event;
}

public class MinusPlusSelector : MonoBehaviour
{
    [SerializeField] protected Button m_minusButton;
    [SerializeField] protected Text m_label;
    [SerializeField] protected Button m_plusButton;
    [SerializeField] protected List<EventSelectedLabel> m_eventSelectedLabel;
    [SerializeField] protected int m_index = 0;
    
    // Start is called before the first frame update
    protected virtual void Start()
    {
        m_minusButton.onClick.AddListener(DecrementLabel);
        m_plusButton.onClick.AddListener(IncrementLabel);
        
        m_index = (int)Mathf.Clamp((float)m_index, 0f, (float)(m_eventSelectedLabel.Count - 1));
        
        if (m_eventSelectedLabel.Count != 0)
            m_label.text = m_eventSelectedLabel[m_index].m_labelName;
    }

    public void IncrementLabel()
    {
        if (++m_index >= m_eventSelectedLabel.Count)
        {
            m_index = m_eventSelectedLabel.Count - 1;
        }
        else
        {
            CallEventAndChangeLabel();
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
            CallEventAndChangeLabel();
        }
    }

    void CallEventAndChangeLabel()
    {
        m_eventSelectedLabel[m_index].m_event.Invoke();
        m_label.text = m_eventSelectedLabel[m_index].m_labelName;
    }
}
