using HoloToolkit.Unity;
using HoloToolkit.Unity.SpatialMapping;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.WSA;

public class VertexCollector : MonoBehaviour
{    
    [SerializeField] SpatialMappingManager  manager;

    float timeStep = 0;
    float currentTime = 0;
    float elapsedTime = 0;

    private void Awake()
    {        
        manager = GetComponent<SpatialMappingManager>();
    }

    private void OnValidate()
    {
        manager = GetComponent<SpatialMappingManager>();
    }   
    
    List<Vector3> vertices = new List<Vector3>();
    List<Vector3> normals  = new List<Vector3>();

    public VerticesEvent OnVerticesCollected = new VerticesEvent();
    

    public void Collect()
    {
        vertices.Clear();
        normals.Clear();

        List<Mesh> meshes = manager.GetMeshes();

        foreach (Mesh mesh in meshes)
        {
            mesh.RecalculateNormals();
            vertices.AddRange(mesh.vertices);
            normals.AddRange(mesh.normals);
        }

        if (OnVerticesCollected != null) 
        {
            OnVerticesCollected.Invoke(string.Format("Number of vertices: {0}", vertices.Count));
        }
    }
    public List<Vector3> GetVertices()
    {
        return vertices;
    }
    public List<Vector3> GetNormals()
    {
        return normals;
    }
}

[Serializable]
public class VerticesEvent : UnityEvent<string> { }

