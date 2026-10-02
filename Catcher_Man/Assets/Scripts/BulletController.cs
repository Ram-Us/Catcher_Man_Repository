using UnityEngine;
using System.Collections.Generic;
using System;
public class BulletController : MonoBehaviour
{
    [SerializeField] private GameObject enemy;
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            Debug.Log("壁に激突");
            Destroy(this.gameObject);
        }else if (collision.gameObject.CompareTag("Player"))
        {
            int damage = enemy.gameObject.GetComponent<EnemyController>().EnemyAttackDamage;
            collision.gameObject.GetComponent<PlayerManager>().SetPlayerHp(damage);
            Destroy(this.gameObject);
        }else if (collision.gameObject.CompareTag("Item")&&collision.gameObject.GetComponent<ItemGimmick>().IsThrown)
        {
            Destroy(collision.gameObject);
            Destroy(this.gameObject);
        }

        
    }
}
