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
    [SerializeField]
    private int _collisionCheckFrequency = 2;
    [SerializeField]
    [Min(1)]
    private int _collisionCheckFrequencyHigh = 1;

    [Header("Player interaction")]
    [SerializeField] private LayerMask _playerLayer;
    [SerializeField] private float _playerProximityRadius = 5f;

    [Header("Rope Behavior")]
    [SerializeField] private bool _pinFirstPoint = false;

    private Rope _rope;
    private ContactFilter2D _collisionFilter;
    private List<Collider2D> _colliders = new List<Collider2D>(32);

    private void Awake()
    {
        _rope = GetComponent<Rope>();
        if (_rope != null)
        {
            _segmentLength = _rope.GetSegmentLength();
            _collisionMask = LayerMask.GetMask("Ground", "Player");
        }
        _collisionFilter = new ContactFilter2D();
        _collisionFilter.SetLayerMask(_collisionMask);
        _collisionFilter.useTriggers = false;
    }

    public void UpdateSegmentLength(float newLength)
    {
        _segmentLength = newLength;
    }

    public void SetPinFirstPoint(bool pinFirst)
    {
        _pinFirstPoint = pinFirst;
    }

    private void FixedUpdate()
    {
        Simulate();
        bool playerNearby = _rope.ropeSegments.Count > 0
            && Physics2D.OverlapCircle(_rope.ropeSegments[0].position, _playerProximityRadius, _playerLayer) != null;
        int runs = playerNearby ? _nrOfConstraintRunsHigh : _nrOfConstraintRuns;
        int freq = playerNearby ? _collisionCheckFrequencyHigh : _collisionCheckFrequency;

        for (int i = 0; i < runs; i++)
        {
            ApplyConstraints();
            if (i % freq == 0) HandleCollisions();
        }
    }
    private void Simulate()
    {
        for (int i = 0; i < _rope.ropeSegments.Count; i++)
        {
            RopeSegment s = _rope.ropeSegments[i];
            Vector2 vel = (s.position - s.previousPosition) * _damping;
            s.previousPosition = s.position;
            s.position += vel + _gravityForce * Time.fixedDeltaTime;
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
                    s.position += n * (penetration + 0.01f);

                    vel = Vector2.Reflect(vel, n) * _bounceFactor;
                    vel *= 0.8f;
                }
            }

            s.previousPosition = s.position - vel;
            _rope.ropeSegments[i] = s;
        }
    }
}