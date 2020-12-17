using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class CheckBoxInitialiserWithPref : MonoBehaviour
{
    [SerializeField] private string m_prefName;
    [SerializeField] private bool m_isOnDefaultValue;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Toggle>().isOn = PlayerPrefs.GetInt(m_prefName, m_isOnDefaultValue ? 1 : 0) != 0;
    }
}
