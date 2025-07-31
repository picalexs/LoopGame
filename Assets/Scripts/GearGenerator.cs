using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralGear : MonoBehaviour
{
    public int numberOfTeeth = 20;
    public float radius = 1f;
    public float toothDepth = 0.3f;

    void Start()
    {
        GenerateGear();
    }

    void GenerateGear()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Gear";
        int vertIndex = 0;
        int triIndex = 0;
        float angleStep = 2 * Mathf.PI / numberOfTeeth;
        int baseVertices = numberOfTeeth * 3;
        int extraVertices = (numberOfTeeth / 2) * 4;
        int totalVertices = baseVertices + extraVertices;

        int baseTriangles = numberOfTeeth;
        int extraTriangles = (numberOfTeeth / 2) * 2;
        int totalTriangles = (baseTriangles + extraTriangles) * 3;

        Vector3[] vertices = new Vector3[totalVertices];
        int[] triangles = new int[totalTriangles];

        for (int i = 0; i < numberOfTeeth; i++)
        {
            float angle = i * angleStep;
            float nextAngle = (i + 1) * angleStep;
            
            Vector3 p1 = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Vector3 p2 = new Vector3(Mathf.Cos(nextAngle) * radius, Mathf.Sin(nextAngle) * radius, 0);

            vertices[vertIndex] = p1;
            vertices[vertIndex + 1] = p2;
            vertices[vertIndex + 2] = Vector3.zero;

            triangles[triIndex] = vertIndex + 2;
            triangles[triIndex + 1] = vertIndex;
            triangles[triIndex + 2] = vertIndex + 1;

            vertIndex += 3;
            triIndex += 3;

            if (i % 2 == 0)
            {
                Vector3 mid = (p1 + p2) * 0.5f;
                Vector3 normal = mid.normalized;
                Debug.Log(normal);
                Vector3 q1 = p1 + normal * toothDepth;
                Vector3 q2 = p2 + normal * toothDepth;

                //GameObject.CreatePrimitive(PrimitiveType.Sphere).transform.position = q1;
                //GameObject.CreatePrimitive(PrimitiveType.Sphere).transform.position = q2;

                vertices[vertIndex] = p1;
                vertices[vertIndex + 1] =p2;
                vertices[vertIndex + 2] = q2;
                vertices[vertIndex + 3] = q1;

                triangles[triIndex] = vertIndex;
                triangles[triIndex + 1] = vertIndex+2;
                triangles[triIndex + 2] = vertIndex+1;

                triangles[triIndex + 3] = vertIndex;
                triangles[triIndex + 4] = vertIndex+3;
                triangles[triIndex + 5] = vertIndex+2;

                vertIndex += 4;
                triIndex += 6;
            }

        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GetComponent<MeshFilter>().mesh = mesh;

    }
}
