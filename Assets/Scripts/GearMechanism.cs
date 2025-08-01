using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GearMechanism : MonoBehaviour
{
    public bool isSpinning = false;
    public int way = 1;
    public float speed = 10f;
    private Coroutine spinCoroutine;
    public HashSet<GameObject> neighbours = new HashSet<GameObject>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("Gear") || other.gameObject == gameObject)
        {
            return;
        }

        GameObject otherGear = other.gameObject;
        if (otherGear != gameObject)
            neighbours.Add(otherGear);
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("Gear") || other.gameObject == gameObject)
        {
            return;
        }

        GameObject otherGear = other.gameObject;
        if (otherGear != gameObject)
        {
            GearMechanism otherMech = otherGear.GetComponent<GearMechanism>();
            neighbours.Remove(otherGear);
            otherMech.isSpinning = false;
            otherMech.spinCheck();
        }
    }
    public void spinCheck()
    {
        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
        }
        if (isSpinning == true)
        {
            spinCoroutine = StartCoroutine(spinGear());
        }
    }
    IEnumerator spinGear()
    {
        while (isSpinning)
        {
            float currentSpeed = speed;
            transform.Rotate(0f, 0f, way * currentSpeed * Time.deltaTime);
            yield return null;
        }
    }
}
