using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ChainTensionCalculator : MonoBehaviour
{
    [Header("Path Calculation")]
    [SerializeField] private LayerMask _obstacleLayer = -1;
    [SerializeField] private float _wrapDistance = 0.1f;
    [SerializeField] private int _pathResolution = 100;
    [SerializeField] private bool _debugVisualization = true;

    private List<Vector2> _optimalPath = new List<Vector2>();
    private List<Collider2D> _wrappedObjects = new List<Collider2D>();
    private List<Vector2> _originalRopePoints = new List<Vector2>();

    public List<Vector2> CalculateOptimalWrappedPath(List<RopeSegment> ropeSegments)
    {
        if (ropeSegments.Count < 3) return new List<Vector2>();

        _originalRopePoints.Clear();
        foreach (var segment in ropeSegments)
        {
            _originalRopePoints.Add(segment.position);
        }

        _wrappedObjects.Clear();
        _optimalPath.Clear();

        FindObjectsInsideRope(ropeSegments);

        if (_wrappedObjects.Count == 0)
        {
            return CreateSimpleLoop(_originalRopePoints);
        }

        return CalculateConvexHullWithWrapping();
    }

    private void FindObjectsInsideRope(List<RopeSegment> ropeSegments)
    {
        Vector2[] ropePoints = ropeSegments.Select(s => s.position).ToArray();

        Bounds ropeBounds = GetRopeBounds(ropePoints);
        Collider2D[] allColliders = Physics2D.OverlapAreaAll(ropeBounds.min, ropeBounds.max, _obstacleLayer);

        foreach (var collider in allColliders)
        {
            Vector2 objCenter = collider.transform.position;
            if (IsPointInsidePolygon(objCenter, ropePoints))
            {
                _wrappedObjects.Add(collider);
            }
        }
    }

    private Bounds GetRopeBounds(Vector2[] points)
    {
        if (points.Length == 0) return new Bounds();

        Vector2 min = points[0];
        Vector2 max = points[0];

        foreach (var point in points)
        {
            min = Vector2.Min(min, point);
            max = Vector2.Max(max, point);
        }

        return new Bounds((min + max) * 0.5f, max - min);
    }

    private bool IsPointInsidePolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        int j = polygon.Length - 1;

        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];

            if (((pi.y > point.y) != (pj.y > point.y)) &&
                (point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x))
            {
                inside = !inside;
            }
            j = i;
        }

        return inside;
    }

    private List<Vector2> CreateSimpleLoop(List<Vector2> originalPoints)
    {
        Vector2 center = GetCentroid(originalPoints);
        float avgRadius = GetAverageDistanceFromCenter(originalPoints, center);

        List<Vector2> loop = new List<Vector2>();

        for (int i = 0; i < _pathResolution; i++)
        {
            float angle = (float)i / _pathResolution * 2f * Mathf.PI;
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * avgRadius;
            loop.Add(point);
        }

        return loop;
    }

    private List<Vector2> CalculateConvexHullWithWrapping()
    {
        List<Vector2> hullPoints = new List<Vector2>();

        foreach (var obj in _wrappedObjects)
        {
            Vector2 center = obj.transform.position;
            Bounds bounds = obj.bounds;
            float radius = Mathf.Max(bounds.size.x, bounds.size.y) * 0.5f + _wrapDistance;

            int pointsPerObject = Mathf.Max(8, _pathResolution / _wrappedObjects.Count);

            for (int i = 0; i < pointsPerObject; i++)
            {
                float angle = (float)i / pointsPerObject * 2f * Mathf.PI;
                Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                hullPoints.Add(point);
            }
        }

        if (hullPoints.Count < 3) return CreateSimpleLoop(_originalRopePoints);

        return CalculateConvexHull(hullPoints);
    }

    private List<Vector2> CalculateConvexHull(List<Vector2> points)
    {
        if (points.Count < 3) return points;

        points = points.OrderBy(p => p.x).ThenBy(p => p.y).ToList();

        List<Vector2> hull = new List<Vector2>();

        for (int i = 0; i < points.Count; i++)
        {
            while (hull.Count >= 2 && CrossProduct(hull[hull.Count - 2], hull[hull.Count - 1], points[i]) <= 0)
            {
                hull.RemoveAt(hull.Count - 1);
            }
            hull.Add(points[i]);
        }

        int lowerSize = hull.Count;

        for (int i = points.Count - 2; i >= 0; i--)
        {
            while (hull.Count > lowerSize && CrossProduct(hull[hull.Count - 2], hull[hull.Count - 1], points[i]) <= 0)
            {
                hull.RemoveAt(hull.Count - 1);
            }
            hull.Add(points[i]);
        }

        if (hull.Count > 1) hull.RemoveAt(hull.Count - 1);

        return SmoothHull(hull);
    }

    private float CrossProduct(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    private List<Vector2> SmoothHull(List<Vector2> hull)
    {
        if (hull.Count < 3) return hull;

        List<Vector2> smoothed = new List<Vector2>();

        for (int i = 0; i < hull.Count; i++)
        {
            Vector2 current = hull[i];
            Vector2 next = hull[(i + 1) % hull.Count];

            int subdivisions = Mathf.Max(2, (int)(Vector2.Distance(current, next) / _wrapDistance));

            for (int j = 0; j < subdivisions; j++)
            {
                float t = (float)j / subdivisions;
                smoothed.Add(Vector2.Lerp(current, next, t));
            }
        }

        return smoothed;
    }

    private Vector2 GetCentroid(List<Vector2> points)
    {
        Vector2 sum = Vector2.zero;
        foreach (var point in points)
        {
            sum += point;
        }
        return sum / points.Count;
    }

    private float GetAverageDistanceFromCenter(List<Vector2> points, Vector2 center)
    {
        float totalDistance = 0f;
        foreach (var point in points)
        {
            totalDistance += Vector2.Distance(point, center);
        }
        return totalDistance / points.Count;
    }

    public List<Vector2> GetOptimalPath()
    {
        return _optimalPath;
    }

    public void SetOptimalPath(List<Vector2> path)
    {
        _optimalPath = path;
    }

    private void OnDrawGizmos()
    {
        if (!_debugVisualization) return;

        if (_optimalPath.Count > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _optimalPath.Count; i++)
            {
                int nextIndex = (i + 1) % _optimalPath.Count;
                Gizmos.DrawLine(_optimalPath[i], _optimalPath[nextIndex]);
            }
        }

        if (_wrappedObjects.Count > 0)
        {
            Gizmos.color = Color.yellow;
            foreach (var obj in _wrappedObjects)
            {
                if (obj != null)
                {
                    Gizmos.DrawWireCube(obj.transform.position, obj.bounds.size);
                }
            }
        }
    }
}