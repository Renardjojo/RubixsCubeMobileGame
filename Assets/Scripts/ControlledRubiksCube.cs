using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ControlledRubiksCube : RubiksCube
{
    [Header("Lock slice setting")]
        [SerializeField] protected float m_lockSliceRotationSpeedInDegBySec = 180;
        protected Coroutine m_lockSliceCoroutine = null;
    
    [Header("Input Setting")]
        //This value will be multiplicate by the length of the cursor movement when all the rubix cube is rotate
        [SerializeField] protected float m_rubixRotInDegByPixel = 0.01f;       
        [SerializeField] protected float m_rubixSliceRotInDegByPixel = 0.01f;

        [SerializeField] protected bool m_inverseYAxis = false;
        [SerializeField] protected bool m_inverseXAxis = false;
        [SerializeField] protected float m_rangeMouseMovement = 6f;
#if UNITY_EDITOR
        [SerializeField] protected bool m_useMobileInput = false;
#endif
        
#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
        protected bool m_screenIsTouch = false;
#endif
        
        //Usefull to check the difference of position between each frame when cursor is clicked
        protected Vector2 m_lastCursorPos;
        protected ResultRayCast m_resultRayCast;
        protected float m_sliceDeltaAngle;
    
    [Header("Win Event")]
        [SerializeField] protected Vector3 m_winRotationAxis;
        [SerializeField] protected float m_winRotationSpeedInDegBySec = 30;
        [SerializeField] protected UnityEvent m_onPlayerWin;
        protected Coroutine m_winCoroutine = null;
    
#if UNITY_EDITOR
    [Header("Debug")]
        [SerializeField] protected GameObject m_toucheIndicatorDebug;
#endif
        
    protected new void Init()
    {
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
        
        base.Init();
        
        m_resultRayCast.m_isDefinited = false;
        
#if UNITY_IOS || UNITY_ANDROID || UNITY_EDITOR
        m_screenIsTouch = Input.touchCount > 0;
#endif
        
        //Init default value
        m_lastCursorPos = Vector2.zero;
        m_resultRayCast = new ResultRayCast();
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
                UnselectRubixSlice();
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

                RotateRubixCube(movement.normalized, movement.sqrMagnitude * m_rubixRotInDegByPixel);
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
    
    void UpdateSliceControl()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        //Rotated slice of rubix cube if is it selected
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

                m_sliceDeltaAngle += deltaMovementInPixel * m_rubixSliceRotInDegByPixel;

                RotateSlice(m_selectedSlice, deltaMovementInPixel * m_rubixSliceRotInDegByPixel);
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
                if (m_toucheIndicatorDebug != null)
                    m_toucheIndicatorDebug.transform.position = hit.point;
#endif
                m_resultRayCast.m_isDefinited = true;
            }
        }
    }

    void UnselectRubixSlice()
    {
        float rotationTodo = -m_sliceDeltaAngle % 90f;
        
        if (m_sliceDeltaAngle > 45f)
            rotationTodo += 90f;
        else if (m_sliceDeltaAngle < -45f)
            rotationTodo -= 90f;
 
        m_resolutionSteps.Push(new StepResolution(-(m_sliceDeltaAngle + rotationTodo), currentPlaneSelectedID));
        m_lockSliceCoroutine = StartCoroutine(SmoothSliceLockRotationCorroutineAndCheckIfWin(m_selectedSlice, rotationTodo, m_lockSliceRotationSpeedInDegBySec));

        m_sliceDeltaAngle = 0f;
        currentPlaneSelectedID = -1;
        m_resultRayCast.m_isDefinited = false;
        m_resultRayCast.m_directionTurnIsDefinited = false;
        m_resultRayCast.m_directionRowDefinited = false;
    }
    
    protected void DefinitedDirectionTurn(Vector3 direction)
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
    
    protected bool CheckIfPlayerWin()
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
    
    protected IEnumerator SmoothSliceLockRotationCorroutineAndCheckIfWin(List<GameObject> slice, float rotation, float 
        angularSpeedInDegBySec)
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

    public void WinRotateRubixCube()
    {
        if (m_winCoroutine == null)
        {
            m_winCoroutine = StartCoroutine(InfinitRotateCoroutine(m_winRotationAxis, m_winRotationSpeedInDegBySec));
        }
    }
    
    protected new bool isCoroutineRun()
    {
        return base.isCoroutineRun() || m_lockSliceCoroutine != null;
    }
}
