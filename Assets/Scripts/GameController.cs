using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    [SerializeField] protected UnityEvent m_onStart;
    
    public void Start()
    {
        AudioListener.volume = PlayerPrefs.GetFloat("generalVolume", 0.5f);
        QualitySettings.SetQualityLevel(PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel()));
        
        m_onStart?.Invoke();
    }

    public void SavePrefs()
    {
        PlayerPrefs.Save();
    }
    
    public void SetGeneralVolume(float newVolume)
    {
        AudioListener.volume = newVolume;
        PlayerPrefs.SetFloat("generalVolume", newVolume);
    }
    
    public void SetInvXAxis(bool flag)
    {
        PlayerPrefs.SetInt("invXAxis", flag ? 1 : 0);
    }

    public void SetInvYAxis(bool flag)
    {
        PlayerPrefs.SetInt("invYAxis", flag ? 1 : 0);
    }
    
    public void OpenUrl(string URL)
    {
        Application.OpenURL(URL);
    }
    
    public void LoadNewSceneWithIndex(int index)
    {
        SceneManager.LoadScene(SceneManager.GetSceneAt(index).ToString(), LoadSceneMode.Single);
    }

    public void LoadNewSceneWithName(string name)
    {
        SceneManager.LoadScene(name, LoadSceneMode.Single);
    }

    public void LoadNewSceneAsyncWithName(string name)
    {
        SceneManager.LoadSceneAsync(name, LoadSceneMode.Single);
    }
    
    public void QuitApplication()
    {
        Application.Quit();
    }
}
