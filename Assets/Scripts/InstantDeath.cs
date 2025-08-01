using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantDeath : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private void Start()
    {
        playerMovement = FindFirstObjectByType<PlayerMovement>();
    }
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.layer == 7)
        {
            playerMovement.Die();
        }
    }
}
