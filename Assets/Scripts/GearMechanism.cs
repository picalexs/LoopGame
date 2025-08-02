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
    HashSet<GameObject> visited = new HashSet<GameObject>();

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
        GearMechanism otherMech = otherGear.GetComponent<GearMechanism>();
        neighbours.Remove(otherGear);
        if(otherGear.GetComponent<GearMotor>() == null)
        {
            otherMech.isSpinning = false;
            visited.Add(otherGear);
            PropagateStop(otherGear, otherMech.neighbours);
            visited.Clear();
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
    void PropagateStop(GameObject parent, HashSet<GameObject> neighbours)
    {
        foreach (GameObject neighbour in neighbours)
        {
            if (!visited.Contains(neighbour))
                visited.Add(neighbour);
            else continue;
            if (neighbour.GetComponent<GearMotor>() != null)
                continue;
            GearMechanism otherMech = neighbour.GetComponent<GearMechanism>();
            GearGenerator otherGen = neighbour.GetComponent<GearGenerator>();
            GearMechanism parentMech = parent.GetComponent<GearMechanism>();
            GearGenerator parentGen = parent.GetComponent<GearGenerator>();

            if (otherMech == null || otherGen == null || parentGen == null)
            {
                Debug.LogWarning("Gear not found");
                return;
            }

            int numberOfTeeth = parentGen.numberOfTeeth;
            int otherNumberOfTeeth = otherGen.numberOfTeeth;

            otherMech.isSpinning = false;
            otherMech.spinCheck();

            PropagateStop(neighbour, otherMech.neighbours);
        }
    }
}
