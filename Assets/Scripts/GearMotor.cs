using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GearMotor : MonoBehaviour
{
    HashSet<GameObject> visited = new HashSet<GameObject> ();
    public static GearMechanism motorMech;
    public static bool isSpinning;
    void Start()
    {
        motorMech = gameObject.GetComponent<GearMechanism>();
        isSpinning = motorMech.isSpinning;
        visited.Add(gameObject);
    }
    void Update()
    {
        gameObject.GetComponent<GearMechanism>().spinCheck();
        Propagate(gameObject, motorMech.neighbours);
        visited.Clear();
        visited.Add(gameObject);
    }
    public void Propagate(GameObject parent,HashSet<GameObject> neighbours)
    {
        //Debug.Log("Entered Propagate function");
        foreach (GameObject neighbour in neighbours)
        {
            if (!visited.Contains(neighbour))
                visited.Add(neighbour);
            else continue;
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
            //Debug.Log(motorMech.isSpinning);
            if (motorMech.isSpinning)
            {
                float newSpeed = parentMech.speed * numberOfTeeth / otherNumberOfTeeth;
                if (Mathf.Abs(otherMech.speed - newSpeed) > 0.01f || otherMech.way != parentMech.way * -1 || !otherMech.isSpinning)
                {
                    otherMech.speed = newSpeed;
                    otherMech.way = parentMech.way * -1;
                    otherMech.isSpinning = true;
                    otherMech.spinCheck();
                }
            }
            else
            {
                otherMech.isSpinning = false;
                otherMech.spinCheck();
            }
            Propagate(neighbour, otherMech.neighbours);
        }
    }
}
