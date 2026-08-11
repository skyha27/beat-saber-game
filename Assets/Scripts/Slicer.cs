using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MeshData
{
    public List<Vector3> vertices;
    public List<int> triangles;
    public List<Vector2> uvs;

    public MeshData()
    {
        vertices = new List<Vector3>();
        triangles = new List<int>();
        uvs = new List<Vector2>();
    }
}

public class Slicer : MonoBehaviour
{
    public MeshCreator meshCreator;
    public GameObject _tip;
    public GameObject _base;
    // fields present to create infinite slicing plane
    Vector3 _triggerEnterTipPosition;
    Vector3 _triggerEnterBasePosition;
    Vector3 _triggerExitTipPosition;

    public MeshData positiveMesh = new();
    public MeshData negativeMesh = new();

    private List<Vector3> _intersectionPoints = new();


    void OnTriggerEnter(Collider collider)
    {
        _triggerEnterBasePosition = _base.transform.position;
        _triggerEnterTipPosition = _tip.transform.position;
    }

    void OnTriggerExit(Collider collider)
    {
        _triggerExitTipPosition = _tip.transform.position;

        // create intersection plane
        Vector3 side1 = _triggerEnterTipPosition - _triggerEnterBasePosition;
        Vector3 side2 = _triggerExitTipPosition - _triggerEnterBasePosition;
        Vector3 normal = Vector3.Cross(side1, side2).normalized;

        Vector3 planeWorldNorm = normal.normalized;
        Vector3 planeWorldPos = _triggerEnterTipPosition;

        // Create plane and set normal and pos
        Plane plane = new(planeWorldNorm, planeWorldPos);

        // split the triangles
        SplitTriangles(plane, collider.gameObject, planeWorldNorm, planeWorldPos);
    }

    void SplitTriangles(Plane plane, GameObject gameObject, Vector3 planeWorldNorm, Vector3 planeWorldPos)
    {
        var mesh = gameObject.GetComponent<MeshFilter>().mesh;

        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            int index1 = mesh.triangles[i];
            var index2 = mesh.triangles[i + 1];
            var index3 = mesh.triangles[i + 2];

            Transform transform = gameObject.transform;
            Vector3 vert1 = transform.TransformPoint(mesh.vertices[index1]);
            Vector3 vert2 = transform.TransformPoint(mesh.vertices[index2]);
            Vector3 vert3 = transform.TransformPoint(mesh.vertices[index3]);

            bool vert1side = plane.GetSide(vert1);
            bool vert2side = plane.GetSide(vert2);
            bool vert3side = plane.GetSide(vert3);

            Vector2 uv1 = mesh.uv[index1];
            Vector2 uv2 = mesh.uv[index2];
            Vector2 uv3 = mesh.uv[index3];

            Vector3 intersection1 = new();
            Vector3 intersection2 = new();

            float t1, t2;

            // case all verts are on same side
            if (vert1side == vert2side && vert2side == vert3side)
            {
                bool positiveSide = vert1side;
                // Add the single triangle
                AddTriangle(vert1, vert2, vert3, uv1, uv2, uv3, positiveSide);
            }

            // case vert 1 and 2 are on same side
            else if (vert1side == vert2side)
            {
                intersection1 = CalculateIntersectionPoint(vert2, planeWorldPos, (vert3 - vert2), planeWorldNorm, out t1);
                intersection2 = CalculateIntersectionPoint(vert1, planeWorldPos, (vert3 - vert1), planeWorldNorm, out t2);

                Vector2 iuv1 = LerpUV(uv2, uv3, t1);
                Vector2 iuv2 = LerpUV(uv1, uv3, t2);

                AddTriangle(vert1, vert2, intersection1, uv1, uv2, iuv1, vert1side);
                AddTriangle(intersection1, intersection2, vert1, iuv1, iuv2, uv1, vert1side);

                AddTriangle(intersection1, vert3, intersection2, iuv1, uv3, iuv2, !vert1side);
            }

            // case vert 2 and vert 3 are on same side
            else if (vert2side == vert3side)
            {
                intersection1 = CalculateIntersectionPoint(vert3, planeWorldPos, (vert1 - vert3), planeWorldNorm, out t1);
                intersection2 = CalculateIntersectionPoint(vert2, planeWorldPos, (vert1 - vert2), planeWorldNorm, out t2);

                Vector2 iuv1 = LerpUV(uv3, uv1, t1);
                Vector2 iuv2 = LerpUV(uv2, uv1, t2);

                AddTriangle(vert2, vert3, intersection1, uv2, uv3, iuv1, vert2side);
                AddTriangle(vert2, intersection1, intersection2, uv2, iuv1, iuv2, vert2side);

                AddTriangle(vert1, intersection2, intersection1, uv1, iuv2, iuv1, vert1side);
            }

            // case vert 3 and vert 1 are on same side
            else if (vert1side == vert3side)
            {
                intersection1 = CalculateIntersectionPoint(vert1, planeWorldPos, (vert2 - vert1), planeWorldNorm, out t1);
                intersection2 = CalculateIntersectionPoint(vert3, planeWorldPos, (vert2 - vert3), planeWorldNorm, out t2);

                Vector2 iuv1 = LerpUV(uv1, uv2, t1);
                Vector2 iuv2 = LerpUV(uv3, uv2, t2);

                AddTriangle(vert3, vert1, intersection1, uv3, uv1, iuv1, vert3side);
                AddTriangle(vert3, intersection1, intersection2, uv3, iuv1, iuv2, vert3side);

                AddTriangle(vert2, intersection2, intersection1, uv2, iuv2, iuv1, vert2side);
            }

            if (intersection1 != Vector3.zero)
                _intersectionPoints.Add(intersection1);
            if (intersection2 != Vector3.zero)
                _intersectionPoints.Add(intersection2);
        }
        // Fill the empty face of the mesh
        FillMesh(planeWorldNorm);
        // Destory the original mesh
        Destroy(gameObject);
        // Create two new meshes
        Material material = gameObject.GetComponent<MeshRenderer>().material;
        GameObject? posObject = meshCreator.CreateMesh(positiveMesh, material);
        GameObject? negObject = meshCreator.CreateMesh(negativeMesh, material);
        // Horizontal Force
        if (posObject != null && negObject != null)
        {
            posObject.GetComponent<Rigidbody>().AddForce(planeWorldNorm * 2f, ForceMode.Impulse);
            negObject.GetComponent<Rigidbody>().AddForce(-planeWorldNorm * 2f, ForceMode.Impulse);
        }
        // Clear vert and tri arrays
        positiveMesh = new();
        negativeMesh = new();
        _intersectionPoints.Clear();
    }

    /// <summary>
    /// Helper method to calculate intersection point of infinite plane and triangle side
    /// </summary>
    /// <param name="P"></param> 
    /// <param name="Q"></param>
    /// <param name="d"></param>
    /// <param name="n"></param>
    /// <returns></returns>
    Vector3 CalculateIntersectionPoint(Vector3 P, Vector3 Q, Vector3 d, Vector3 n, out float t)
    {
        float denominator = Vector3.Dot(d, n);
        // NaN check
        t = 0.01f;
        if (Mathf.Abs(denominator) < 0.001) return P;
        t = Vector3.Dot((Q - P), n) / Vector3.Dot(d, n);
        return P + t * d;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="uvA"></param>
    /// <param name="uvB"></param>
    /// <param name="t"></param>
    /// <returns></returns>
    Vector2 LerpUV(Vector2 uvA, Vector2 uvB, float t)
    {
        return Vector2.Lerp(uvA, uvB, t);
    }

    /// <summary>
    /// Adds single triangle (verticies and indexes) to the either postive and negative mesh
    /// </summary>
    void AddTriangle(Vector3 vert1, Vector3 vert2, Vector3 vert3, Vector2 uv1, Vector2 uv2, Vector2 uv3, bool positiveSide)
    {
        // Current mesh to add Triangle to
        MeshData meshData = positiveSide ? positiveMesh : negativeMesh;
        // add verts
        Vector3[] verts = new Vector3[] { vert1, vert2, vert3 };
        foreach (var vert in verts)
        {

            meshData.vertices.Add(vert);
            meshData.triangles.Add(meshData.vertices.Count - 1);
        }
        Vector2[] uvs = new Vector2[] { uv1, uv2, uv3 };
        foreach (var uv in uvs)
        {
            meshData.uvs.Add(uv);
        }
    }

    void FillMesh(Vector3 planeWorldNormal)
    {
        Vector3 center = FindCenterVertex();
        Vector2 centerUV = new(0.5f, 0.5f);
        // Remove duplicate points
        List<Vector3> uniquePoints = RemoveDuplicatePoints(_intersectionPoints);
        // Sort intersection points to enable correct triangle winding order
        Vector3 refDir = Vector3.ProjectOnPlane(Vector3.up, planeWorldNormal).normalized; // project to get the 2D "shadow" used to get y-component of angle
        Vector3 perpDir = Vector3.Cross(planeWorldNormal, refDir).normalized; // vector perpendicular (90 degrees) to reference direction; on same x,y plane axis; used to get x component of angle
        uniquePoints.Sort((a, b) =>
        {
            float angleA = Mathf.Atan2(Vector3.Dot(a - center, perpDir), Vector3.Dot(a - center, refDir));
            float angleB = Mathf.Atan2(Vector3.Dot(b - center, perpDir), Vector3.Dot(b - center, refDir));
            return angleA.CompareTo(angleB);
        });
        // find the max distance between center and all points, to find radius/scale factor of cap
        float radius = 0f;
        foreach (var p in uniquePoints)
        {
            float distance = Vector3.Distance(p, center);
            if (distance > radius)
            {
                radius = distance;
            }
        }
        // create cap triangles and uvs
        for (int i = 0; i < uniquePoints.Count; i++)
        {
            Vector3 a = uniquePoints[i];
            Vector3 b = uniquePoints[(i + 1) % uniquePoints.Count]; // modulo for wrapping (last vert connects to first vert; unit circle)

            Vector2 uvA = ProjectUV(a, center, refDir, perpDir, radius);
            Vector2 uvB = ProjectUV(b, center, refDir, perpDir, radius);

            AddTriangle(center, a, b, centerUV, uvA, uvB, false);
            AddTriangle(center, b, a, centerUV, uvB, uvA, true);
        }
    }

    Vector2 ProjectUV(Vector3 point, Vector3 center, Vector3 refDir, Vector3 perpDir, float radius)
    {
        // Project uv points onto capped faces in range [-1, 1]
        float u = Vector3.Dot(point - center, perpDir) / radius;
        float v = Vector3.Dot(point - center, refDir) / radius;
        // shift uvs to range [0, 1]
        return new Vector2(u, v) * 0.5f + new Vector2(0.5f, 0.5f);
    }

    List<Vector3> RemoveDuplicatePoints(List<Vector3> points)
    {
        List<Vector3> newList = new();
        foreach (Vector3 point in points)
        {
            // Tolerance instead of indexOf() checking bc... sure
            if (!newList.Exists(p => Vector3.Distance(p, point) < 0.001f))
            {
                newList.Add(point);
            }
        }
        return newList;
    }

    /// <summary>
    /// Orders intersection points by the angle created between each point and the center; smallest first
    /// </summary>
    /// <param name="center"></param>
    void SortPoints(Vector3 center, List<Vector3> points, Vector3 planeWorldNormal)
    {
        Vector3 refDir = (points[0] - center).normalized; // located on the cap plane x,y axis; functions as a local starting point for ordering; used to get y-component of angle
        Vector3 perpDir = Vector3.Cross(planeWorldNormal, refDir).normalized; // vector perpendicular to reference direction; on same x,y plane axis; used to get x component of angle

        points.Sort((a, b) =>
        {
            float angleA = Mathf.Atan2(Vector3.Dot(a - center, perpDir), Vector3.Dot(a - center, refDir));
            float angleB = Mathf.Atan2(Vector3.Dot(b - center, perpDir), Vector3.Dot(b - center, refDir));
            return angleA.CompareTo(angleB);
        });
    }

    Vector3 FindCenterVertex()
    {
        Vector3 center = Vector3.zero;
        foreach (Vector3 point in _intersectionPoints)
        {
            center += point;
        }
        return center / _intersectionPoints.Count;
    }
}
