using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class D_Destructible : MonoBehaviour
{

    public int health = 100;

    public void Damage(int dmg = 1)
    {
        health -= dmg;
        if (health <= 0) Kill();
    }

    private void Kill(int dmg = 1)
    {
        Destroy(gameObject);
    }
}
