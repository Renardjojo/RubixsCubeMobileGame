using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class MusicSelector : MonoBehaviour
{
    [SerializeField] private AudioClip[] m_musicContainer;
    private AudioSource m_currentMusic;
    private int indexCurrentMusic = 0;

    // Start is called before the first frame update
    void Awake()
    {
        m_currentMusic = GetComponent<AudioSource>();
    }

    private void Start()
    {
        AudioListener.volume = 0.5f;
        m_currentMusic.clip = m_musicContainer[0];
        m_currentMusic.Play();
    }

    public void SetVolume(float newVolume)
    {
        AudioListener.volume = newVolume;
    }
    
    public void ChangeMusic()
    {
        indexCurrentMusic++;
        if (indexCurrentMusic > m_musicContainer.Length - 1)
            indexCurrentMusic = 0;
        
        m_currentMusic.clip = m_musicContainer[indexCurrentMusic];
        m_currentMusic.Play();
    }
}
