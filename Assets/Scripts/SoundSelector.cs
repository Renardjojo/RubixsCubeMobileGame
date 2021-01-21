using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SoundSelector : MonoBehaviour
{
    private UnityEvent m_soundClick;
    private UnityEvent m_soundWin;
    [SerializeField] private AudioClip m_soundClickMusic;
    [SerializeField] private List<AudioClip> m_soundWinMusic;
    public int numberOfMusic;
    private AudioSource m_audioSource;
    void Start()
    {
        if (m_soundClick == null)
            m_soundClick = new UnityEvent();
        
        m_soundClick.AddListener(ClickSound);
        m_audioSource = GetComponent<AudioSource>();
        
        if (m_soundWin == null)
            m_soundWin = new UnityEvent();
        
        m_soundWin.AddListener(WinSound);
        numberOfMusic = 0;
    }

    public void ClickSound()
    {
        m_audioSource.PlayOneShot(m_soundClickMusic, 1.5f);
    }

    public void WinSound()
    {
        m_audioSource.PlayOneShot(m_soundWinMusic[numberOfMusic], 2.0f);
    }
}
