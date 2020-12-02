using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Plane = UnityEngine.Plane;
using Quaternion = UnityEngine.Quaternion;
using Random = System.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public struct ResultRayCast
{
    public Vector3 positionMouse;
    public Vector3 normalFace;
    public int indexFace;
    public bool isDefinited;
    public List<GameObject> row;
    public List<GameObject> column;
    public Vector3 normalRow;
    public Vector3 normalColumn;
    public Plane face;
    public bool directionTurnIsDefinited;
    public bool directionRowDefinited;
}

public class Rubikscube : MonoBehaviour
{
    [Header("Rubbix cube properties")]
        [SerializeField] private int m_width;
        [SerializeField] private int m_heigth;
        [SerializeField] private int m_depth;

        [SerializeField] private GameObject m_cubePrefab;
        [SerializeField] private float range;
    
    //The list of subcube that rubbixcube contain
    private List<GameObject> m_cubes;
    
    //The list of plane for each horizontal and vertical rotation
    private List<Plane> m_listPlane;

    //Usefull to check the difference of position between each frame when cursor is clicked
    private Vector2 m_lastCursorPos; 
    
    private ResultRayCast m_resultRayCast;

    //The slice of rubbix that is selected
    private List<GameObject> m_selectedSlice;
    private Plane m_selectedPlane;
    private float m_sliceDeltaAngle = 0f;

    [Header("Input Setting")]
    //This value will be multiplicate by the length of the cursor movement when all the rubbix cube is rotate
        [SerializeField] private float m_rubbixRotInDegByPixel = 0.01f;       
        [SerializeField] private float m_rubbixSliceRotInDegByPixel = 0.01f;

        [SerializeField] private bool m_inverseYAxis = true;
        [SerializeField] private bool m_inverseXAxis = false;
        [SerializeField] private bool m_useMobileInput = false;

    [Header("Debug")]
        [SerializeField] private GameObject m_planePrefab;
        [SerializeField] private bool m_drawPlane = false;
        [SerializeField] private bool m_drawSelectedPlane = false;
        [SerializeField] private bool m_drawSelectedCube = false;

        [SerializeField] private GameObject m_toucheIndicatorDebug;
        [SerializeField] private Material m_debugSelectedMaterial;
        
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
        m_selectedSlice = new List<GameObject>();
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
            m_listPlane.Add(new Plane(Vector3.right, (i / (float)m_width - 0.5f) * m_width + 0.5f));
        
        for (int j = 0; j < m_heigth; j++)
            m_listPlane.Add(new Plane(Vector3.up, (j / (float)m_heigth - 0.5f) * m_heigth + 0.5f));
        
        for (int k = 0; k < m_depth; k++)
            m_listPlane.Add(new Plane(Vector3.forward, (k / (float)m_depth - 0.5f) * m_depth + 0.5f));

        //DEBUG : Display the plane of the rubbix cube
        if (m_drawPlane)
        {
            foreach (var plane in m_listPlane)
            {
                Debug.Log(plane.normal);
                drawDebugPlane(plane);
            }
        }

        m_resultRayCast.isDefinited = false;
        
        m_lastCursorPos = m_useMobileInput ? Input.touches[0].position : (Vector2)Input.mousePosition;
    }
    

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            //for (int indexTouche = 0; indexTouche < Input.touchCount && m_useMobileInput; indexTouche++)
            //{
                UpdateSliceControl();
            //}
        }
        else
        {
            if (m_resultRayCast.isDefinited)
            {
                UnselectRubbixSlice();
            }

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

    void UnselectRubbixSlice()
    {
        m_sliceDeltaAngle %= 90f;
        
        if (Mathf.Abs(m_sliceDeltaAngle) > 45f)
            RotateSlice(m_selectedSlice, (90f - m_sliceDeltaAngle) / m_rubbixSliceRotInDegByPixel, true);
        else
            RotateSlice(m_selectedSlice, m_sliceDeltaAngle / m_rubbixSliceRotInDegByPixel, false);

        m_sliceDeltaAngle = 0f;
        m_selectedPlane = new Plane(Vector3.zero, 0f);
        m_selectedSlice = null;
        m_resultRayCast.isDefinited = false;
        m_resultRayCast.directionTurnIsDefinited = false;
        m_resultRayCast.directionRowDefinited = false;

        UpdateFaceLocation();
    }
    
    void RotateRubbixCube(Vector3 axis, float deltaMovementInPixel)
    {
        transform.Rotate(axis, deltaMovementInPixel * m_rubbixRotInDegByPixel, Space.World);
    }

    void UpdateSliceControl()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        //Rotated slice of rubbix cube if is it selected
        if (m_resultRayCast.isDefinited == true)
        {
            float enter;
            if (m_resultRayCast.face.Raycast(ray, out enter))
            {
                Vector3 direction = ray.GetPoint(enter) - m_resultRayCast.positionMouse;
                if (direction.sqrMagnitude >= range * range)
                {
                    float dotProduct = 0.0f;
                    if (!m_resultRayCast.directionTurnIsDefinited)
                        DefinitedDirectionTurn(direction);

                    if (m_resultRayCast.directionRowDefinited)
                        dotProduct = Vector3.Dot(direction.normalized, m_resultRayCast.normalRow);
                    else
                        dotProduct = Vector3.Dot(direction.normalized, m_resultRayCast.normalColumn);
                    
                    Vector2 movement = m_lastCursorPos - (m_useMobileInput ? Input.touches[/*indexTouche*/ 0].position : (Vector2)Input.mousePosition);
                    float tempX = movement.x;

                    movement.x = m_inverseYAxis ? movement.y : -movement.y;
                    movement.y = m_inverseXAxis ? -tempX : tempX;
                    
                    float deltaMovementInPixel = dotProduct * movement.sqrMagnitude;

                    if ((m_resultRayCast.normalFace == Vector3.down || m_resultRayCast.normalFace == Vector3.left ||
                    m_resultRayCast.normalFace == Vector3.forward) && m_resultRayCast.directionRowDefinited)
                    deltaMovementInPixel *= -1;

                    if ((m_resultRayCast.normalFace == Vector3.up || m_resultRayCast.normalFace == Vector3.right ||
                    m_resultRayCast.normalFace == Vector3.back) && !m_resultRayCast.directionRowDefinited)
                    deltaMovementInPixel *= -1;

                    m_sliceDeltaAngle += deltaMovementInPixel * m_rubbixSliceRotInDegByPixel;

                    RotateSlice(m_selectedSlice, deltaMovementInPixel,  true);
                }
            }
        }
        else
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 1000))
            {
                m_lastCursorPos = Input.mousePosition;
                GameObject parent = hit.collider.gameObject.transform.parent.gameObject;
                m_resultRayCast.normalFace = parent.transform.worldToLocalMatrix * hit.normal;
                m_resultRayCast.indexFace = m_cubes.IndexOf(parent);

                m_selectedSlice = GetColumn(m_resultRayCast.indexFace, m_resultRayCast.normalFace);
                m_resultRayCast.column = m_selectedSlice;
                m_resultRayCast.normalColumn = GetSelectedPlane().normal;
                
                m_selectedSlice = GetRow(m_resultRayCast.indexFace, m_resultRayCast.normalFace);
                m_resultRayCast.row = m_selectedSlice;
                m_resultRayCast.normalRow = GetSelectedPlane().normal;
                
                m_selectedSlice = GetFace(m_resultRayCast.indexFace, m_resultRayCast.normalFace);
                m_resultRayCast.face = GetSelectedPlane();
                
                //DEBUG
                for (int i = 0; i < m_selectedSlice.Count && m_drawSelectedCube; i++)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        GameObject face = m_selectedSlice[i].transform.GetChild(j).gameObject;
                        face.GetComponent<Renderer>().material = m_debugSelectedMaterial;
                    }
                }
                
                
                float enter;
                if (m_resultRayCast.face.Raycast(ray, out enter))
                    m_resultRayCast.positionMouse = ray.GetPoint(enter);

                m_toucheIndicatorDebug.transform.position = hit.point;
                m_resultRayCast.isDefinited = true;
            }
            else
            {
                Vector3 direction = hit.point - m_resultRayCast.positionMouse;
                direction.Normalize();
            }
        }
    }

    void RotateSlice(List<GameObject> slice, float deltaMovementInPixel, bool direction)
    {
        for (int i = 0; i < slice.Count; i++)
        {
            Transform faceTransform = slice[i].transform;

            float currentAngle = 0f;
            Vector3 currentAxis = Vector3.zero;
            transform.rotation.ToAngleAxis(out currentAngle, out currentAxis);

            float sign = direction ? 1f : -1f;
                
            faceTransform.RotateAround(m_selectedPlane.distance * m_selectedPlane.normal, m_selectedPlane.normal,
                  deltaMovementInPixel * m_rubbixSliceRotInDegByPixel * sign);
        }
    }

    void DefinitedDirectionTurn(Vector3 direction)
    {
        float RowDotProduct = Vector3.Dot(direction.normalized, m_resultRayCast.normalRow);
        if (RowDotProduct >= 0.5f || RowDotProduct <= -0.5f)
        {
            m_resultRayCast.directionRowDefinited = true;
            m_selectedSlice = m_resultRayCast.column;
            m_selectedPlane = GetSelectedPlane();
        }
        else
        {
            m_resultRayCast.directionRowDefinited = false;
            m_selectedSlice = m_resultRayCast.row;
            m_selectedPlane = GetSelectedPlane();
        }

        m_resultRayCast.directionTurnIsDefinited = true;
    }
    
    List<GameObject> GetFace(int index, Vector3 normal)
    {
        if (normal == Vector3.back)
            return GetColumn(0, Vector3.right);
        else if (normal == Vector3.right)
            return GetColumn(sizeRubiksCube - 1, Vector3.forward);
        else if (normal == Vector3.left)
            return GetColumn(0, Vector3.back);
        else if (normal == Vector3.forward)
            return GetColumn(sizeRubiksCube * m_depth - 1, Vector3.right);
        else if (normal == Vector3.up)
            return GetRow(sizeRubiksCube - 1, Vector3.right);
        return GetRow(0, Vector3.right);
    }

    List<GameObject> GetColumn(int index, Vector3 normal)
    {
        List<GameObject> column = new List<GameObject>();

        if (normal == Vector3.right || normal == Vector3.left)
        {
            for (int i = index / sizeRubiksCube * sizeRubiksCube; i < m_cubes.Count; i++)
            {
                if (i >= index / sizeRubiksCube * sizeRubiksCube + sizeRubiksCube)
                    return column;
                column.Add(m_cubes[i]);
            }
            return column;
        }
        
        for (int i = index % m_heigth; i < m_cubes.Count; i += m_heigth)
            column.Add(m_cubes[i]);

        return column;
    }

    List<GameObject> GetRow(int index, Vector3 normal)
    {
        List<GameObject> row = new List<GameObject>();

        if (normal == Vector3.up || normal == Vector3.down)
        {
            for (int i = index / sizeRubiksCube * sizeRubiksCube; i < m_cubes.Count; i++)
            {
                if (i >= index / sizeRubiksCube * sizeRubiksCube + sizeRubiksCube)
                    return row;
                row.Add(m_cubes[i]);
            }
            return row;
        }

        int inccrement = 0;
        int indexFirstFace = index < sizeRubiksCube ? index / m_width * m_heigth: (index % (sizeRubiksCube) - 1) / m_width * m_width;
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
    
    public static bool Approximately(float a, float b, float espilon) => Mathf.Abs(b - a) <= espilon;
    
    void UpdateFaceLocation()
    {
        m_cubes.Sort(delegate(GameObject c1, GameObject c2)
        {
            Vector3 localPosC1 = c1.transform.localPosition;
            Vector3 localPosC2 = c2.transform.localPosition;
            const float espilon = 0.001f;

            if (localPosC1.z > localPosC2.z || Approximately(localPosC1.z, localPosC2.z, espilon))
            {
                if (!Approximately(localPosC1.z, localPosC2.z, espilon))
                    return 1;

                if (localPosC1.y > localPosC2.y || Approximately(localPosC1.y, localPosC2.y, espilon))
                {
                    if (!Approximately(localPosC1.y, localPosC2.y, espilon))
                        return 1;

                    if (localPosC1.x > localPosC2.x || Approximately(localPosC1.x, localPosC2.x, espilon))
                    {
                        if (!Approximately(localPosC1.x, localPosC2.x, espilon))
                            return 1;

                        return 0;
                    }
                }
            }
            return -1;
        });
    }

    Plane GetSelectedPlane()
    {
        const float distEpsilon = 0.01f; 
        bool isFound;
        foreach (Plane plane in m_listPlane)
        {
            isFound = true;

            Plane globalPlane = new Plane(transform.TransformPoint(plane.normal), plane.distance);

            foreach (var faceGO in m_selectedSlice)
            {
                if (Mathf.Abs(globalPlane.GetDistanceToPoint(faceGO.transform.position)) > distEpsilon)
                {
                    isFound = false;
                    break;
                }
            }

            if (isFound)
            {
                if (m_drawSelectedPlane)
                    drawDebugPlane(globalPlane);
                
                return globalPlane;
            }
        }
        
        Debug.Log("Cannot found plane");
        return new Plane();
    }

    void drawDebugPlane(Plane plane)
    {
        Quaternion rotation = Quaternion.LookRotation
            (plane.normal) * Quaternion.Euler(90f, 0f, 0f);
        
            GameObject newGo = Instantiate(m_planePrefab, -plane.normal * plane.distance,
                rotation);
            newGo.GetComponent<MeshRenderer>().material.color = new Color(1f, 1f, 1f, 0.05f);
            newGo.transform.SetParent(gameObject.transform);
    }
}