using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class MultipleMaterialSelector : MonoBehaviour
{
    [SerializeField] private List<Material> m_materialContainer;
    private MeshRenderer m_meshRenderer;
    private int currentIdTexture = 0;
    
    // Start is called before the first frame update
    void Awake()
    {
        m_meshRenderer = GetComponent<MeshRenderer>();
        m_meshRenderer.material = m_materialContainer[currentIdTexture];
    }

    public void ChangeTexture(int newId)
    {
        m_meshRenderer.material = m_materialContainer[newId];
        currentIdTexture = newId;
    }
    
    public void ChangeTexture()
    {
        currentIdTexture++;

        if (currentIdTexture >= m_materialContainer.Count)
            currentIdTexture = 0;
        
        m_meshRenderer.material = m_materialContainer[currentIdTexture];
    }
}
