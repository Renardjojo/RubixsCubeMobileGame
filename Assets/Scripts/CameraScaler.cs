using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraScaler : MonoBehaviour
{
    [SerializeField] private float m_zoomSensibility = 1f;
    [SerializeField] private float m_maxZoom = 5f;
    private float m_currentZoom= 0f;
    [SerializeField] private float m_baseDicstanceScale = 4f;
    private float m_baseDistance = 0f;
    private float m_currentDicstanceScale = 0f;
    private bool enableScale = false;
    
    private Camera m_camera;
    
    // Start is called before the first frame update
    void Awake()
    {
        m_camera = GetComponent<Camera>();
    }

    void Start()
    {
        m_baseDistance = transform.position.z;
        m_currentDicstanceScale = m_baseDicstanceScale;
    }
    
    public void SetDistance(float newScale)
    {
        m_currentDicstanceScale = newScale;
        enableScale = true;
    }
    
    void Update()
    {
        if (enableScale)
        {
            float goolDistance = m_baseDistance * m_currentDicstanceScale / m_baseDicstanceScale;

            if (Mathf.Abs(transform.position.z - goolDistance) < 0.01f)
            {
                transform.position = new Vector3(transform.position.x,
                    transform.position.y, goolDistance);
                enableScale = false;
            }
            else
            {
                transform.position = new Vector3(transform.position.x,
                    transform.position.y,
                    Mathf.Lerp(transform.position.z, goolDistance, Time.deltaTime));
            }
        }

        if (Input.mouseScrollDelta.y != 0)
        {
            if (Mathf.Abs(m_currentZoom - Input.mouseScrollDelta.y * m_zoomSensibility) > m_maxZoom)
            {
                float zoomToDo = 0f;
                if (m_currentZoom - Input.mouseScrollDelta.y * m_zoomSensibility < 0)
                {
                    zoomToDo = -m_currentZoom - m_maxZoom;
                    m_currentZoom = -m_maxZoom;
                    
                }
                else
                {
                    zoomToDo = m_maxZoom - m_currentZoom;
                    m_currentZoom = m_maxZoom;
                }
                
                SetDistance(m_currentDicstanceScale - zoomToDo);
            }
            else
            {
                m_currentZoom -= Input.mouseScrollDelta.y * m_zoomSensibility;
                SetDistance(m_currentDicstanceScale - Input.mouseScrollDelta.y * m_zoomSensibility);
            }
        }
    }
}
