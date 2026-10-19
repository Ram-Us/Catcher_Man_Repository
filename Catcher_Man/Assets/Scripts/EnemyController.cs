using UnityEngine;
using System.Collections.Generic;
using System;
public class EnemyController : MonoBehaviour
{

    [SerializeField] private int enemyHp,enemyAttackDamage;
    public int EnemyHp => enemyHp;
    public int EnemyAttackDamage => enemyAttackDamage;

    [SerializeField] private ItemDataBase db;
    [SerializeField] private GameObject hitEffectPrefab;


    

    private void OnCollisionEnter(Collision collision)
    {
        // ボスのHP・撃破・特殊武器解除はBossController側で一元管理する。
        // ここでも処理すると二重ダメージや即Destroyが発生するため、通常敵だけ処理する。
        if (TryGetComponent<BossController>(out _))
        {
            return;
        }

        ItemGimmick item = collision.gameObject.GetComponent<ItemGimmick>();
        if(collision.gameObject.CompareTag("Item") &&(item.GetThrown() || item.IsAttached) )
        {   
            int id = item.Id;
            int playerDamage = db.GetAttackById(id);
            Debug.Log(enemyHp +"<"+ playerDamage);
            if(enemyHp < playerDamage)
            {
                Destroy(this.gameObject);
                Debug.Log("敵撃破");
            }
            else
            {
                enemyHp -= playerDamage;
                Debug.Log("HPを"+playerDamage+"減らしたよ");
            }
            if (item.GetThrown())
            {
                Destroy(collision.gameObject);
            }
            if (hitEffectPrefab != null &&
            collision.contactCount > 0)
            {
                ContactPoint contact = collision.GetContact(0);

                GameObject pre = Instantiate(
                    hitEffectPrefab,
                    contact.point,
                    Quaternion.LookRotation(contact.normal)
                );
                Destroy(pre);
            }
            
        }
    }
    public void SetEnemyHp(int damage)
    {
        enemyHp -= damage;
    }
}
