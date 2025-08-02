using UnityEngine;

public class RopeSegment
{
    public Vector2 position;
    public Vector2 previousPosition;
    public RopeSegment(Vector2 position)
    {
        this.position = position;
        this.previousPosition = position;
    }
}
