using UnityEngine;
using System.Collections.Generic;
using System;
public class EnemyController : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("敵撃破");
        if(collision.gameObject.CompareTag("Item"))
        {
            Destroy(this);
            Debug.Log("敵撃破");
        }
    }
}
