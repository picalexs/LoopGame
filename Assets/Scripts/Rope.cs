using System.Collections.Generic;
using UnityEngine;

public class Rope : MonoBehaviour
{
    [Header("Rope")]
    [SerializeField] private int _nrOfSegments;
    [SerializeField] private float _segmentLength;

    [System.NonSerialized]
    public List<RopeSegment> ropeSegments = new List<RopeSegment>();
    public LineRenderer _lineRenderer;

    public void Init(int nrOfSegments, float segmentLength)
    {
        _nrOfSegments = nrOfSegments;
        _segmentLength = segmentLength;

        _lineRenderer = GetComponent<LineRenderer>();
        if (_lineRenderer == null)
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        _lineRenderer.positionCount = _nrOfSegments;
        _lineRenderer.loop = false;
    }

    public void Reset()
    {
        ropeSegments.Clear();
        _lineRenderer.positionCount = 0;
        _lineRenderer.loop = false;
    }
    private void Update()
    {
        RenderRope();
    }

    private void RenderRope()
    {
        if (ropeSegments.Count == 0)
            return;

        _lineRenderer.positionCount = ropeSegments.Count;

        for (int i = 0; i < ropeSegments.Count; i++)
        {
            _lineRenderer.SetPosition(i, ropeSegments[i].position);
        }
    }

    public float GetSegmentLength()
    {
        return _segmentLength;
    }

    public void SetSegmentLength(float newSegmentLength)
    {
        _segmentLength = newSegmentLength;
    }
}
