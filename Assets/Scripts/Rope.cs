using System.Collections.Generic;
using UnityEngine;

public class Rope : MonoBehaviour
{
    [Header("Rope")]
    [SerializeField] private int _nrOfSegments;
    [SerializeField] private float _segmentLength;

    [Header("Visual Smoothing")]
    [SerializeField] private bool _enableSmoothing = true;
    [SerializeField] private int _smoothingPasses = 1;

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

        if (_enableSmoothing && ropeSegments.Count > 2)
        {
            Vector3[] smoothedPositions = new Vector3[ropeSegments.Count];

            for (int i = 0; i < ropeSegments.Count; i++)
            {
                smoothedPositions[i] = ropeSegments[i].position;
            }

            for (int pass = 0; pass < _smoothingPasses; pass++)
            {
                for (int i = 1; i < smoothedPositions.Length - 1; i++)
                {
                    smoothedPositions[i] = Vector3.Lerp(smoothedPositions[i],
                        (smoothedPositions[i - 1] + smoothedPositions[i + 1]) * 0.5f, 0.5f);
                }
            }

            _lineRenderer.SetPositions(smoothedPositions);
        }
        else
        {
            for (int i = 0; i < ropeSegments.Count; i++)
            {
                _lineRenderer.SetPosition(i, ropeSegments[i].position);
            }
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