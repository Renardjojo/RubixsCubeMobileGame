using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Camera))]
public class CameraScaler : MonoBehaviour
{
    [SerializeField] private float m_maxZoom = 5f;
    private float m_currentZoom = 0f;
    [SerializeField] private float m_baseDistanceScale = 4f;
    private float m_baseDistance = 0f;
    private float m_currentDistanceScale = 0f;
    private bool m_enableScale = false;
    
    private Camera m_camera;

#if !UNITY_STANDALONE
    protected float m_baseDistanceBetweenBothFingers = -1f;
    private bool fingerDistanceIsInit = false;
    [SerializeField] private float m_zoomSensibilityFingerScale = 0.001f;
#else
    [SerializeField] private float m_zoomSensibilityMouseRool = 0.1f;
#endif
    
    
    
    // Start is called before the first frame update
    void Awake()
    {
        m_camera = GetComponent<Camera>();
    }

    void Start()
    {
        m_baseDistance = transform.position.z;
        m_currentDistanceScale = m_baseDistanceScale;
    }
    
    public void SetDistance(float newScale)
    {
        m_currentDistanceScale = newScale;
        m_enableScale = true;
    }
    
    void Update()
    {
        if (m_enableScale)
        {
            float goalDistance = m_baseDistance * m_currentDistanceScale / m_baseDistanceScale;

            if (Mathf.Abs(transform.position.z - goalDistance) < 0.01f)
            {
                transform.position = new Vector3(transform.position.x,
                    transform.position.y, goalDistance);
                m_enableScale = false;
            }
            else
            {
                transform.position = new Vector3(transform.position.x,
                    transform.position.y,
                    Mathf.Lerp(transform.position.z, goalDistance, Time.deltaTime));
            }
        }
        
#if UNITY_STANDALONE
        if (Input.mouseScrollDelta.y != 0)
        {
            float deltaZoom = -Input.mouseScrollDelta.y * m_zoomSensibilityMouseRool;
#else
        if (Input.touchCount == 2)
        {
            if (!fingerDistanceIsInit)
            {
                fingerDistanceIsInit = true;
                m_baseDistanceBetweenBothFingers = (Input.GetTouch(0).position - Input.GetTouch(1).position).magnitude;
                return;
            }
            
            float newFingerDist = (Input.GetTouch(0).position - Input.GetTouch(1).position).magnitude;
            float deltaZoom = (m_baseDistanceBetweenBothFingers - newFingerDist) * m_zoomSensibilityFingerScale;
            m_baseDistanceBetweenBothFingers = newFingerDist;

            if (deltaZoom < Mathf.Epsilon && deltaZoom > -Mathf.Epsilon )
                return;
            

#endif
            if (Mathf.Abs(m_currentZoom + deltaZoom) > m_maxZoom)
            {
                float zoomToDo = 0f;
                if (m_currentZoom + deltaZoom < 0)
                {
                    zoomToDo = -m_currentZoom - m_maxZoom;
                    m_currentZoom = -m_maxZoom;
                    
                }
                else
                {
                    zoomToDo = m_maxZoom - m_currentZoom;
                    m_currentZoom = m_maxZoom;
                }
                Debug.Log(zoomToDo + " " + deltaZoom);
                SetDistance(m_currentDistanceScale + zoomToDo);
            }
            else
            {
                m_currentZoom += deltaZoom;
                SetDistance(m_currentDistanceScale + deltaZoom);
            }
        }
#if !UNITY_STANDALONE
        else
        {
            fingerDistanceIsInit = false;
        }
#endif
    }
}