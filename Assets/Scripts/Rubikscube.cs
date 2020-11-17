using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rubikscube : MonoBehaviour
{
    [SerializeField] private int m_width;
    [SerializeField] private int m_heigth;
    [SerializeField] private int m_depth;

    [SerializeField] private GameObject m_cubePrefab;
    
    private List<GameObject> m_cubes;

    private void Awake()
    {
        m_cubes = new List<GameObject>();
    }

    // Start is called before the first frame update
    void Start()
    {
        m_cubes.Capacity = m_heigth * m_width * m_depth;

        for (int k = 0; k < m_depth; k++)
        {
            for (int j = 0; j < m_heigth; j++)
            {
                for (int i = 0; i < m_width; i++)
                {
                    m_cubes.Add(Instantiate(m_cubePrefab, new Vector3(i, j, k), Quaternion.identity));
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
