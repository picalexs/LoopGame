using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static RopeVerlet;

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

    private List<Rope> _ropes = new List<Rope>();
    private GameObject _ropeObject;
    private Rope _rope;
    private bool _isLeftClicking = false;

    private void Awake()
    {
        _inputSystemActions = new InputSystem_Actions();
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
                        RopeShrinkController shrinkController = _ropeObject.AddComponent<RopeShrinkController>();
                        shrinkController.Initialize(_shrinkRate, _minSegmentLength, _shrinkDelay);
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
            _rope._lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _rope.ropeSegments.Add(new RopeSegment(mousePosition));
        }
        else
        {
            AddSegment(mousePosition);
        }

        _isLeftClicking = true;
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