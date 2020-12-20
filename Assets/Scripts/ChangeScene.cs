using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public void LoadNewSceneWithIndex(int index)
    {
        SceneManager.LoadScene(SceneManager.GetSceneAt(index).ToString(), LoadSceneMode.Single);
    }

    public void LoadNewSceneWithName(string name)
    {
        SceneManager.LoadScene(name, LoadSceneMode.Single);
    }
}
