using UnityEngine;
using System.Collections;

public class RopeShrinkController : MonoBehaviour
{
    private Rope _rope;
    private RopeVerlet _ropeVerlet;
    private float _shrinkRate;
    private float _minSegmentLength;
    private float _shrinkDelay;
    private bool _hasStartedShrinking = false;
    private bool _isFinishedShrinking = false;

    public void Initialize(float shrinkRate, float minSegmentLength, float shrinkDelay)
    {
        _shrinkRate = shrinkRate;
        _minSegmentLength = minSegmentLength;
        _shrinkDelay = shrinkDelay;

        _rope = GetComponent<Rope>();
        _ropeVerlet = GetComponent<RopeVerlet>();

        StartCoroutine(StartShrinkingAfterDelay());
    }

    private IEnumerator StartShrinkingAfterDelay()
    {
        yield return new WaitForSeconds(_shrinkDelay);
        _hasStartedShrinking = true;

        if (_ropeVerlet != null)
        {
            _ropeVerlet.SetPinFirstPoint(false);
        }
    }

    private void FixedUpdate()
    {
        if (_hasStartedShrinking && !_isFinishedShrinking && _rope != null)
        {
            ShrinkRope();
        }
    }

    private void ShrinkRope()
    {
        float currentSegmentLength = _rope.GetSegmentLength();

        if (currentSegmentLength <= _minSegmentLength)
        {
            _isFinishedShrinking = true;
            if (_ropeVerlet != null)
            {
                Destroy(_ropeVerlet);
            }
            return;
        }
        float newSegmentLength = currentSegmentLength - _shrinkRate * Time.fixedDeltaTime;
        newSegmentLength = Mathf.Max(newSegmentLength, _minSegmentLength);

        _rope.SetSegmentLength(newSegmentLength);

        if (_ropeVerlet != null)
        {
            _ropeVerlet.UpdateSegmentLength(newSegmentLength);
        }
    }
}