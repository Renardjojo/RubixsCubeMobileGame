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
    public Vector3 m_normalFace;
    public bool m_isDefinited;
    public List<GameObject> m_row;
    public List<GameObject> m_column;
    public Vector3 m_normalRow;
    public Vector3 m_normalColumn;
    public bool m_directionTurnIsDefinited;
    public bool m_directionRowDefinited;
}

public struct StepResolution
{
    public float angle;
    public int planeID;

    public StepResolution(float newAngle, int newPlaneID)
    {
        angle = newAngle;
        planeID = newPlaneID;
    }
}

public class Rubikscube : MonoBehaviour
{
    [Header("Rubbix cube properties")]
        [SerializeField] private int m_width;
        [SerializeField] private int m_heigth;
        [SerializeField] private int m_depth;

        [SerializeField] private GameObject m_voidCubePrefab;
        [SerializeField] private GameObject m_UnitPlaneXPrefab;
        [SerializeField] private GameObject m_UnitPlaneNegXPrefab;
        [SerializeField] private GameObject m_UnitPlaneYPrefab;
        [SerializeField] private GameObject m_UnitPlaneNegYPrefab;
        [SerializeField] private GameObject m_UnitPlaneZPrefab;
        [SerializeField] private GameObject m_UnitPlaneNegZPrefab;
        [SerializeField] private GameObject m_UnitNeutralPlanePrefab;
        
        private GameObject m_NeutralPlaneRubbix1;
        private GameObject m_NeutralPlaneRubbix2;
        private GameObject m_NeutralPlaneSlice1;
        private GameObject m_NeutralPlaneSlice2;
        
        [SerializeField] private float m_rangeMouseMovement;
        private int m_idCurrentSkin = 0;
    
        //The list of subcube that rubbixcube contain
        private List<GameObject> m_cubes;
        private List<GameObject> m_unsortedCubes;
        
        //The list of plane for each horizontal and vertical rotation
        private List<Plane> m_listPlane;

        //Usefull to check the difference of position between each frame when cursor is clicked
        private Vector2 m_lastCursorPos; 
        
        private ResultRayCast m_resultRayCast;

        //The slice of rubbix that is selected
        private List<GameObject> m_selectedSlice;
        private Plane m_selectedPlane;
        private float m_sliceDeltaAngle;

    [Header("Input Setting")]
    //This value will be multiplicate by the length of the cursor movement when all the rubbix cube is rotate
        [SerializeField] private float m_rubbixRotInDegByPixel = 0.01f;       
        [SerializeField] private float m_rubbixSliceRotInDegByPixel = 0.01f;

        [SerializeField] private bool m_inverseYAxis = true;
        [SerializeField] private bool m_inverseXAxis;
#if UNITY_EDITOR
        [SerializeField] private bool m_useMobileInput;
#endif
    
#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
        private bool m_screenIsTouch = false;
#endif        

        [Header("Suffle Event")]
        [SerializeField] private float m_shuffleRotInDegBySec = 90f;
        private Coroutine m_shuffleCoroutine = null;

    [Header("Win Event")]
        [SerializeField] private Vector3 m_winRotationAxis;
        [SerializeField] private float m_winRotationSpeedInDegBySec;
        [SerializeField] private UnityEvent m_onPlayerWin;
        private Coroutine m_winCoroutine = null;
    
    [Header("Lock slice setting")]
        [SerializeField] private float m_lockSliceRotationSpeedInDegBySec;
        private Coroutine m_lockSliceCoroutine = null;
    
#if UNITY_EDITOR
    [Header("Debug")]
        [SerializeField] private GameObject m_planePrefab;
        [SerializeField] private bool m_drawPlane = false;
        [SerializeField] private bool m_drawSelectedPlane = false;

        [SerializeField] private GameObject m_toucheIndicatorDebug;
#endif

    [Header("Solve setting")]
        [SerializeField] private float m_resolutionRotInDegBySec = 90f;
        private Stack<StepResolution> m_resolutionSteps;
        private int currentPlaneSelectedID = -1;
        private Coroutine m_solveCoroutine = null;
    
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
            m_listPlane.Clear();

        if (m_selectedSlice != null)
            m_selectedSlice.Clear();
        
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
        
        if (m_solveCoroutine != null)
        {
            StopCoroutine(m_solveCoroutine);
            m_solveCoroutine = null;
        }
        
        m_resolutionSteps = new Stack<StepResolution>();

        if (m_NeutralPlaneRubbix1 == null) //If it is null, all Neutral plane is also null
        {
            m_NeutralPlaneRubbix1 = Instantiate(m_UnitNeutralPlanePrefab);
            m_NeutralPlaneRubbix2 = Instantiate(m_UnitNeutralPlanePrefab);
            m_NeutralPlaneSlice1 = Instantiate(m_UnitNeutralPlanePrefab);
            m_NeutralPlaneSlice2 = Instantiate(m_UnitNeutralPlanePrefab);
            
            m_NeutralPlaneRubbix1.transform.localPosition =
            m_NeutralPlaneSlice1.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            
            m_NeutralPlaneRubbix2.transform.localPosition =
            m_NeutralPlaneSlice2.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        }

        m_NeutralPlaneRubbix1.transform.localScale = 
        m_NeutralPlaneRubbix2.transform.localScale = 
        m_NeutralPlaneSlice1.transform.localScale = 
        m_NeutralPlaneSlice2.transform.localScale = new Vector3(m_width, m_heigth, m_depth);
        
        DisableAllGrayFace();
        
        m_cubes = new List<GameObject>();
        m_unsortedCubes = new List<GameObject>();
        m_listPlane = new List<Plane>();
        m_selectedSlice = new List<GameObject>();
        
        //Init default transform
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        
        //Init the subcube of rubbixcube
        m_cubes.Capacity = m_heigth * m_width * m_depth;
        m_unsortedCubes.Capacity = m_cubes.Capacity;
        
        float halfdepth = (m_depth - 1) / 2f;
        float halfHeigth = (m_heigth - 1) / 2f;
        float halfWidth = (m_width - 1 )/ 2f;
        
        for (int k = 0; k < m_depth; k++)
        {
            for (int j = 0; j < m_heigth; j++)
            {
                for (int i = 0; i < m_width; i++)
                {
                    m_cubes.Add(Instantiate(m_voidCubePrefab, new Vector3(i - halfWidth, j - halfHeigth, k - halfdepth), 
                    Quaternion.identity));

                    m_unsortedCubes.Add(m_cubes.Last());
                    m_cubes.Last().transform.SetParent(gameObject.transform);

                    if (i == 0)
                        Instantiate(m_UnitPlaneNegXPrefab).transform.SetParent(m_cubes.Last().transform, false);
                    else if (i == m_width - 1)
                        Instantiate(m_UnitPlaneXPrefab).transform.SetParent(m_cubes.Last().transform, false);
                    
                    if (j == 0)
                        Instantiate(m_UnitPlaneNegYPrefab).transform.SetParent(m_cubes.Last().transform, false);
                    else if (j == m_heigth - 1)
                        Instantiate(m_UnitPlaneYPrefab).transform.SetParent(m_cubes.Last().transform, false);
                    
                    if (k == 0)
                        Instantiate(m_UnitPlaneNegZPrefab).transform.SetParent(m_cubes.Last().transform, false);
                    else if (k == m_depth - 1)
                        Instantiate(m_UnitPlaneZPrefab).transform.SetParent(m_cubes.Last().transform, false);
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
        
#if UNITY_EDITOR
        //DEBUG : Display the plane of the rubbix cube
        if (m_drawPlane)
        {
            foreach (var plane in m_listPlane)
            {
                drawDebugPlane(plane);
            }
        }
#endif
        
        m_resultRayCast.m_isDefinited = false;
        
#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
        m_screenIsTouch = Input.touchCount > 0;
#endif
        
        //Init default value
        m_lastCursorPos = Vector2.zero;
        m_resultRayCast = new ResultRayCast();
        m_selectedPlane = new Plane(Vector3.zero, 0f);
        m_sliceDeltaAngle = 0f;
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        bool sliceClic = (!m_useMobileInput && Input.GetMouseButton(0)) || Input.touchCount == 1;        
#elif UNITY_STANDALONE
        bool sliceClic = Input.GetMouseButton(0);
#else
        bool sliceClic = Input.touchCount == 1;
#endif        

        if (sliceClic && (!EventSystem.current.IsPointerOverGameObject() || m_resultRayCast.m_isDefinited)
                      && m_shuffleCoroutine == null && 
                      m_winCoroutine == null && 
                      m_lockSliceCoroutine == null && 
                      m_solveCoroutine == null)
        {
            UpdateSliceControl();
        }
        else
        {
            if (m_resultRayCast.m_isDefinited)
            {
                UnselectRubbixSlice();
            } else
            
#if UNITY_EDITOR
            if (m_lockSliceCoroutine == null && (!m_useMobileInput && Input.GetMouseButton(1)) || Input.touchCount > 1 && m_screenIsTouch)
            {
                Vector2 movement = m_lastCursorPos - (m_useMobileInput ? Input.GetTouch(0).position : (Vector2)Input.mousePosition);
#elif UNITY_STANDALONE
            if (m_lockSliceCoroutine == null && Input.GetMouseButton(1))
            {
                Vector2 movement = m_lastCursorPos - (Vector2)Input.mousePosition;
#else
            if (m_lockSliceCoroutine == null && Input.touchCount > 1 && m_screenIsTouch)
            {
                Vector2 movement = m_lastCursorPos - Input.GetTouch(0).position;
#endif
                float tempX = movement.x;
                
                movement.x = m_inverseYAxis ? movement.y : -movement.y;
                movement.y = m_inverseXAxis ? -tempX : tempX;

                RotateRubbixCube(movement.normalized, movement.sqrMagnitude);
            }
        }
        
#if UNITY_EDITOR
        m_screenIsTouch = Input.touchCount > 0;
        m_lastCursorPos = m_useMobileInput ? m_screenIsTouch ? Input.GetTouch(0).position : m_lastCursorPos : (Vector2)Input.mousePosition;
#elif UNITY_STANDALONE
        m_lastCursorPos = (Vector2)Input.mousePosition;
#else
        m_screenIsTouch = Input.touchCount > 0;
        m_lastCursorPos = m_screenIsTouch ? Input.GetTouch(0).position : m_lastCursorPos;
#endif
    }

    public void SetSizeAndReinit(float size)
    {
        m_width = m_heigth = m_depth = (int)size;
        Init();
    }

    public void Restart()
    {
        Init();
    }

    void UnselectRubbixSlice()
    {
        float rotationTodo = -m_sliceDeltaAngle % 90f;
        
        if (m_sliceDeltaAngle > 45f)
            rotationTodo += 90f;
        else if (m_sliceDeltaAngle < -45f)
            rotationTodo -= 90f;
 
        m_resolutionSteps.Push(new StepResolution(-(m_sliceDeltaAngle + rotationTodo), currentPlaneSelectedID));
        m_lockSliceCoroutine = StartCoroutine(SmoothSliceRotationCorroutine(m_selectedSlice, rotationTodo, m_lockSliceRotationSpeedInDegBySec));

        m_sliceDeltaAngle = 0f;
        currentPlaneSelectedID = -1;
        m_resultRayCast.m_isDefinited = false;
        m_resultRayCast.m_directionTurnIsDefinited = false;
        m_resultRayCast.m_directionRowDefinited = false;
    }
    
    void RotateRubbixCube(Vector3 axis, float deltaMovementInPixel)
    {
        transform.rotation = Quaternion.AngleAxis(deltaMovementInPixel * m_rubbixRotInDegByPixel, axis) * transform.rotation;
        RefreachPrecisionPlane();
    }

    void UpdateSliceControl()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        //Rotated slice of rubbix cube if is it selected
        if (m_resultRayCast.m_isDefinited == true)
        {
#if UNITY_EDITOR
            Vector2 movement = m_lastCursorPos - (m_useMobileInput ? Input.GetTouch(0).position : (Vector2)Input.mousePosition);
#elif UNITY_STANDALONE
            Vector2 movement = m_lastCursorPos - (Vector2)Input.mousePosition;
#else
            Vector2 movement = m_lastCursorPos - Input.GetTouch(0).position;
#endif
            
            if (movement.sqrMagnitude >= m_rangeMouseMovement * m_rangeMouseMovement)
            {
                float dotProduct = 0.0f;
                
                //if direction is not defined, watch the direction of the mouse
                if (!m_resultRayCast.m_directionTurnIsDefinited) 
                    DefinitedDirectionTurn(-movement.normalized);
                    
                if (m_resultRayCast.m_directionRowDefinited)
                    dotProduct = Vector3.Dot(-movement.normalized, m_resultRayCast.m_normalRow);
                else
                    dotProduct = Vector3.Dot(-movement.normalized, m_resultRayCast.m_normalColumn);

                float tempX = movement.x;

                movement.x = m_inverseYAxis ? movement.y : -movement.y;
                movement.y = m_inverseXAxis ? -tempX : tempX;

                float deltaMovementInPixel = movement.sqrMagnitude;

                if ((m_resultRayCast.m_normalFace == Vector3.down || m_resultRayCast.m_normalFace == Vector3.left ||
                     m_resultRayCast.m_normalFace == Vector3.forward) && m_resultRayCast.m_directionRowDefinited)
                    deltaMovementInPixel *= -1;

                if ((m_resultRayCast.m_normalFace == Vector3.up || m_resultRayCast.m_normalFace == Vector3.right ||
                     m_resultRayCast.m_normalFace == Vector3.back) && !m_resultRayCast.m_directionRowDefinited)
                    deltaMovementInPixel *= -1;
                    
                if (dotProduct <= 0)
                    deltaMovementInPixel *= -1;

                m_sliceDeltaAngle += deltaMovementInPixel * m_rubbixSliceRotInDegByPixel;

                RotateSlice(m_selectedSlice, deltaMovementInPixel * m_rubbixSliceRotInDegByPixel);
            }
        }
        else
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 1000))
            {
                //Get the cube which is hit and the Rubick's cube's normal
                GameObject parent = hit.collider.gameObject.transform.parent.gameObject;
                m_resultRayCast.m_normalFace = transform.worldToLocalMatrix * hit.normal;
                int indexFace = m_cubes.IndexOf(parent);

                //Get Column Slice to have the plane
                m_selectedSlice = GetColumn(indexFace, m_resultRayCast.m_normalFace);
                m_resultRayCast.m_column = m_selectedSlice;
                m_resultRayCast.m_normalColumn = GetSelectedPlane().normal;

                //Get Row Slice to have the plane
                m_selectedSlice = GetRow(indexFace, m_resultRayCast.m_normalFace);
                m_resultRayCast.m_row = m_selectedSlice;
                m_resultRayCast.m_normalRow = GetSelectedPlane().normal;

#if UNITY_EDITOR
                m_toucheIndicatorDebug.transform.position = hit.point;
#endif
                m_resultRayCast.m_isDefinited = true;
            }
        }
    }

    void RotateSlice(List<GameObject> slice, float angleInDeg)
    {
        Vector3 axis = (m_shuffleCoroutine != null ||  m_solveCoroutine != null) ? 
        transform.TransformVector(m_selectedPlane
        .normal) : m_selectedPlane
        .normal;
        
        Vector3 point = m_selectedPlane.distance * axis;
        
        for (int i = 0; i < slice.Count; i++)
        {
            Vector3 position = slice[i].transform.position;
            slice[i].transform.position = point + Quaternion.AngleAxis(angleInDeg, axis) * (position - point);
            slice[i].transform.rotation = Quaternion.AngleAxis(angleInDeg, axis) * slice[i].transform.rotation;
        }
        
        Vector3 up = Mathf.Abs(Vector3.Dot(slice.Last().transform.up, axis)) < 0.5f
            ? slice.Last().transform.up
            : Mathf.Abs(Vector3.Dot(slice.Last().transform.right, axis)) < 0.5f 
                ? slice.Last().transform.right 
                : slice.Last().transform.forward;
        
        m_NeutralPlaneSlice1.transform.rotation = Quaternion.LookRotation(-axis, up);
        m_NeutralPlaneSlice2.transform.rotation = Quaternion.LookRotation(axis, up);
    }

    void DefinitedDirectionTurn(Vector3 direction)
    {
        float RowDotProduct = Vector3.Dot(direction.normalized, m_resultRayCast.m_normalRow);
        if (RowDotProduct >= 0.5f || RowDotProduct <= -0.5f)
        {
            m_resultRayCast.m_directionRowDefinited = true;
            m_selectedSlice = m_resultRayCast.m_column;
        }
        else
        {
            m_resultRayCast.m_directionRowDefinited = false;
            m_selectedSlice = m_resultRayCast.m_row;
        }
        
        m_selectedPlane = GetSelectedPlane();

        FillSliceVoidWithNeutralPlane();
        
        m_resultRayCast.m_directionTurnIsDefinited = true;
    }

    void FillSliceVoidWithNeutralPlane()
    {
        Vector3 axis = (m_shuffleCoroutine != null ||  m_solveCoroutine != null) ? transform.TransformVector(m_selectedPlane.normal) : m_selectedPlane
            .normal;

        if (m_selectedPlane.distance < (0.5f * m_width - 0.5f))
        {
            m_NeutralPlaneRubbix2.SetActive(true);
            m_NeutralPlaneSlice2.SetActive(true);
        }
        
        if (m_selectedPlane.distance > -(0.5f * m_width - 0.5f))
        {
            m_NeutralPlaneRubbix1.SetActive(true);
            m_NeutralPlaneSlice1.SetActive(true);
        }
        
        m_NeutralPlaneRubbix1.transform.position =
            m_NeutralPlaneSlice1.transform.position = axis * -(m_selectedPlane.distance - 0.5f);
        
        m_NeutralPlaneRubbix2.transform.position = 
            m_NeutralPlaneSlice2.transform.position = axis * -(m_selectedPlane.distance + 0.5f);
        
        Vector3 up = Mathf.Abs(Vector3.Dot(transform.up, axis)) < 0.5f
            ? transform.up
            : Mathf.Abs(Vector3.Dot(transform.right, axis)) < 0.5f ? transform.right : transform.forward;
        
        m_NeutralPlaneRubbix2.transform.rotation =
            m_NeutralPlaneSlice2.transform.rotation = Quaternion.LookRotation(-axis, up);
        
        m_NeutralPlaneRubbix1.transform.rotation =
            m_NeutralPlaneSlice1.transform.rotation = Quaternion.LookRotation(axis, up);
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
        
        if (normal == Vector3.forward)
        {
            int indexFace = index % sizeRubiksCube;
            if (indexFace % m_width != 0)
                indexFace -= indexFace % m_width;
            
            int inc = 0;
            for (int i = indexFace; i < m_cubes.Count; i++)
            {
                if (i % m_width == 0 && i != indexFace && i + sizeRubiksCube - m_width < m_cubes.Count)
                    i += sizeRubiksCube - m_width;

                inc += 1;
            
                if (inc > m_depth * m_width)
                    return row;
            
                row.Add(m_cubes[i]);
            }

            return row;
        }
        
        int increment = 0;
        int indexFirstFace = index < sizeRubiksCube ? index / m_width * m_heigth: (index % (sizeRubiksCube) - 1) / m_width * m_width;
        for (int i = indexFirstFace; i < m_cubes.Count; i++)
        {
            if (i % m_width == 0 && i != indexFirstFace && i + sizeRubiksCube - m_width < m_cubes.Count)
                i += sizeRubiksCube - m_width;

            increment += 1;
            
            if (increment > m_depth * m_width)
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
        const float distEpsilon = 0.2f; 
        bool isFound;
        bool loopOnceIfPlaneNotFound = true;

        do
        {
            for (int idPlane = 0; idPlane < m_listPlane.Count; idPlane++)
            {
                isFound = true;

                Plane globalPlane = new Plane(transform.TransformPoint(m_listPlane[idPlane].normal),
                    m_listPlane[idPlane].distance);

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
#if UNITY_EDITOR
                    if (m_drawSelectedPlane)
                        drawDebugPlane(globalPlane);
#endif
                    currentPlaneSelectedID = idPlane;
                    return globalPlane;
                }
            }

            loopOnceIfPlaneNotFound = false;
            RefreachPrescisionCube();
            RefreachPrecisionPlane();
            Debug.LogWarning("Cube or plane corruption");

        } while (loopOnceIfPlaneNotFound);
        
        Debug.LogError("Cube still corrupted after plane and cube refreach");
        Restart();
        
        return new Plane();
    }

    void RefreachPrescisionCube()
    {
        float halfdepth = (m_depth - 1) / 2f;
        float halfHeigth = (m_heigth - 1) / 2f;
        float halfWidth = (m_width - 1 )/ 2f;

        for (int k = 0; k < m_depth; k++)
        {
            for (int j = 0; j < m_heigth; j++)
            {
                for (int i = 0; i < m_width; i++)
                {
                    Transform cubeTransform = m_unsortedCubes[i + j * m_width + k * m_heigth * m_width].transform;
                    cubeTransform.localRotation.Normalize();
                    cubeTransform.transform.localPosition = cubeTransform.transform.localRotation * new Vector3(i - 
                    halfWidth, j - halfHeigth, k - halfdepth);
                }
            }
        }
    }

    void RefreachPrecisionPlane()
    {
        Vector3 up = transform.up;
        Vector3 right = transform.right;
        Vector3 forward = transform.forward;
        
        m_listPlane.ForEach(delegate(Plane plane)
        {
            if (Vector3.Dot(plane.normal, up) > 0.5f)
            {
                plane.normal = up;
            }
            else if (Vector3.Dot(plane.normal, right) > 0.5f)
            {
                plane.normal = right;
            }
            else
            {
                plane.normal = forward;
            } 
        });
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

#if UNITY_EDITOR
    void drawDebugPlane(Plane plane)
    {
        Quaternion rotation = Quaternion.LookRotation
            (plane.normal) * Quaternion.Euler(90f, 0f, 0f);
        
            GameObject newGo = Instantiate(m_planePrefab, -plane.normal * plane.distance,
                rotation);
            newGo.GetComponent<MeshRenderer>().material.color = new Color(1f, 1f, 1f, 0.05f);
            newGo.transform.SetParent(gameObject.transform);
    }
#endif

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

    IEnumerator SmoothSliceRotationCorroutine(List<GameObject> slice, float rotation, float angularSpeedInDegBySec)
    {
        yield return MultipleSliceRotationSequence(slice, rotation, angularSpeedInDegBySec);
        
        m_selectedPlane = new Plane();
        m_selectedSlice = null;

        if (CheckIfPlayerWin())
        {
            m_onPlayerWin?.Invoke();
        }
        m_lockSliceCoroutine = null;
    }
    void DisableAllGrayFace()
    {
        m_NeutralPlaneRubbix1.gameObject.SetActive(false);
        m_NeutralPlaneRubbix2.gameObject.SetActive(false);
        m_NeutralPlaneSlice1.gameObject.SetActive(false);
        m_NeutralPlaneSlice2.gameObject.SetActive(false);
    }

    IEnumerator MultipleSliceRotationSequence(List<GameObject> slice, float rotation, float angularSpeedInDegBySec)
    {
        int rotationSign = rotation < 0f ? -1 : 1;
        float currentRot = 0f;
        rotation = Mathf.Abs(rotation);
        
        do
        {
            FillSliceVoidWithNeutralPlane();
            
            currentRot += angularSpeedInDegBySec * Time.deltaTime;
            
            float angleInDeg;
            
            if (currentRot > rotation)
                angleInDeg = ((angularSpeedInDegBySec * Time.deltaTime) - (currentRot - rotation));
            else
                angleInDeg = angularSpeedInDegBySec * Time.deltaTime;

            RotateSlice(slice, angleInDeg * rotationSign);

            yield return null;
        } while (currentRot < rotation);
        
        DisableAllGrayFace();
        UpdateFaceLocation();
    }
    
    public void Solve()
    {
        if (m_shuffleCoroutine == null && m_solveCoroutine == null && m_lockSliceCoroutine == null)
        {
            m_solveCoroutine = StartCoroutine(SolveCorroutine());
        }
    }

    IEnumerator SolveCorroutine()
    {
        transform.rotation = Quaternion.identity;
        
        while (m_resolutionSteps.Count > 0)
        {
            StepResolution stepResolutionData;
            
            //Try to avoid to turn many time the same slice
            float angleToDo = 0f;
            do
            {
                stepResolutionData = m_resolutionSteps.Pop();
                angleToDo += stepResolutionData.angle;
                
            } while (m_resolutionSteps.Count > 0 && m_resolutionSteps.Peek().planeID == stepResolutionData.planeID);
            
            m_selectedPlane = m_listPlane[stepResolutionData.planeID];
            angleToDo %= 360f;
            if (Approximately(angleToDo, 0f, 0))
                continue;
            
            //Avoid unecessary rotation the fell that solve copy all movement
            float shortestAngle;
            if (stepResolutionData.angle > 180f)
                shortestAngle = angleToDo - 360f;
            else if (stepResolutionData.angle < -180f)
                shortestAngle = angleToDo + 360f;
            else
                shortestAngle = angleToDo;
            
            yield return MultipleSliceRotationSequence(GetSelectedCubeWithPlane(m_selectedPlane), shortestAngle, 
            m_resolutionRotInDegBySec);
        }
        
        m_solveCoroutine = null;
    }
    
    public void Shuffle(float depth)
    {
        if (m_shuffleCoroutine == null && m_solveCoroutine == null  && m_lockSliceCoroutine == null)
        {
            m_shuffleCoroutine = StartCoroutine(ShuffleCorroutine((int) depth));
        }
    }

    IEnumerator ShuffleCorroutine(int depth)
    {
        transform.rotation = Quaternion.identity;
        
        for (int i = 0; i < depth; i++)
        {
            int planID = Random.Range(0, m_listPlane.Count);
            m_selectedPlane = m_listPlane[planID];
            float rotation = Random.Range(1, 4) * 90f * (Random.Range(0, 2) == 0 ? -1 : 1);

            m_resolutionSteps.Push( new StepResolution(-rotation, planID));
            yield return MultipleSliceRotationSequence(GetSelectedCubeWithPlane(m_selectedPlane), rotation, m_shuffleRotInDegBySec);
        }
        
        m_shuffleCoroutine = null;
    }

    public void WinRotateRubbixCube()
    {
        if (m_winCoroutine == null)
        {
            m_winCoroutine = StartCoroutine(InfinitRotateCoroutine(m_winRotationAxis, m_winRotationSpeedInDegBySec));
        }
    }
    
    IEnumerator InfinitRotateCoroutine(Vector3 axis, float rotationSpeedInDeg)
    {
        do
        {
            RotateRubbixCube(axis, rotationSpeedInDeg * Time.deltaTime / m_rubbixRotInDegByPixel);
            yield return null;
            
        } while (true);
    }

    public void ChangeRubbixSkin()
    {
        foreach (var cube in m_cubes)
        {
            for (int i = 0; i < cube.transform.childCount; i++)
            {
                //Can be improve but fast to write it
                cube.transform.GetChild(i).GetComponent<MultipleMaterialSelector>().ChangeTexture();
            }
        }
        
        m_NeutralPlaneRubbix1.GetComponent<MultipleMaterialSelector>().ChangeTexture();
        m_NeutralPlaneRubbix2.GetComponent<MultipleMaterialSelector>().ChangeTexture();
        m_NeutralPlaneSlice1.GetComponent<MultipleMaterialSelector>().ChangeTexture();
        m_NeutralPlaneSlice2.GetComponent<MultipleMaterialSelector>().ChangeTexture();
    }
}