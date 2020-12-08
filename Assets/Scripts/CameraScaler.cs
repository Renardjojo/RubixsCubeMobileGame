using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraScaler : MonoBehaviour
{
    [SerializeField] private float m_baseDicstanceScale = 4f;
    private float m_baseDistance = 0f;
    private float m_currentDicstanceScale = 0f;
    
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
        enabled = true;
    }
    
    void Update()
    {
        float goolDistance = m_baseDistance * m_currentDicstanceScale / m_baseDicstanceScale;
        
        if (Mathf.Abs(transform.position.z - goolDistance) < 0.01f)
        {
            transform.position = new Vector3(transform.position.x,
                transform.position.y,goolDistance);
            enabled = false;
        }
        else
        {
            transform.position = new Vector3(transform.position.x,
                transform.position.y,
                Mathf.Lerp(transform.position.z, goolDistance, Time.deltaTime));
        }

    }
}
