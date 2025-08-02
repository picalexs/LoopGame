using System.Collections.Generic;
using UnityEngine;

public class RopeVerlet : MonoBehaviour
{
    [Header("Rope")]
    [SerializeField] private float _segmentLength;

    [Header("Physics")]
    [SerializeField] private Vector2 _gravityForce = new Vector2(0f, 0f);
    [SerializeField] private float _damping = 0.98f;
    [SerializeField] private LayerMask _collisionMask;
    [SerializeField] private float _collisionRadius = 0.125f;
    [SerializeField] private float _bounceFactor = 0.5f;

    [Header("Constraints")]
    [SerializeField] private int _nrOfConstraintRuns = 30;
    [SerializeField] private int _nrOfConstraintRunsHigh = 60;

    [Header("Optimizations")]
    [Min(1)]
    [SerializeField] private int _collisionCheckFrequency = 2;
    [Min(1)]
    [SerializeField] private int _collisionCheckFrequencyHigh = 1;

    [Header("Player interaction")]
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private float _playerProximityRadius = 5f;

    [Header("Rope Behavior")]
    [SerializeField] private bool _pinFirstPoint = false;
    [SerializeField] private float _centripetalForce = 0f;

    [Header("Collision Enhancement")]
    [SerializeField] private float _collisionBuffer = 0.02f;
    [SerializeField] private float _frictionForce = 0.3f;
    [SerializeField] private float _collisionDamping = 0.6f;

    [Header("Geometric Tension")]
    [SerializeField] private bool _useGeometricTension = false;

    private Rope _rope;
    private ContactFilter2D _collisionFilter;
    private List<Collider2D> _colliders = new List<Collider2D>(32);
    private Vector2 _ropeCenter;
    private bool _isShrinking = false;

    private void Awake()
    {
        _rope = GetComponent<Rope>();
        if (_rope != null)
        {
            _segmentLength = _rope.GetSegmentLength();
        }

        UpdateCollisionFilter();
    }
    public void SetGeometricTensionMode(bool useGeometric)
    {
        _useGeometricTension = useGeometric;
    }

    public void SetPhysicsProperties(Vector2 gravityForce, float damping, LayerMask collisionMask, float collisionRadius, float bounceFactor)
    {
        _gravityForce = gravityForce;
        _damping = damping;
        _collisionMask = collisionMask;
        _collisionRadius = collisionRadius;
        _bounceFactor = bounceFactor;
        UpdateCollisionFilter();
    }

    public void SetConstraintProperties(int constraintRuns, int constraintRunsHigh)
    {
        _nrOfConstraintRuns = constraintRuns;
        _nrOfConstraintRunsHigh = constraintRunsHigh;
    }

    public void SetOptimizationProperties(int collisionCheckFreq, int collisionCheckFreqHigh)
    {
        _collisionCheckFrequency = collisionCheckFreq;
        _collisionCheckFrequencyHigh = collisionCheckFreqHigh;
    }

    public void SetPlayerInteractionProperties(LayerMask playerLayer, float proximityRadius)
    {
        _playerLayer = playerLayer;
        _playerProximityRadius = proximityRadius;
    }

    public void SetBehaviorProperties(bool pinFirstPoint, float centripetalForce)
    {
        _pinFirstPoint = pinFirstPoint;
        _centripetalForce = centripetalForce;
    }

    private void UpdateCollisionFilter()
    {
        _collisionFilter = new ContactFilter2D();
        _collisionFilter.SetLayerMask(_collisionMask);
        _collisionFilter.useTriggers = false;
    }

    public void UpdateSegmentLength(float newLength)
    {
        _segmentLength = newLength;
    }

    public void SetSegmentLength(float newLength)
    {
        _segmentLength = newLength;
    }

    public void SetPinFirstPoint(bool pinFirst)
    {
        _pinFirstPoint = pinFirst;
    }

    public void SetCentripetalForce(float force)
    {
        _centripetalForce = force;
        _isShrinking = force > 0f;
    }

    public void SetShrinkingMode(bool shrinking)
    {
        _isShrinking = shrinking;
    }

    private void FixedUpdate()
    {
        CalculateRopeCenter();
        Simulate();
        bool playerNearby = _rope.ropeSegments.Count > 0
            && Physics2D.OverlapCircle(_rope.ropeSegments[0].position, _playerProximityRadius, _playerLayer) != null;
        int runs = playerNearby ? _nrOfConstraintRunsHigh : _nrOfConstraintRuns;
        int freq = playerNearby ? _collisionCheckFrequencyHigh : _collisionCheckFrequency;

        if (_isShrinking)
        {
            runs = Mathf.Max(runs, _nrOfConstraintRuns + 10);
            freq = Mathf.Max(freq - 1, 1);
        }

        for (int i = 0; i < runs; i++)
        {
            ApplyConstraints();
            if (i % freq == 0) HandleCollisions();
        }
    }

    private void CalculateRopeCenter()
    {
        if (_rope.ropeSegments.Count == 0) return;

        Vector2 center = Vector2.zero;
        foreach (var segment in _rope.ropeSegments)
        {
            center += segment.position;
        }
        _ropeCenter = center / _rope.ropeSegments.Count;
    }

    private void Simulate()
    {
        for (int i = 0; i < _rope.ropeSegments.Count; i++)
        {
            RopeSegment s = _rope.ropeSegments[i];
            Vector2 vel = (s.position - s.previousPosition) * _damping;
            s.previousPosition = s.position;

            Vector2 totalForce = _gravityForce;

            if (_centripetalForce > 0f && !_useGeometricTension)
            {
                Vector2 toCenter = (_ropeCenter - s.position);
                if (toCenter.magnitude > 0.01f)
                {
                    totalForce += toCenter.normalized * _centripetalForce;
                }
            }

            s.position += vel + totalForce * Time.fixedDeltaTime;
            _rope.ropeSegments[i] = s;
        }
    }

    private void ApplyConstraints()
    {
        if (_rope.ropeSegments.Count < 2) return;

        for (int i = 0; i < _rope.ropeSegments.Count - 1; i++)
        {
            RopeSegment a = _rope.ropeSegments[i];
            RopeSegment b = _rope.ropeSegments[i + 1];
            Vector2 delta = a.position - b.position;
            float dist = delta.magnitude;
            float diff = dist - _segmentLength;

            if (dist > 0f)
            {
                Vector2 n = delta / dist;
                Vector2 move = n * (diff * 0.5f);

                if (_pinFirstPoint && i == 0)
                {
                    b.position += n * diff;
                }
                else
                {
                    a.position -= move;
                    b.position += move;
                }
            }

            _rope.ropeSegments[i] = a;
            _rope.ropeSegments[i + 1] = b;
        }
    }

    private void HandleCollisions()
    {
        int count = _rope.ropeSegments.Count;
        for (int i = 0; i < count; i++)
        {
            RopeSegment s = _rope.ropeSegments[i];
            Vector2 vel = s.position - s.previousPosition;

            _colliders.Clear();
            int hits = Physics2D.OverlapCircle(s.position, _collisionRadius, _collisionFilter, _colliders);

            for (int j = 0; j < hits; j++)
            {
                Collider2D c = _colliders[j];
                Vector2 cp = c.ClosestPoint(s.position);
                float d = Vector2.Distance(s.position, cp);

                if (d < _collisionRadius)
                {
                    Vector2 n = (s.position - cp).normalized;
                    if (n == Vector2.zero)
                        n = (s.position - (Vector2)c.transform.position).normalized;

                    float penetration = _collisionRadius - d;

                    float pushOut = penetration + (_isShrinking ? _collisionBuffer * 1.5f : _collisionBuffer);
                    s.position += n * pushOut;

                    vel = Vector2.Reflect(vel, n) * _bounceFactor;

                    float dampingMultiplier = _isShrinking ? _collisionDamping * 0.8f : _collisionDamping;
                    vel *= dampingMultiplier;

                    Vector2 tangent = Vector2.Perpendicular(n);
                    float tangentVel = Vector2.Dot(vel, tangent);
                    vel -= tangent * (tangentVel * _frictionForce);
                }
            }

            s.previousPosition = s.position - vel;
            _rope.ropeSegments[i] = s;
        }
    }

    public bool HasExcessiveTension(float tensionThreshold = 2f)
    {
        if (_rope.ropeSegments.Count < 3) return false;

        for (int i = 1; i < _rope.ropeSegments.Count - 1; i++)
        {
            Vector2 prev = _rope.ropeSegments[i - 1].position;
            Vector2 current = _rope.ropeSegments[i].position;
            Vector2 next = _rope.ropeSegments[i + 1].position;

            Vector2 toPrev = (prev - current).normalized;
            Vector2 toNext = (next - current).normalized;
            float tension = -Vector2.Dot(toPrev, toNext);

            if (tension > tensionThreshold)
            {
                return true;
            }
        }
        return false;
    }

    public float GetTotalRopeLength()
    {
        if (_rope.ropeSegments.Count < 2) return 0f;

        float totalLength = 0f;
        for (int i = 0; i < _rope.ropeSegments.Count - 1; i++)
        {
            totalLength += Vector2.Distance(_rope.ropeSegments[i].position, _rope.ropeSegments[i + 1].position);
        }
        return totalLength;
    }

    public bool HasTooCloseSegments(float minDistance = 0.05f)
    {
        for (int i = 0; i < _rope.ropeSegments.Count - 1; i++)
        {
            float distance = Vector2.Distance(_rope.ropeSegments[i].position, _rope.ropeSegments[i + 1].position);
            if (distance < minDistance)
            {
                return true;
            }
        }
        return false;
    }

    public void SmoothRopeSegments(float smoothingFactor = 0.5f)
    {
        if (_rope.ropeSegments.Count < 3) return;

        List<RopeSegment> smoothedSegments = new List<RopeSegment>(_rope.ropeSegments);

        for (int i = 1; i < smoothedSegments.Count - 1; i++)
        {
            Vector2 prev = _rope.ropeSegments[i - 1].position;
            Vector2 current = _rope.ropeSegments[i].position;
            Vector2 next = _rope.ropeSegments[i + 1].position;

            Vector2 smoothed = (prev + current * 2f + next) * 0.25f;

            float deviation = Vector2.Distance(current, smoothed);
            if (deviation < _segmentLength * 0.3f)
            {
                RopeSegment segment = smoothedSegments[i];
                segment.position = Vector2.Lerp(current, smoothed, smoothingFactor);
                smoothedSegments[i] = segment;
            }
        }

        _rope.ropeSegments = smoothedSegments;
    }
}