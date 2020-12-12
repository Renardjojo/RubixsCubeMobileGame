using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Plane = UnityEngine.Plane;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public struct ResultRayCast
{
    public Vector3 m_positionMouse;
    public Vector3 m_normalFace;
    public int m_indexFace;
    public bool m_isDefinited;
    public List<GameObject> m_row;
    public List<GameObject> m_column;
    public Vector3 m_normalRow;
    public Vector3 m_normalColumn;
    public Plane m_face;
    public bool m_directionTurnIsDefinited;
    public bool m_directionRowDefinited;
}

public class Rubikscube : MonoBehaviour
{
    [Header("Rubbix cube properties")]
        [SerializeField] private int m_width;
        [SerializeField] private int m_heigth;
        [SerializeField] private int m_depth;

        [SerializeField] private GameObject m_cubePrefab;
        [SerializeField] private float m_range;
    
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

    [Header("Suffle Event")]
        [SerializeField] private float m_shuffleRotInDegBySec = 90f;
        private Coroutine m_shuffleCoroutine;

    [Header("Win Event")]
        [SerializeField] private Vector3 m_winRotationAxis;
        [SerializeField] private float m_winRotationSpeedInDegBySec;
        [SerializeField] private UnityEvent m_onPlayerWin;
        private Coroutine m_winCoroutine;
    
    [Header("Lock slice setting")]
        [SerializeField] private float m_lockSliceRotationSpeedInDegBySec;
        private Coroutine m_lockSliceCoroutine;

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
    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    void Init()
    {
        if (m_cubes != null)
        {
            foreach (var cubes in m_cubes)
            {
                GameObject.Destroy(cubes);
            }
            m_cubes.Clear();
        }

        if (m_listPlane != null)
        {
            m_listPlane.Clear();
        }
        
        if (m_selectedSlice != null)
        {
            m_selectedSlice.Clear();
        }
        
        if (m_shuffleCoroutine != null)
        {
            StopCoroutine(m_shuffleCoroutine);
            m_shuffleCoroutine = null;
        }
        
        if (m_winCoroutine != null)
        {
            StopCoroutine(m_winCoroutine);
            m_winCoroutine = null;
        }
        
        if (m_lockSliceCoroutine != null)
        {
            StopCoroutine(m_lockSliceCoroutine);
            m_lockSliceCoroutine = null;
        }
        
        m_cubes = new List<GameObject>();
        m_listPlane = new List<Plane>();
        m_selectedSlice = new List<GameObject>();
        
        //Init default transform
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        
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

        //float scale = 4f / ((m_heigth + m_width + m_depth) / 3f);
        //transform.localScale = new  Vector3(scale, scale, scale);
        
        m_resultRayCast.m_isDefinited = false;
        
        m_lastCursorPos = m_useMobileInput ? Input.touches[0].position : (Vector2)Input.mousePosition;

        //Init default value
        m_lastCursorPos = Vector2.zero;
        m_resultRayCast = new ResultRayCast();
        m_selectedPlane = new Plane(Vector3.zero, 0f);
        m_sliceDeltaAngle = 0f;
    }
    

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject() && m_shuffleCoroutine == null 
        && m_winCoroutine == null && m_lockSliceCoroutine == null)
        {
            UpdateSliceControl();
        }
        else
        {
            if (m_resultRayCast.m_isDefinited)
            {
                UnselectRubbixSlice();
            }

            if ((Input.GetMouseButton(1) || Input.touchCount > 1))
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

    public void SetSizeAndReinit(float size)
    {
        m_width = m_heigth = m_depth = (int)size;
        Init();
    }
    
    void UnselectRubbixSlice()
    {
        m_sliceDeltaAngle %= 90f;
        
        if (m_sliceDeltaAngle > 45f)
            m_lockSliceCoroutine = StartCoroutine(SmoothSliceRotationCorroutine(m_selectedSlice, (90f - m_sliceDeltaAngle), true, m_lockSliceRotationSpeedInDegBySec));
        else if (m_sliceDeltaAngle > 0f)
            m_lockSliceCoroutine = StartCoroutine(SmoothSliceRotationCorroutine(m_selectedSlice, m_sliceDeltaAngle, false, m_lockSliceRotationSpeedInDegBySec));
        else if (m_sliceDeltaAngle > -45f)
            m_lockSliceCoroutine = StartCoroutine(SmoothSliceRotationCorroutine(m_selectedSlice, -m_sliceDeltaAngle, true, m_lockSliceRotationSpeedInDegBySec));
        else
            m_lockSliceCoroutine = StartCoroutine(SmoothSliceRotationCorroutine(m_selectedSlice, (90f + m_sliceDeltaAngle), 
            false, m_lockSliceRotationSpeedInDegBySec));
        
        m_sliceDeltaAngle = 0f;
        m_resultRayCast.m_isDefinited = false;
        m_resultRayCast.m_directionTurnIsDefinited = false;
        m_resultRayCast.m_directionRowDefinited = false;
    }
    
    void RotateRubbixCube(Vector3 axis, float deltaMovementInPixel)
    {
        transform.rotation = Quaternion.AngleAxis(deltaMovementInPixel * m_rubbixRotInDegByPixel, axis) * transform.rotation;
    }

    void UpdateSliceControl()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        //Rotated slice of rubbix cube if is it selected
        if (m_resultRayCast.m_isDefinited == true)
        {
            float enter;
            if (m_resultRayCast.m_face.Raycast(ray, out enter))
            {
                Vector3 direction = ray.GetPoint(enter) - m_resultRayCast.m_positionMouse;
                if (direction.sqrMagnitude >= m_range * m_range)
                {
                    float dotProduct = 0.0f;
                    //if direction is not defined, watch the direction of the mouse
                    if (!m_resultRayCast.m_directionTurnIsDefinited)
                        DefinitedDirectionTurn(direction);

                    if (m_resultRayCast.m_directionRowDefinited)
                        dotProduct = Vector3.Dot(direction.normalized, m_resultRayCast.m_normalRow);
                    else
                        dotProduct = Vector3.Dot(direction.normalized, m_resultRayCast.m_normalColumn);

                    //DEBUG
                    for (int i = 0; i < m_selectedSlice.Count && m_drawSelectedCube; i++)
                    {
                        for (int j = 0; j < 6; j++)
                        {
                            GameObject face = m_selectedSlice[i].transform.GetChild(j).gameObject;
                            face.GetComponent<Renderer>().material = m_debugSelectedMaterial;
                        }
                    }
                    
                    Vector2 movement = m_lastCursorPos - (m_useMobileInput ? Input.touches[/*indexTouche*/ 0].position : (Vector2)Input.mousePosition);
                    float tempX = movement.x;

                    movement.x = m_inverseYAxis ? movement.y : -movement.y;
                    movement.y = m_inverseXAxis ? -tempX : tempX;

                    float deltaMovementInPixel = dotProduct * movement.sqrMagnitude; 

                    if ((m_resultRayCast.m_normalFace == Vector3.down || m_resultRayCast.m_normalFace == Vector3.left ||
                    m_resultRayCast.m_normalFace == Vector3.forward) && m_resultRayCast.m_directionRowDefinited)
                    deltaMovementInPixel *= -1;

                    if ((m_resultRayCast.m_normalFace == Vector3.up || m_resultRayCast.m_normalFace == Vector3.right ||
                    m_resultRayCast.m_normalFace == Vector3.back) && !m_resultRayCast.m_directionRowDefinited)
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
                //Get the cube which is hit and the Rubick's cube's normal
                GameObject parent = hit.collider.gameObject.transform.parent.gameObject;
                m_resultRayCast.m_normalFace = transform.worldToLocalMatrix * hit.normal;
                m_resultRayCast.m_indexFace = m_cubes.IndexOf(parent);
                Debug.Log(m_resultRayCast.m_normalFace);

                //Get Column Slice to have the plane
                m_selectedSlice = GetColumn(m_resultRayCast.m_indexFace, m_resultRayCast.m_normalFace);
                m_resultRayCast.m_column = m_selectedSlice;
                m_resultRayCast.m_normalColumn = GetSelectedPlane().normal;
                
                //Get Row Slice to have the plane
                m_selectedSlice = GetRow(m_resultRayCast.m_indexFace, m_resultRayCast.m_normalFace);
                m_resultRayCast.m_row = m_selectedSlice;
                m_resultRayCast.m_normalRow = GetSelectedPlane().normal;

                //Get Face to check the mouse's position 
                m_selectedSlice = GetFace(m_resultRayCast.m_indexFace, m_resultRayCast.m_normalFace);
                if (m_selectedSlice != null)
                    m_resultRayCast.m_face = GetSelectedPlane();

                //Get the hit point in the plane
                float enter;
                if (m_resultRayCast.m_face.Raycast(ray, out enter))
                    m_resultRayCast.m_positionMouse = ray.GetPoint(enter);

                m_toucheIndicatorDebug.transform.position = hit.point;
                m_resultRayCast.m_isDefinited = true;
            }
        }
    }

    void RotateSlice(List<GameObject> slice, float deltaMovementInPixel, bool direction)
    {
        Vector3 axis = m_shuffleCoroutine != null ? transform.TransformVector(m_selectedPlane.normal) : m_selectedPlane
        .normal;
        
        for (int i = 0; i < slice.Count; i++)
        {
            float sign = direction ? 1f : -1f;

            float angle = deltaMovementInPixel * m_rubbixSliceRotInDegByPixel * sign;
            Vector3 point = m_selectedPlane.distance * axis;
            Vector3 position = slice[i].transform.position;
            slice[i].transform.position = point + Quaternion.AngleAxis(angle, axis) * (position - point);
            slice[i].transform.rotation = Quaternion.AngleAxis(angle, axis) * slice[i].transform.rotation;
        }
    }

    void DefinitedDirectionTurn(Vector3 direction)
    {
        float RowDotProduct = Vector3.Dot(direction.normalized, m_resultRayCast.m_normalRow);
        if (RowDotProduct >= 0.5f || RowDotProduct <= -0.5f)
        {
            m_resultRayCast.m_directionRowDefinited = true;
            m_selectedSlice = m_resultRayCast.m_column;
            m_selectedPlane = GetSelectedPlane();
            Debug.Log("COLUMN");
        }
        else
        {
            m_resultRayCast.m_directionRowDefinited = false;
            m_selectedSlice = m_resultRayCast.m_row;
            m_selectedPlane = GetSelectedPlane();
            Debug.Log("ROW");
        }

        m_resultRayCast.m_directionTurnIsDefinited = true;
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
        else if (normal == Vector3.down)
            return GetRow(0, Vector3.right);
        
        Debug.Log("--------------------------------------------");
        Debug.Log(normal);
        Debug.Log("DON'T FIND");
        Debug.Log("--------------------------------------------");
        return null;
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

        if (normal == Vector3.left)
        {
            int inccrements = 0;
            int indexFirstFaces = index < sizeRubiksCube ? index / m_width * m_heigth: (index % (sizeRubiksCube));
            for (int i = indexFirstFaces; i < m_cubes.Count; i++)
            {
                if (i % m_width == 0 && i != indexFirstFaces && i + sizeRubiksCube - m_width < m_cubes.Count)
                    i += sizeRubiksCube - m_width;

                inccrements += 1;
            
                if (inccrements > m_depth * m_width)
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
        
        //Normalize rotation for more security
        m_cubes.ForEach(delegate(GameObject cube){ cube.transform.rotation.Normalize();});
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
    
    List<GameObject> GetSelectedCubeWithPlane(Plane plane)
    {
        List<GameObject> rst = new List<GameObject>();
        const float distEpsilon = 0.01f;
        foreach (GameObject cube in m_cubes)
        {
            Plane globalPlane = new Plane(transform.TransformPoint(plane.normal), plane.distance);

            if (Mathf.Abs(globalPlane.GetDistanceToPoint(cube.transform.position)) < distEpsilon)
            {
                rst.Add(cube);
            }
        }
        return rst;
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

    bool CheckIfPlayerWin()
    {
        const float espilon = 0.001f;
        
        foreach (var cube in m_cubes)
        {
            if (!((Approximately(cube.transform.localRotation.x, 0f, espilon) && 
                Approximately(cube.transform.localRotation.y, 0f, espilon) && 
                Approximately(cube.transform.localRotation.z, 0f, espilon))))
            {
                return false;
            }
        }

        return true;
    }

    IEnumerator SmoothSliceRotationCorroutine(List<GameObject> slice,  float rotation,  bool direction, 
    float angularSpeedInDegBySec)
    {
        float currentRot = 0f;
        do
        {
            currentRot += angularSpeedInDegBySec * Time.deltaTime;
            float movementInPixel;

            if (currentRot > rotation)
            {
                movementInPixel = ((angularSpeedInDegBySec * Time.deltaTime) - (currentRot - rotation)) /
                                  m_rubbixSliceRotInDegByPixel;
            }
            else
            {
                movementInPixel = angularSpeedInDegBySec * Time.deltaTime /
                                  m_rubbixSliceRotInDegByPixel;
            }
            

            RotateSlice(slice, movementInPixel, direction);

            yield return null;
        } while (currentRot < rotation);
        
        m_selectedPlane = new Plane(Vector3.zero, 0f);
        m_selectedSlice = null;
        
        UpdateFaceLocation();

        if (CheckIfPlayerWin())
        {
            m_onPlayerWin?.Invoke();
        }
        
        m_lockSliceCoroutine = null;
    }

    public void Shuffle(float depth)
    {
        if (m_shuffleCoroutine == null)
        {
            m_shuffleCoroutine = StartCoroutine(ShuffleCorroutine((int) depth));
        }
    }

    IEnumerator ShuffleCorroutine(int depth)
    {
        transform.rotation = Quaternion.identity;
        
        for (int i = 0; i < depth; i++)
        {
            m_selectedPlane = m_listPlane[Random.Range(0, m_listPlane.Count)];
            float rotation = Random.Range(1, 4) * 90f;
            bool direction = Random.Range(0, 2) == 0;
            
            float currentRot = 0f;
            do
            {
                currentRot += m_shuffleRotInDegBySec * Time.deltaTime;
                float movementInPixel;
                if (currentRot > rotation)
                {
                    movementInPixel = ((m_shuffleRotInDegBySec * Time.deltaTime) - (currentRot - rotation)) /
                                      m_rubbixSliceRotInDegByPixel;
                }
                else
                {
                    movementInPixel = m_shuffleRotInDegBySec * Time.deltaTime /
                                      m_rubbixSliceRotInDegByPixel;
                }

                RotateSlice(GetSelectedCubeWithPlane(m_selectedPlane), movementInPixel, direction);

                yield return null;
            } while (currentRot < rotation);
            
            UpdateFaceLocation();
        }
        
        m_shuffleCoroutine = null;
        yield break;
    }

    public void WinRotateRubbixCube()
    {
        if (m_winCoroutine == null)
        {
            m_winCoroutine = StartCoroutine(RotateCoroutine(m_winRotationAxis, m_winRotationSpeedInDegBySec));
        }
    }
    
    IEnumerator RotateCoroutine(Vector3 axis, float rotationSpeedInDeg)
    {
        do
        {
            RotateRubbixCube(axis, rotationSpeedInDeg * Time.deltaTime / m_rubbixRotInDegByPixel);
            yield return null;
            
        } while (true);

        m_winCoroutine = null;
        yield break;
    }
}