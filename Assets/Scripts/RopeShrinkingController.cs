using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RopeShrinkController : MonoBehaviour
{
    private Rope _rope;
    private RopeVerlet _ropeVerlet;
    private ChainTensionCalculator _pathCalculator;
    private float _shrinkRate;
    private float _minSegmentLength;
    private float _shrinkDelay;
    private ShrinkMethod _shrinkMethod;
    private bool _hasStartedShrinking = false;
    private bool _isFinishedShrinking = false;
    private bool _hasCalculatedOptimalPath = false;

    [Header("Shrinking Constraints")]
    [SerializeField] private float _minAllowableDistance = 0.05f;
    [SerializeField] private float _tensionThreshold = 2f;
    [SerializeField] private int _stuckFrameThreshold = 30;
    [SerializeField] private float _centripetalForce = 1f;

    [Header("Optimal Path")]
    [SerializeField] private float _pathTransitionSpeed = 2f;
    [SerializeField] private float _pathTolerance = 0.1f;
    [SerializeField] private bool _showOptimalPath = true;

    private int _stuckFrameCount = 0;
    private float _lastTotalRopeLength = 0f;
    private List<Vector2> _targetPath = new List<Vector2>();

    public enum ShrinkMethod
    {
        ReduceLength,
        RemoveSegments,
        Hybrid,
        SmartShrink,
        OptimalPath
    }

    private void Awake()
    {
        _pathCalculator = gameObject.AddComponent<ChainTensionCalculator>();
    }

    public void Initialize(float shrinkRate, float minSegmentLength, float shrinkDelay, ShrinkMethod shrinkMethod,
        float minAllowableDistance, float tensionThreshold, int stuckFrameThreshold, float centripetalForce)
    {
        _shrinkRate = shrinkRate;
        _minSegmentLength = minSegmentLength;
        _shrinkDelay = shrinkDelay;
        _shrinkMethod = shrinkMethod;
        _minAllowableDistance = minAllowableDistance;
        _tensionThreshold = tensionThreshold;
        _stuckFrameThreshold = stuckFrameThreshold;
        _centripetalForce = centripetalForce;

        _rope = GetComponent<Rope>();
        _ropeVerlet = GetComponent<RopeVerlet>();

        if (_shrinkMethod == ShrinkMethod.OptimalPath)
        {
            CalculateOptimalPathImmediate();
        }

        StartCoroutine(StartShrinkingAfterDelay());
    }

    private void CalculateOptimalPathImmediate()
    {
        if (_rope != null && _rope.ropeSegments.Count > 0)
        {
            _targetPath = _pathCalculator.CalculateOptimalWrappedPath(_rope.ropeSegments);
            _pathCalculator.SetOptimalPath(_targetPath);
            _hasCalculatedOptimalPath = true;

            Debug.Log($"Calculated optimal path with {_targetPath.Count} points");
        }
    }

    private IEnumerator StartShrinkingAfterDelay()
    {
        yield return new WaitForSeconds(_shrinkDelay);
        _hasStartedShrinking = true;
        _lastTotalRopeLength = CalculateTotalRopeLength();

        if (_ropeVerlet != null)
        {
            _ropeVerlet.SetPinFirstPoint(false);

            if (_shrinkMethod == ShrinkMethod.OptimalPath)
            {
                _ropeVerlet.SetCentripetalForce(0f);
            }
            else
            {
                _ropeVerlet.SetCentripetalForce(_centripetalForce);
            }
        }
    }

    private void FixedUpdate()
    {
        if (_hasStartedShrinking && !_isFinishedShrinking && _rope != null)
        {
            if (_shrinkMethod == ShrinkMethod.OptimalPath)
            {
                if (_hasCalculatedOptimalPath && _targetPath.Count > 0)
                {
                    bool reachedTarget = MoveTowardsOptimalPath();
                    if (reachedTarget)
                    {
                        FinishShrinking();
                        return;
                    }
                }
            }
            else
            {
                if (ShouldStopShrinking())
                {
                    FinishShrinking();
                    return;
                }

                switch (_shrinkMethod)
                {
                    case ShrinkMethod.ReduceLength:
                        ShrinkRopeByLength();
                        break;
                    case ShrinkMethod.RemoveSegments:
                        ShrinkRopeByRemovingSegments();
                        break;
                    case ShrinkMethod.Hybrid:
                        ShrinkRopeHybrid();
                        break;
                    case ShrinkMethod.SmartShrink:
                        SmartShrinkRope();
                        break;
                }

                SmoothRope();
            }
        }
    }

    private bool MoveTowardsOptimalPath()
    {
        if (_targetPath.Count == 0) return true;

        AdjustSegmentCountToMatchTarget();

        bool allSegmentsNearTarget = true;

        for (int i = 0; i < _rope.ropeSegments.Count; i++)
        {
            RopeSegment segment = _rope.ropeSegments[i];

            int targetIndex = Mathf.RoundToInt((float)i / _rope.ropeSegments.Count * _targetPath.Count) % _targetPath.Count;
            Vector2 targetPos = _targetPath[targetIndex];

            Vector2 toTarget = targetPos - segment.position;
            float distance = toTarget.magnitude;

            if (distance > _pathTolerance)
            {
                allSegmentsNearTarget = false;
                Vector2 moveDirection = toTarget.normalized * _pathTransitionSpeed * Time.fixedDeltaTime;

                if (moveDirection.magnitude > distance)
                {
                    moveDirection = toTarget;
                }

                segment.position += moveDirection;
                segment.previousPosition = segment.position - moveDirection * 0.1f;
                _rope.ropeSegments[i] = segment;
            }
        }

        return allSegmentsNearTarget;
    }

    private void AdjustSegmentCountToMatchTarget()
    {
        if (_targetPath.Count == 0) return;

        int optimalSegmentCount = Mathf.Clamp(_targetPath.Count / 2, 8, 50);

        while (_rope.ropeSegments.Count > optimalSegmentCount)
        {
            RemoveSegmentIntelligently();
        }

        while (_rope.ropeSegments.Count < optimalSegmentCount && _rope.ropeSegments.Count > 0)
        {
            AddSegmentIntelligently();
        }
    }

    private void AddSegmentIntelligently()
    {
        if (_rope.ropeSegments.Count == 0) return;

        float maxDistance = 0f;
        int insertIndex = -1;

        for (int i = 0; i < _rope.ropeSegments.Count; i++)
        {
            int nextIndex = (i + 1) % _rope.ropeSegments.Count;
            float distance = Vector2.Distance(_rope.ropeSegments[i].position, _rope.ropeSegments[nextIndex].position);

            if (distance > maxDistance)
            {
                maxDistance = distance;
                insertIndex = nextIndex;
            }
        }

        if (insertIndex > 0)
        {
            int prevIndex = insertIndex - 1;
            if (prevIndex < 0) prevIndex = _rope.ropeSegments.Count - 1;

            Vector2 newPos = Vector2.Lerp(_rope.ropeSegments[prevIndex].position, _rope.ropeSegments[insertIndex].position, 0.5f);
            RopeSegment newSegment = new RopeSegment(newPos);
            _rope.ropeSegments.Insert(insertIndex, newSegment);
        }
    }

    private bool ShouldStopShrinking()
    {
        float currentLength = CalculateTotalRopeLength();
        if (Mathf.Abs(currentLength - _lastTotalRopeLength) < 0.001f)
        {
            _stuckFrameCount++;
            if (_stuckFrameCount >= _stuckFrameThreshold)
            {
                return true;
            }
        }
        else
        {
            _stuckFrameCount = 0;
            _lastTotalRopeLength = currentLength;
        }

        if (HasExcessiveTension()) return true;
        if (HasTooCloseSegments()) return true;
        if (_rope.GetSegmentLength() <= _minSegmentLength && _rope.ropeSegments.Count <= 8) return true;

        return false;
    }

    private bool HasExcessiveTension()
    {
        if (_rope.ropeSegments.Count < 3) return false;

        for (int i = 1; i < _rope.ropeSegments.Count - 1; i++)
        {
            Vector2 prev = _rope.ropeSegments[i - 1].position;
            Vector2 current = _rope.ropeSegments[i].position;
            Vector2 next = _rope.ropeSegments[i + 1].position;

            Vector2 toPrev = (prev - current).normalized;
            Vector2 toNext = (next - current).normalized;
            float tension = Vector2.Dot(toPrev, toNext);

            if (tension > _tensionThreshold) return true;
        }
        return false;
    }

    private bool HasTooCloseSegments()
    {
        for (int i = 0; i < _rope.ropeSegments.Count - 1; i++)
        {
            float distance = Vector2.Distance(_rope.ropeSegments[i].position, _rope.ropeSegments[i + 1].position);
            if (distance < _minAllowableDistance) return true;
        }
        return false;
    }

    private float CalculateTotalRopeLength()
    {
        float totalLength = 0f;
        for (int i = 0; i < _rope.ropeSegments.Count - 1; i++)
        {
            totalLength += Vector2.Distance(_rope.ropeSegments[i].position, _rope.ropeSegments[i + 1].position);
        }
        return totalLength;
    }

    private void SmartShrinkRope()
    {
        float currentAvgDistance = CalculateTotalRopeLength() / (_rope.ropeSegments.Count - 1);
        float targetDistance = currentAvgDistance * (1f - _shrinkRate * Time.fixedDeltaTime);

        if (_rope.ropeSegments.Count > 12 && currentAvgDistance < _rope.GetSegmentLength() * 0.7f)
        {
            RemoveSegmentIntelligently();
        }
        else
        {
            _rope.SetSegmentLength(targetDistance);
            if (_ropeVerlet != null)
            {
                _ropeVerlet.UpdateSegmentLength(targetDistance);
            }
        }
    }

    private void SmoothRope()
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
            if (deviation < _rope.GetSegmentLength() * 0.3f)
            {
                RopeSegment segment = smoothedSegments[i];
                segment.position = Vector2.Lerp(current, smoothed, 0.5f);
                smoothedSegments[i] = segment;
            }
        }

        _rope.ropeSegments = smoothedSegments;
    }

    private void ShrinkRopeByLength()
    {
        float currentSegmentLength = _rope.GetSegmentLength();
        float newSegmentLength = currentSegmentLength - _shrinkRate * Time.fixedDeltaTime;
        newSegmentLength = Mathf.Max(newSegmentLength, _minSegmentLength);

        _rope.SetSegmentLength(newSegmentLength);

        if (_ropeVerlet != null)
        {
            _ropeVerlet.UpdateSegmentLength(newSegmentLength);
        }
    }

    private void ShrinkRopeByRemovingSegments()
    {
        float removalRate = _shrinkRate * 15f;
        if (Time.fixedTime % (1f / removalRate) < Time.fixedDeltaTime)
        {
            RemoveSegmentIntelligently();
        }
    }

    private void ShrinkRopeHybrid()
    {
        if (_rope.ropeSegments.Count > 15)
        {
            ShrinkRopeByRemovingSegments();
        }
        else
        {
            ShrinkRopeByLength();
        }
    }

    private void RemoveSegmentIntelligently()
    {
        if (_rope.ropeSegments.Count <= 8) return;

        List<RopeSegment> segments = _rope.ropeSegments;
        float maxDistance = 0f;
        int indexToRemove = -1;

        for (int i = 1; i < segments.Count - 1; i++)
        {
            float distance = Vector2.Distance(segments[i - 1].position, segments[i + 1].position);
            float currentDist1 = Vector2.Distance(segments[i - 1].position, segments[i].position);
            float currentDist2 = Vector2.Distance(segments[i].position, segments[i + 1].position);

            float efficiency = (currentDist1 + currentDist2) / distance;

            if (efficiency > maxDistance && distance < _rope.GetSegmentLength() * 1.8f)
            {
                maxDistance = efficiency;
                indexToRemove = i;
            }
        }

        if (indexToRemove > 0)
        {
            segments.RemoveAt(indexToRemove);
        }
    }

    private void FinishShrinking()
    {
        _isFinishedShrinking = true;

        if (_ropeVerlet != null)
        {
            _ropeVerlet.SetCentripetalForce(0f);
            Destroy(_ropeVerlet);
        }

        Debug.Log($"Rope shrinking completed. Method: {_shrinkMethod}, Final segments: {_rope.ropeSegments.Count}");
    }
}