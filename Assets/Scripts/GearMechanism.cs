using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GearMechanism : MonoBehaviour
{
    public bool isSpinning = false;
    //public bool isMotor = false;
    public int way = 1;
    public float speed = 10f;
    private Coroutine spinCoroutine;
    public HashSet<GameObject> neighbours = new HashSet<GameObject>();
    //void OnValidate()
    //{
    //    if (Application.isPlaying)
    //    {
    //        spinCheck();
    //    }
    //}

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("Gear") || other.gameObject == gameObject)
        {
            return;
        }

        GameObject otherGear = other.gameObject;
        if (otherGear != gameObject)
            neighbours.Add(otherGear);
        //GearMechanism otherMech = otherGear.GetComponent<GearMechanism>();
        //GearGenerator otherGen = otherGear.GetComponent<GearGenerator>();
        //GearGenerator thisGen = GetComponent<GearGenerator>();

        //if (otherMech == null || otherGen == null || thisGen == null)
        //{
        //    Debug.LogWarning("Doesn't find gear");
        //    return;
        //}

        //neighbours.Add(otherGear);
        //if (otherMech.isMotor)
        //    return;

        //int numberOfTeeth = thisGen.numberOfTeeth;
        //int otherNumberOfTeeth = otherGen.numberOfTeeth;

        //if (isSpinning)
        //{
        //    float newSpeed = speed * numberOfTeeth / otherNumberOfTeeth;
        //    if (Mathf.Abs(otherMech.speed - newSpeed) > 0.01f || otherMech.way != way * -1 || !otherMech.isSpinning)
        //    {
        //        otherMech.speed = newSpeed;
        //        otherMech.way = way * -1;
        //        otherMech.isSpinning = true;
        //        otherMech.spinCheck();
        //    }
        //}
        //else
        //{
        //    otherMech.isSpinning = false;
        //    otherMech.spinCheck();
        //}

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
        //Debug.Log($"{gameObject.name} spinning with speed: {speed} and way: {way}");
        while (isSpinning)
        {
            float currentSpeed = speed;
            transform.Rotate(0f, 0f, way * currentSpeed * Time.deltaTime);
            yield return null;
        }
    }
}
