using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Plane = UnityEngine.Plane;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public struct ResultRayCast
{
    public Vector3 positionMouse;
    public Vector3 normalFace;
    public int indexFace;
    public bool isDefinited;
}

public class Rubikscube : MonoBehaviour
{
    [Header("Rubbix cube properties")]
        [SerializeField] private int m_width;
        [SerializeField] private int m_heigth;
        [SerializeField] private int m_depth;
        
        [SerializeField] private GameObject m_cubePrefab;
    
    //The list of subcube that rubbixcube contain
    private List<GameObject> m_cubes;
    
    //The list of plane for each horizontal and vertical rotation
    private List<Plane> m_listPlane;

    //Usefull to check the difference of position between each frame when cursor is clicked
    private Vector2 m_lastCursorPos; 
    
    private ResultRayCast m_resultRayCast;
    
    [Header("Input Setting")]
    //This value will be multiplicate by the length of the cursor movement when all the rubbix cube is rotate
        [SerializeField] private float m_rubbixRotationSensibility = 1f;
    
        [SerializeField] private bool m_inverseYAxis = true;
        [SerializeField] private bool m_inverseXAxis = false;
        [SerializeField] private bool m_useMobileInput = false;

    [Header("Debug")]
        [SerializeField] private GameObject m_planePrefab;
        [SerializeField] private bool m_drawPlane = false;
        
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
        m_listPlane = new List<Plane>();
    }

    // Start is called before the first frame update
    void Start()
    {
        //Init the subcube of rubbixcube
        m_cubes.Capacity = m_heigth * m_width * m_depth;
        
        float halfdepth = (m_depth - 1) / 2f;
        float halfHeigth = (m_heigth - 1) / 2f;
        float halfWidth = (m_width - 1 )/ 2f;
        
        for (int k = 0; k < m_depth; k++)
        {
            for (int j = 0; j < m_heigth; j++)
            {
                for (int i = 0; i < m_width; i++)
                {
                    m_cubes.Add(Instantiate(m_cubePrefab, new Vector3(i - halfWidth, j - halfHeigth, k - halfdepth), Quaternion.identity));
                    m_cubes.Last().transform.SetParent(gameObject.transform);
                }
            }
        }
        
        //Init plan taht reprensent the rotation horizontal and vertical
        m_listPlane.Capacity = m_heigth + m_width + m_depth;

        for (int i = 0; i < m_width; i++)
            m_listPlane.Add(new Plane(Vector3.right, i / (float)m_width * m_width));
        
        for (int j = 0; j < m_heigth; j++)
            m_listPlane.Add(new Plane(Vector3.up, j / (float)m_heigth * m_heigth));
        
        for (int k = 0; k < m_depth; k++)
            m_listPlane.Add(new Plane(Vector3.forward, k / (float)m_depth * m_depth));

        //DEBUG : Display the plane of the rubbix cube
        if (m_drawPlane)
        {
            foreach (var plane in m_listPlane)
            {
                Debug.Log(plane.normal);

                if (plane.normal == Vector3.right)
                {
                    GameObject newGo = Instantiate(m_planePrefab, plane.normal * plane.distance, Quaternion.Euler(
                        Vector3
                            .forward * -90f));
                    newGo.GetComponent<MeshRenderer>().material.color = new Color(1f, 0f, 0f, 0.1f);
                    newGo.transform.SetParent(gameObject.transform);
                }
                else if (plane.normal == Vector3.up)
                {
                    GameObject newGo = Instantiate(m_planePrefab, plane.normal * plane.distance, Quaternion.identity);
                    newGo.GetComponent<MeshRenderer>().material.color = new Color(0f, 1f, 0f, 0.1f);
                    newGo.transform.SetParent(gameObject.transform);
                }
                else
                {
                    GameObject newGo = Instantiate(m_planePrefab, plane.normal * plane.distance,
                        Quaternion.Euler(Vector3.right * 90f));
                    newGo.GetComponent<MeshRenderer>().material.color = new Color(0f, 0f, 1f, 0.1f);
                    newGo.transform.SetParent(gameObject.transform);
                }
            }
        }

        m_resultRayCast.isDefinited = false;
        
        m_lastCursorPos = m_useMobileInput ? Input.touches[0].position : (Vector2)Input.mousePosition;
    }
    

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0) && Input.touchCount == 1)
        {
            RotateFace((m_lastCursorPos - (Vector2)Input.mousePosition).sqrMagnitude);
        }
        else
        {
            m_resultRayCast.isDefinited = false;
            
            if (Input.GetMouseButton(1) || Input.touchCount > 1)
            {
                Vector2 movement = m_lastCursorPos - (m_useMobileInput ? Input.touches[0].position : (Vector2)Input.mousePosition);
                float tempX = movement.x;
                
                movement.x = m_inverseYAxis ? movement.y : -movement.y;
                movement.y = m_inverseXAxis ? -tempX : tempX;

                RotateRubbixCube(movement.normalized, movement.sqrMagnitude);
            }
        }
        
        m_lastCursorPos = m_useMobileInput ? Input.touches[0].position : (Vector2)Input.mousePosition;
    }

    void RotateRubbixCube(Vector3 axis ,float rotationScale)
    {
        transform.Rotate(axis, rotationScale * m_rubbixRotationSensibility, Space.World);
    }

    void RotateFace(float rotationScale)
    {
        Debug.Log("RotateFace");
        
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
                
                List<GameObject> row = GetColumn(m_resultRayCast.indexFace, hit.normal);
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
