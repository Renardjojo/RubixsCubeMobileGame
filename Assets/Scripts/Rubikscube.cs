using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public struct ResultRayCast
{
    public Vector3 positionMouse;
    public Vector3 normalFace;
    public int indexFace;
    public bool isDefinited;
}

public class Rubikscube : MonoBehaviour
{
    [SerializeField] private int m_width;
    [SerializeField] private int m_heigth;
    [SerializeField] private int m_depth;

    [SerializeField] private GameObject m_cubePrefab;
    
    private List<GameObject> m_cubes;

    private ResultRayCast m_resultRayCast;
    
    
    [SerializeField] private GameObject touchTemp;
    [SerializeField] private Material newMat;
    
    
    public int sizeRubiksCube
    {
        get
        {
            return m_width * m_heigth;
        }
    }

    
    
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

        m_resultRayCast.isDefinited = false;
    }
    

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); 
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 1000))
            {
                GameObject parent = hit.collider.gameObject.transform.parent.gameObject;
                if (m_resultRayCast.isDefinited == false)
                {
                    m_resultRayCast.normalFace = hit.normal;
                    m_resultRayCast.indexFace = m_cubes.IndexOf(parent);
                    m_resultRayCast.positionMouse = hit.point;
                    m_resultRayCast.isDefinited = true;
                
                    touchTemp.transform.position = m_resultRayCast.positionMouse;
                
                    List<GameObject> row = GetRow(m_resultRayCast.indexFace, hit.normal);
                    for (int i = 0; i < row.Count; i++)
                    {
                        for (int j = 0; j < 6; j++)
                        {
                            GameObject face = row[i].transform.GetChild(j).gameObject;
                            face.GetComponent<Renderer>().material = newMat;
                        }
                    }
                }
                else
                {
                    Vector3 direction = hit.point - m_resultRayCast.positionMouse;
                    direction.Normalize();
                    
                }
            }
        }
        else
        {
            m_resultRayCast.isDefinited = false;
        }
        
    }

    List<GameObject> GetColumn(int Index, Vector3 normal)
    {
        List<GameObject> column = new List<GameObject>();

        if (normal == Vector3.right || normal == Vector3.left)
        {
            for (int i = Index / sizeRubiksCube * sizeRubiksCube; i < m_cubes.Count; i++)
            {
                if (i >= Index / sizeRubiksCube * sizeRubiksCube + sizeRubiksCube)
                    return column;
                column.Add(m_cubes[i]);
            }
            return column;
        }
        
        for (int i = Index % m_heigth; i < m_cubes.Count; i += m_heigth)
            column.Add(m_cubes[i]);

        return column;
    }

    List<GameObject> GetRow(int Index, Vector3 normal)
    {
        List<GameObject> row = new List<GameObject>();

        if (normal == Vector3.up || normal == Vector3.down)
        {
            for (int i = Index / sizeRubiksCube * sizeRubiksCube; i < m_cubes.Count; i++)
            {
                if (i >= Index / sizeRubiksCube * sizeRubiksCube + sizeRubiksCube)
                    return row;
                row.Add(m_cubes[i]);
            }
            return row;
        }

        int inccrement = 0;
        int indexFirstFace = Index < sizeRubiksCube ? Index / m_width * m_heigth: (Index % (sizeRubiksCube) - 1) / m_width * m_width;
        for (int i = indexFirstFace; i < m_cubes.Count; i++)
        {
            if (i % m_width == 0 && i != indexFirstFace && i + sizeRubiksCube - m_width < m_cubes.Count)
                i += sizeRubiksCube - m_width;

            inccrement += 1;
            
            if (inccrement > m_depth * m_width)
                return row;
            
            row.Add(m_cubes[i]);
        }
        return row;
    }
}
