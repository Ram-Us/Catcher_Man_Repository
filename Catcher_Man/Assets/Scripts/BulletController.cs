using UnityEngine;
using System.Collections.Generic;
using System;
public class BulletController : MonoBehaviour
{
    [SerializeField] private GameObject enemy;
    [SerializeField] private AudioClip hitClip;

    private void OnCollisionEnter(Collision collision)
    {
        if (hitClip != null)
        {
            Vector3 hitPosition = collision.contactCount > 0
                ? collision.GetContact(0).point
                : transform.position;
            AudioSource.PlayClipAtPoint(hitClip, hitPosition);
        }

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
