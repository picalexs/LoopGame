using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class RopeDrawer : MonoBehaviour
{
    private InputSystem_Actions _inputSystemActions;

    [Header("Draw Line")]
    [Min(0.1f)]
    [SerializeField] private float _segmentLength = 0.5f;
    [Min(10)]
    [SerializeField] private int _minSegmentNumber = 20;

    [Header("Shrinking")]
    [SerializeField] private float _shrinkRate = 0.01f;
    [SerializeField] private float _minSegmentLength = 0.1f;
    [SerializeField] private float _shrinkDelay = 2f;
    [SerializeField] private RopeShrinkController.ShrinkMethod _shrinkMethod = RopeShrinkController.ShrinkMethod.OptimalPath;

    [Header("Shrinking Constraints")]
    [SerializeField] private float _minAllowableDistance = 0.05f;
    [SerializeField] private float _tensionThreshold = 2f;
    [SerializeField] private int _stuckFrameThreshold = 30;
    [SerializeField] private float _centripetalForce = 1f;

    [Header("Rope Physics Properties")]
    [SerializeField] private Vector2 _gravityForce = new Vector2(0f, 0f);
    [SerializeField] private float _damping = 0.98f;
    [SerializeField] private LayerMask _collisionMask = -1;
    [SerializeField] private float _collisionRadius = 0.125f;
    [SerializeField] private float _bounceFactor = 0.5f;

    [Header("Rope Constraints")]
    [SerializeField] private int _nrOfConstraintRuns = 30;
    [SerializeField] private int _nrOfConstraintRunsHigh = 60;

    [Header("Rope Optimizations")]
    [Min(1)]
    [SerializeField] private int _collisionCheckFrequency = 2;
    [Min(1)]
    [SerializeField] private int _collisionCheckFrequencyHigh = 1;

    [Header("Player Interaction")]
    [SerializeField] private LayerMask _playerLayer = 0;
    [SerializeField] private float _playerProximityRadius = 5f;

    [Header("Rope Behavior")]
    [SerializeField] private bool _pinFirstPoint = false;

    [Header("Visual")]
    [SerializeField] private Material _ropeMaterial;
    [SerializeField] private float _ropeWidth = 0.1f;
    [SerializeField] private Color _ropeStartColor = Color.white;
    [SerializeField] private Color _ropeEndColor = Color.white;

    private List<Rope> _ropes = new List<Rope>();
    private GameObject _ropeObject;
    private Rope _rope;
    private bool _isLeftClicking = false;

    private void Awake()
    {
        _inputSystemActions = new InputSystem_Actions();

        if (_ropeMaterial == null)
        {
            _ropeMaterial = new Material(Shader.Find("Sprites/Default"));
        }
    }

    private void OnEnable()
    {
        _inputSystemActions.Enable();
    }

    private void OnDisable()
    {
        _inputSystemActions.Disable();
    }

    private void Update()
    {
        DrawRope();
        ResetRope();
    }

    private void ResetRope()
    {
        if (!_inputSystemActions.Player.Reset.triggered)
        {
            return;
        }

        for (int i = _ropes.Count - 1; i >= 0; i--)
        {
            Rope rope = _ropes[i];
            rope.Reset();
            _ropes.RemoveAt(i);
            if (rope.gameObject != null)
            {
                Destroy(rope.gameObject);
            }
        }
    }

    private void DrawRope()
    {
        if (!Mouse.current.leftButton.isPressed)
        {
            if (_isLeftClicking)
            {
                if (_rope != null)
                {
                    if (_rope.ropeSegments.Count <= _minSegmentNumber)
                    {
                        Destroy(_ropeObject);
                    }
                    else if (!_ropes.Contains(_rope))
                    {
                        RopeVerlet ropeVerlet = _ropeObject.AddComponent<RopeVerlet>();
                        ConfigureRopeVerlet(ropeVerlet);

                        RopeShrinkController shrinkController = _ropeObject.AddComponent<RopeShrinkController>();
                        shrinkController.Initialize(_shrinkRate, _minSegmentLength, _shrinkDelay, _shrinkMethod,
                            _minAllowableDistance, _tensionThreshold, _stuckFrameThreshold, _centripetalForce);

                        _ropes.Add(_rope);
                        _rope._lineRenderer.loop = true;
                    }
                }
                _ropeObject = null;
                _rope = null;
            }

            _isLeftClicking = false;
            return;
        }

        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        if (!_isLeftClicking)
        {
            _ropeObject = new GameObject("Rope");
            _ropeObject.transform.position = mousePosition;
            _ropeObject.transform.rotation = Quaternion.identity;
            _rope = _ropeObject.AddComponent<Rope>();
            _rope.Init(0, _segmentLength);

            ConfigureRopeVisuals(_rope);

            _rope.ropeSegments.Add(new RopeSegment(mousePosition));
        }
        else
        {
            AddSegment(mousePosition);
        }

        _isLeftClicking = true;
    }

    private void ConfigureRopeVisuals(Rope rope)
    {
        rope._lineRenderer.material = _ropeMaterial;
        rope._lineRenderer.startColor = _ropeStartColor;
        rope._lineRenderer.endColor = _ropeEndColor;
        rope._lineRenderer.startWidth = _ropeWidth;
        rope._lineRenderer.endWidth = _ropeWidth;
        rope._lineRenderer.useWorldSpace = true;
    }

    private void ConfigureRopeVerlet(RopeVerlet ropeVerlet)
    {
        ropeVerlet.SetPhysicsProperties(_gravityForce, _damping, _collisionMask, _collisionRadius, _bounceFactor);
        ropeVerlet.SetConstraintProperties(_nrOfConstraintRuns, _nrOfConstraintRunsHigh);
        ropeVerlet.SetOptimizationProperties(_collisionCheckFrequency, _collisionCheckFrequencyHigh);
        ropeVerlet.SetPlayerInteractionProperties(_playerLayer, _playerProximityRadius);
        ropeVerlet.SetBehaviorProperties(_pinFirstPoint, 0f);
    }

    private void AddSegment(Vector2 newPosition)
    {
        RopeSegment lastSegment = _rope.ropeSegments[_rope.ropeSegments.Count - 1];
        Vector2 lastPos = lastSegment.position;
        float distance = Vector2.Distance(lastPos, newPosition);

        if (distance >= _segmentLength)
        {
            int segmentsToAdd = Mathf.FloorToInt(distance / _segmentLength);
            Vector2 direction = (newPosition - lastPos).normalized;

            for (int i = 1; i <= segmentsToAdd; i++)
            {
                Vector2 newPoint = lastPos + direction * (_segmentLength * i);
                _rope.ropeSegments.Add(new RopeSegment(newPoint));
            }
        }
    }
}