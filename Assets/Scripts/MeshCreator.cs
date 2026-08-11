using UnityEngine;
using static UnityEngine.Mesh;

public class MeshCreator : MonoBehaviour
{
    public GameObject? CreateMesh(MeshData meshData, Material originalMaterial)
    {
        if (meshData.vertices.Count == 0)
        {
            return null;
        }
        Mesh mesh = new()
        {
            vertices = meshData.vertices.ToArray(),
            triangles = meshData.triangles.ToArray(),
            uv = meshData.uvs.ToArray(),
        };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateUVDistributionMetrics();

        // Create new mesh gameobject
        GameObject gameObject = new("Mesh", typeof(MeshFilter), typeof(MeshRenderer), typeof(Rigidbody), typeof(MeshCollider), typeof(MeshCollider));
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshRenderer.material = originalMaterial;
        //meshRenderer.material = new Material(Shader.Find("Standard"));

        Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
        //rigidbody.isKinematic = true;
        rigidbody.useGravity = true;

        MeshCollider[] meshColliders = gameObject.GetComponents<MeshCollider>();
        meshColliders[0].sharedMesh = mesh;
        meshColliders[0].convex = true;
        meshColliders[0].isTrigger = true;

        meshColliders[1].sharedMesh = mesh;
        meshColliders[1].convex = true;
        meshColliders[1].isTrigger = false;

        return gameObject;
    }
}
