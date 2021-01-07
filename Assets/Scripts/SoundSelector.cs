using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SoundSelector : MonoBehaviour
{
    private UnityEvent m_soundClick;
    [SerializeField] private AudioClip m_soundClickMusic;
    private AudioSource m_audioSource;
    void Start()
    {
        if (m_soundClick == null)
            m_soundClick = new UnityEvent();
        
        m_soundClick.AddListener(ClickSound);
        m_audioSource = GetComponent<AudioSource>();
    }

    public void ClickSound()
    {
        m_audioSource.PlayOneShot(m_soundClickMusic, 1.5f);
    }
}
