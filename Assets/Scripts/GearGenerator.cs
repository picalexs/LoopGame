using UnityEngine;

public class GearGenerator : MonoBehaviour
{
    public int sides = 10;
    public int sideLength = 2f;
    public Vector3 center = Vector3.zero;
    private LineRenderer lineRenderer;
    void Start()
    {
        lineRenderer = GetComponent<lineRenderer>();
        DrawPolygon(sides, radius);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if(sides < 20)
            {
                sides++;
            }
            DrawPolygon(sides);
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (sides > 3)
            {
                sides--;
            }
            DrawPolygon(sides);
        }
    }
    void DrawPolygon(int sides)
    {
        float radius = sideLength / (2 * Mathf.Sin(Mathf.PI * sides));
        lineRenderer.positionCount = sides + 1;
        for (int i = 0; i < sides; i++) {
            float angel = (2 * Mathf.PI * i) / sides;
            float x = radius * Mathf.Cos(angel) + center.x;
            float y = radius * Mathf.Sin(angel) + center.y;
            lineRenderer.SetPosition(i, new Vector3(x, y, center.z));
        }
    }

}
