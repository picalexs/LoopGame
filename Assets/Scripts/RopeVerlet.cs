using JetBrains.Annotations;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RopeVerlet : MonoBehaviour
{
    [Header("Rope")]
    [SerializeField] private int _nrOfSegments = 50;
    [SerializeField] private float _segmentLength = 0.225f;

    [Header("Physics")]
    [SerializeField] private Vector2 _gravityForce = new Vector2(0f, -2f);
    [SerializeField] private float _damping = 0.98f;
    [SerializeField] private LayerMask _collisionMask;
    [SerializeField] private float _collisionRadius = 0.1f;
    [SerializeField] private float _bounceFactor = 0.9f;

    [Header("Constraints")]
    [SerializeField] private int _nrOfConstraintRuns = 50;

    [Header("Optimizations")]
    [Min(1)]
    [SerializeField] private int _collisionCheckFrequency = 3;

    private LineRenderer _lineRenderer;
    private List<RopeSegment> _ropeSegments = new List<RopeSegment>();


    [Header("Draw Line")]
    [Min(0.1f)]
    [SerializeField] private float _minSpacingDistance = 0.5f;

    private bool _isLeftClicking = false;

    public struct RopeSegment
    {
        public Vector2 position;
        public Vector2 previousPosition;
        public RopeSegment(Vector2 position)
        {
            this.position = position;
            this.previousPosition = position;
        }
    }

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        DrawRope();
        RenderRope();
    }

    private void FixedUpdate()
    {
        Simulate();

        for (int i = 0; i < _nrOfConstraintRuns; i++)
        {
            ApplyConstraints();

            if (i % _collisionCheckFrequency == 0)
            {
                HandleCollisions();
            }
        }
    }

    private void ResetRope()
    {
        _ropeSegments.Clear();
        _nrOfSegments = 0;
        _lineRenderer.positionCount = 0;
    }

    private void DrawRope()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        if (!Mouse.current.leftButton.isPressed)
        {
            if (_isLeftClicking)
            {
                AddSegment(_ropeSegments[0].position);
                _lineRenderer.loop = true;
            }
            _isLeftClicking = false;
            return;
        }

        _isLeftClicking = true;
        if (_ropeSegments.Count == 0)
        {
            _ropeSegments.Add(new RopeSegment(mousePosition));
            Debug.Log("First segment added at: " + mousePosition);
        }
        else
        {
            AddSegment(mousePosition);
        }
    }

    private void AddSegment(Vector2 newPosition)
    {
        RopeSegment lastSegment = _ropeSegments[_ropeSegments.Count - 1];
        Vector2 lastPos = lastSegment.position;
        float distance = Vector2.Distance(lastPos, newPosition);

        if (distance >= _minSpacingDistance)
        {
            int segmentsToAdd = Mathf.FloorToInt(distance / _minSpacingDistance);
            Vector2 direction = (newPosition - lastPos).normalized;

            for (int i = 1; i <= segmentsToAdd; i++)
            {
                Vector2 newPoint = lastPos + direction * (_minSpacingDistance * i);
                _ropeSegments.Add(new RopeSegment(newPoint));
                Debug.Log("Segment added at: " + newPoint);
            }
        }
    }


    private void RenderRope()
    {
        if (_ropeSegments.Count == 0)
            return;

        _lineRenderer.positionCount = _ropeSegments.Count;

        for (int i = 0; i < _ropeSegments.Count; i++)
        {
            _lineRenderer.SetPosition(i, _ropeSegments[i].position);
        }
    }


    private void Simulate()
    {
        for (int i = 0; i < _ropeSegments.Count; i++)
        {
            RopeSegment segment = _ropeSegments[i];
            Vector2 velocity = (segment.position - segment.previousPosition) * _damping;

            segment.previousPosition = segment.position;
            segment.position += velocity;
            segment.position += _gravityForce * Time.fixedDeltaTime;
            _ropeSegments[i] = segment;
        }
    }

    private void ApplyConstraints()
    {
        if (_ropeSegments.Count == 0)
        {
            return;
        }

        RopeSegment firstSegment = _ropeSegments[0];
        _ropeSegments[0] = firstSegment;

        for (int i = 0; i < _ropeSegments.Count - 1; i++)
        {
            RopeSegment currentSegment = _ropeSegments[i];
            RopeSegment nextSegment = _ropeSegments[i + 1];

            float distance = (currentSegment.position - nextSegment.position).magnitude;
            float difference = distance - _segmentLength;

            Vector2 changeDir = (currentSegment.position - nextSegment.position).normalized;
            Vector2 changeVector = changeDir * difference;

            if (i != 0)
            {
                currentSegment.position -= changeVector * 0.5f;
                nextSegment.position += changeVector * 0.5f;
            }
            else
            {
                nextSegment.position += changeVector;
            }

            _ropeSegments[i] = currentSegment;
            _ropeSegments[i + 1] = nextSegment;
        }
    }

    private void HandleCollisions()
    {
        for (int i = 0; i < _ropeSegments.Count; i++)
        {
            RopeSegment segment = _ropeSegments[i];
            Vector2 velocity = segment.position - segment.previousPosition;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(segment.position, _collisionRadius, _collisionMask);

            foreach (Collider2D collider in colliders)
            {
                Vector2 closestPoint = collider.ClosestPoint(segment.position);
                float distance = Vector2.Distance(segment.position, closestPoint);

                if (distance < _collisionRadius)
                {
                    Vector2 normal = (segment.position - closestPoint).normalized;
                    if (normal == Vector2.zero)
                    {
                        normal = (segment.position - (Vector2)collider.transform.position).normalized;
                    }

                    float depth = _collisionRadius - distance;
                    segment.position += normal * depth;

                    velocity = Vector2.Reflect(velocity, normal) * _bounceFactor;
                }
            }

            segment.previousPosition = segment.position - velocity;
            _ropeSegments[i] = segment;
        }
    }
}
