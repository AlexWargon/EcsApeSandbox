
using System;
using UnityEngine;

public class CombineMeshes : MonoBehaviour
{
    [Button]
    public void Test() {
        Debug.Log("TEST");
    }
    [Button]
    public void Execute()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        int i = 0;
        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);

            i++;
        }

        Mesh mesh = new Mesh();
        mesh.CombineMeshes(combine);
        transform.GetComponent<MeshFilter>().sharedMesh = mesh;
        transform.gameObject.SetActive(true);
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class ButtonAttribute : Attribute {
    public ButtonAttribute() {
        
    }
}
