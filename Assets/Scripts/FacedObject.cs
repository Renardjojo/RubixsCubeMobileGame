using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FacedObject : MonoBehaviour
{
    private MeshRenderer m_meshRenderer;
    private BoxCollider m_boxCollider;

    private List<List<List<int>>> m_facesIndex;
    //private int m_rowCount;
    //private int m_columCount;
    //private int m_depthCount;

    public int RowCount
    {
        get
        {
            return m_facesIndex.Count;
        }
    }

    public int ColumnCount
    {
        get
        {
            return m_facesIndex[0].Count;
        }
    }
    
    public int DepthCount
    {
        get
        {
            return m_facesIndex[0][0].Count;
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        m_meshRenderer = GetComponent<MeshRenderer>();
        m_boxCollider = GetComponent<BoxCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            
        }
    }

    void CreateFacedObjectAndSplitFace()
    {
        
    }

    int GetFaceIndexWithPosition(Vector2 Position)
    {
        return 0;
    }

    List<int> GetLineOfFaceIndex(int FaceIndex)
    {
        return null;
    }
    
    List<int> GetRowOfFaceIndex(int FaceIndex)
    {
        return null;
    }

    void SplitFacedObject()
    {
        
    }
    
    void MergeFacedObject()
    {
        
    }
}
