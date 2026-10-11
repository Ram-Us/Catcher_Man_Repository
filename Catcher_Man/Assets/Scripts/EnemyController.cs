using UnityEngine;
using System.Collections.Generic;
using System;
public class EnemyController : MonoBehaviour
{

    [SerializeField] private int enemyHp,enemyAttackDamage;
    public int EnemyHp => enemyHp;
    public int EnemyAttackDamage => enemyAttackDamage;

    [SerializeField] private ItemDataBase db;
    [SerializeField] private float DestroyTime;

    [SerializeField] private bool isSelectedBoss = false;

    

    private void OnCollisionEnter(Collision collision)
    {
        ItemGimmick item = collision.gameObject.GetComponent<ItemGimmick>();
        if(collision.gameObject.CompareTag("Item") &&(item.GetThrown() || item.IsAttached) )
        {   
            int id = item.Id;
            int playerDamage = db.GetAttackById(id);
            //Debug.Log(enemyHp +"<"+ playerDamage);

            if(enemyHp <= playerDamage)
            {
                DefeatEnemy();
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
    private void DefeatEnemy()
    {
        if (isSelectedBoss)
        {
            
            this.transform.GetChild(0).gameObject.GetComponent<Animator>().SetTrigger("Defeat");
            
        }
        Debug.Log("やられます");
        Invoke(nameof(ExplosionEnemy),DestroyTime);
        
    }
    private void ExplosionEnemy()
    {
        Destroy(this.gameObject);
    }
}

