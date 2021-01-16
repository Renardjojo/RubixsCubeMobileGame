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

[System.Serializable]
public struct Skin
{
    public Material x, minX, y, minY, z, minZ, neutral;
}

public class RubiksCube : MonoBehaviour
{
    [Header("Rubix cube properties")]
        [SerializeField] protected int m_width = 3;
        [SerializeField] protected int m_heigth = 3;
        [SerializeField] protected int m_depth = 3;

        [SerializeField] protected GameObject m_voidCubePrefab;
        [SerializeField] protected GameObject m_UnitPlaneXPrefab;
        [SerializeField] protected GameObject m_UnitPlaneNegXPrefab;
        [SerializeField] protected GameObject m_UnitPlaneYPrefab;
        [SerializeField] protected GameObject m_UnitPlaneNegYPrefab;
        [SerializeField] protected GameObject m_UnitPlaneZPrefab;
        [SerializeField] protected GameObject m_UnitPlaneNegZPrefab;
        [SerializeField] protected GameObject m_UnitNeutralPlanePrefab;
        
        protected GameObject m_NeutralPlaneRubix1;
        protected GameObject m_NeutralPlaneRubix2;
        protected GameObject m_NeutralPlaneSlice1;
        protected GameObject m_NeutralPlaneSlice2;
        
        //The list of subcube that rubixcube contain
        protected List<GameObject> m_cubes;
        protected List<GameObject> m_unsortedCubes;
        
        //The list of plane for each horizontal and vertical rotation
        protected List<Plane> m_listPlane;

        //The slice of rubix that is selected
        protected List<GameObject> m_selectedSlice;
        protected Plane m_selectedPlane;

        [Header("Skins")] 
        [SerializeField] protected List<Skin> m_skins;
        [SerializeField] protected int m_currentSkin = 0;
        [SerializeField] protected UnityEvent m_onStart;

    [Header("Suffle Event")]
        [SerializeField] protected float m_shuffleRotInDegBySec = 720;
        protected Coroutine m_shuffleCoroutine = null;

#if UNITY_EDITOR
    [Header("Debug")]
        [SerializeField] protected GameObject m_planePrefab;
        [SerializeField] protected bool m_drawPlane = false;
        [SerializeField] protected bool m_drawSelectedPlane = false;
#endif

    [Header("Solve setting")]
        [SerializeField] protected float m_resolutionRotInDegBySec = 270;
        protected Stack<StepResolution> m_resolutionSteps;
        protected int currentPlaneSelectedID = -1;
        protected Coroutine m_solveCoroutine = null;
    
    public int SizeRubiksCube
    {
        get
        {
            return m_width * m_heigth;
        }
    }
    
    // Start is called before the first frame update
    protected virtual void Start()
    {
        m_shuffleRotInDegBySec *= PlayerPrefs.GetFloat("ShuffleSpeed", 1f);
        m_resolutionRotInDegBySec *= PlayerPrefs.GetFloat("ShuffleSpeed", 1f);
            
        Init();
        m_onStart?.Invoke();
    }

    protected virtual void Init()
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

        if (m_solveCoroutine != null)
        {
            StopCoroutine(m_solveCoroutine);
            m_solveCoroutine = null;
        }
        
        m_resolutionSteps = new Stack<StepResolution>();

        if (m_NeutralPlaneRubix1 == null) //If it is null, all Neutral plane is also null
        {
            m_NeutralPlaneRubix1 = Instantiate(m_UnitNeutralPlanePrefab);
            m_NeutralPlaneRubix2 = Instantiate(m_UnitNeutralPlanePrefab);
            m_NeutralPlaneSlice1 = Instantiate(m_UnitNeutralPlanePrefab);
            m_NeutralPlaneSlice2 = Instantiate(m_UnitNeutralPlanePrefab);
            
            m_NeutralPlaneRubix1.transform.localPosition =
            m_NeutralPlaneSlice1.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            
            m_NeutralPlaneRubix2.transform.localPosition =
            m_NeutralPlaneSlice2.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        }

        m_NeutralPlaneRubix1.transform.localScale = 
        m_NeutralPlaneRubix2.transform.localScale = 
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
        
        //Init the subcube of rubixcube
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
        
        ChangeRubixSkin(m_currentSkin);

        //Init plan taht reprensent the rotation horizontal and vertical
        m_listPlane.Capacity = m_heigth + m_width + m_depth;

        for (int i = 0; i < m_width; i++)
            m_listPlane.Add(new Plane(Vector3.right, (i / (float)m_width - 0.5f) * m_width + 0.5f));
        
        for (int j = 0; j < m_heigth; j++)
            m_listPlane.Add(new Plane(Vector3.up, (j / (float)m_heigth - 0.5f) * m_heigth + 0.5f));
        
        for (int k = 0; k < m_depth; k++)
            m_listPlane.Add(new Plane(Vector3.forward, (k / (float)m_depth - 0.5f) * m_depth + 0.5f));
        
#if UNITY_EDITOR
        //DEBUG : Display the plane of the rubix cube
        if (m_drawPlane)
        {
            foreach (var plane in m_listPlane)
            {
                drawDebugPlane(plane);
            }
        }
#endif
        
        m_selectedPlane = new Plane(Vector3.zero, 0f);
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

    protected void RotateRubixCube(Vector3 axis, float angleInDeg)
    {
        transform.rotation = Quaternion.AngleAxis(angleInDeg, axis) * transform.rotation;
        RefreachPrecisionPlane();
    }

    protected void RotateSlice(List<GameObject> slice, float angleInDeg)
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

    protected void FillSliceVoidWithNeutralPlane()
    {
        Vector3 axis = (m_shuffleCoroutine != null ||  m_solveCoroutine != null) ? transform.TransformVector(m_selectedPlane.normal) : m_selectedPlane
            .normal;

        if (m_selectedPlane.distance < (0.5f * m_width - 0.5f))
        {
            m_NeutralPlaneRubix2.SetActive(true);
            m_NeutralPlaneSlice2.SetActive(true);
        }
        
        if (m_selectedPlane.distance > -(0.5f * m_width - 0.5f))
        {
            m_NeutralPlaneRubix1.SetActive(true);
            m_NeutralPlaneSlice1.SetActive(true);
        }
        
        m_NeutralPlaneRubix1.transform.position =
            m_NeutralPlaneSlice1.transform.position = axis * -(m_selectedPlane.distance - 0.5f);
        
        m_NeutralPlaneRubix2.transform.position = 
            m_NeutralPlaneSlice2.transform.position = axis * -(m_selectedPlane.distance + 0.5f);
        
        Vector3 up = Mathf.Abs(Vector3.Dot(transform.up, axis)) < 0.5f
            ? transform.up
            : Mathf.Abs(Vector3.Dot(transform.right, axis)) < 0.5f ? transform.right : transform.forward;
        
        m_NeutralPlaneRubix2.transform.rotation =
            m_NeutralPlaneSlice2.transform.rotation = Quaternion.LookRotation(-axis, up);
        
        m_NeutralPlaneRubix1.transform.rotation =
            m_NeutralPlaneSlice1.transform.rotation = Quaternion.LookRotation(axis, up);
    }
    
    protected List<GameObject> GetColumn(int index, Vector3 normal)
    {
        List<GameObject> column = new List<GameObject>();

        if (normal == Vector3.right || normal == Vector3.left)
        {
            for (int i = index / SizeRubiksCube * SizeRubiksCube; i < m_cubes.Count; i++)
            {
                if (i >= index / SizeRubiksCube * SizeRubiksCube + SizeRubiksCube)
                    return column;
                column.Add(m_cubes[i]);
            }
            return column;
        }
        
        for (int i = index % m_heigth; i < m_cubes.Count; i += m_heigth)
            column.Add(m_cubes[i]);

        return column;
    }

    protected List<GameObject> GetRow(int index, Vector3 normal)
    {
        List<GameObject> row = new List<GameObject>();

        if (normal == Vector3.up || normal == Vector3.down)
        {
            for (int i = index / SizeRubiksCube * SizeRubiksCube; i < m_cubes.Count; i++)
            {
                if (i >= index / SizeRubiksCube * SizeRubiksCube + SizeRubiksCube)
                    return row;
                row.Add(m_cubes[i]);
            }
            return row;
        }

        if (normal == Vector3.left)
        {
            int inccrements = 0;
            int indexFirstFaces = index < SizeRubiksCube ? index / m_width * m_heigth: (index % (SizeRubiksCube));
            for (int i = indexFirstFaces; i < m_cubes.Count; i++)
            {
                if (i % m_width == 0 && i != indexFirstFaces && i + SizeRubiksCube - m_width < m_cubes.Count)
                    i += SizeRubiksCube - m_width;

                inccrements += 1;
            
                if (inccrements > m_depth * m_width)
                    return row;
            
                row.Add(m_cubes[i]);
            }
            return row;
        }
        
        if (normal == Vector3.forward)
        {
            int indexFace = index % SizeRubiksCube;
            if (indexFace % m_width != 0)
                indexFace -= indexFace % m_width;
            
            int inc = 0;
            for (int i = indexFace; i < m_cubes.Count; i++)
            {
                if (i % m_width == 0 && i != indexFace && i + SizeRubiksCube - m_width < m_cubes.Count)
                    i += SizeRubiksCube - m_width;

                inc += 1;
            
                if (inc > m_depth * m_width)
                    return row;
            
                row.Add(m_cubes[i]);
            }

            return row;
        }
        
        int increment = 0;
        int indexFirstFace = index < SizeRubiksCube ? index / m_width * m_heigth: (index % (SizeRubiksCube) - 1) / 
        m_width * m_width;
        for (int i = indexFirstFace; i < m_cubes.Count; i++)
        {
            if (i % m_width == 0 && i != indexFirstFace && i + SizeRubiksCube - m_width < m_cubes.Count)
                i += SizeRubiksCube - m_width;

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

    protected Plane GetSelectedPlane()
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

    protected void RefreachPrescisionCube()
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

    protected void RefreachPrecisionPlane()
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
    
    protected List<GameObject> GetSelectedCubeWithPlane(Plane plane)
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
    
    protected void DisableAllGrayFace()
    {
        m_NeutralPlaneRubix1.gameObject.SetActive(false);
        m_NeutralPlaneRubix2.gameObject.SetActive(false);
        m_NeutralPlaneSlice1.gameObject.SetActive(false);
        m_NeutralPlaneSlice2.gameObject.SetActive(false);
    }
    
    protected void ShowNeutralFace(bool flag)
    {
        m_NeutralPlaneRubix1.GetComponent<MeshRenderer>().enabled =
            m_NeutralPlaneRubix2.GetComponent<MeshRenderer>().enabled =
                m_NeutralPlaneSlice1.GetComponent<MeshRenderer>().enabled =
                    m_NeutralPlaneSlice2.GetComponent<MeshRenderer>().enabled = flag;
    }

    protected IEnumerator MultipleSliceRotationSequence(List<GameObject> slice, float rotation, float angularSpeedInDegBySec)
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
        if (!isCoroutineRun())
        {
            m_solveCoroutine = StartCoroutine(SolveCorroutine());
        }
    }

    protected IEnumerator SolveCorroutine()
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
            if (Approximately(angleToDo, 0f, Mathf.Epsilon))
                continue;
            
            //Avoid unecessary rotation the fell that solve copy all movement
            float shortestAngle;
            if (angleToDo > 180f)
                shortestAngle = angleToDo - 360f;
            else if (angleToDo < -180f)
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
        if (!isCoroutineRun())
        {
            m_shuffleCoroutine = StartCoroutine(ShuffleCorroutine((int) depth));
        }
    }

    protected virtual bool isCoroutineRun()
    {
        return m_shuffleCoroutine != null || m_solveCoroutine != null;
    }

    protected IEnumerator ShuffleCorroutine(int depth)
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

    public void StartInfinitRotation()
    {
        StartCoroutine(InfinitRotateCoroutine(Vector3.one, 30f));
    }

    protected IEnumerator InfinitRotateCoroutine(Vector3 axis, float rotationSpeedInDeg)
    {
        do
        {
            transform.rotation = Quaternion.AngleAxis(rotationSpeedInDeg * Time.deltaTime, axis) * transform.rotation;
            yield return null;
            
        } while (true);
    }

    public void ChangeRubixSkin()
    {
        m_currentSkin++;

        if (m_currentSkin >= m_skins.Count)
            m_currentSkin = 0;
        
        ChangeRubixSkin(m_currentSkin);
    }

    public void ChangeRubixSkin(int newId)
    {
        m_currentSkin = Mathf.Clamp(newId, 0, m_skins.Count);

        foreach (var cube in m_cubes)
        {
            for (int i = 0; i < cube.transform.childCount; i++)
            {
                //Can be improve but fast to write it
                switch (cube.transform.GetChild(i).tag)
                {
                    case "X":
                        cube.transform.GetChild(i).GetComponent<MeshRenderer>().material = m_skins[m_currentSkin].x;
                        break;

                    case "-X":
                        cube.transform.GetChild(i).GetComponent<MeshRenderer>().material = m_skins[m_currentSkin].minX;
                        break;

                    case "Y":
                        cube.transform.GetChild(i).GetComponent<MeshRenderer>().material = m_skins[m_currentSkin].y;
                        break;

                    case "-Y":
                        cube.transform.GetChild(i).GetComponent<MeshRenderer>().material = m_skins[m_currentSkin].minY;
                        break;

                    case "Z":
                        cube.transform.GetChild(i).GetComponent<MeshRenderer>().material = m_skins[m_currentSkin].z;
                        break;

                    case "-Z":
                        cube.transform.GetChild(i).GetComponent<MeshRenderer>().material = m_skins[m_currentSkin].minZ;
                        break;
                }
            }
        }

        if (m_skins[m_currentSkin].neutral != null)
        {
            if (!m_NeutralPlaneRubix1.GetComponent<MeshRenderer>().enabled)
            {
                ShowNeutralFace(true);
            }
            
            m_NeutralPlaneRubix1.GetComponent<MeshRenderer>().material =
                m_NeutralPlaneRubix2.GetComponent<MeshRenderer>().material =
                    m_NeutralPlaneSlice1.GetComponent<MeshRenderer>().material =
                        m_NeutralPlaneSlice2.GetComponent<MeshRenderer>().material = m_skins[m_currentSkin].neutral;
        }
        else if (m_NeutralPlaneRubix1.GetComponent<MeshRenderer>().enabled)
        {
            ShowNeutralFace(false);
        }
    }
}