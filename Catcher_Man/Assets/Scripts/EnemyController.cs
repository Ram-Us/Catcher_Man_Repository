using UnityEngine;
using System.Collections.Generic;
using System;
public class EnemyController : MonoBehaviour
{

    [SerializeField] private int enemyHp,enemyAttackDamage;
    public int EnemyHp => enemyHp;
    public int EnemyAttackDamage => enemyAttackDamage;

    [SerializeField] private ItemDataBase db;


    

    private void OnCollisionEnter(Collision collision)
    {
        ItemGimmick item = collision.gameObject.GetComponent<ItemGimmick>();
        if(collision.gameObject.CompareTag("Item") &&(item.GetThrown() || item.IsAttached) )
        {   
            int id = item.Id;
            int playerDamage = db.GetAttackById(id);
            Debug.Log(enemyHp +"<"+ playerDamage);
            if(enemyHp <= playerDamage)
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
            
        }
    }
    public void SetEnemyHp(int damage)
    {
        enemyHp -= damage;
    }
}
