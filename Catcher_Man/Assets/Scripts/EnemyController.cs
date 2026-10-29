using UnityEngine;
using System.Collections;
using System;
using Unity.VisualScripting;
public class EnemyController : MonoBehaviour
{

    [SerializeField] private int enemyHp,enemyAttackDamage;
    public int EnemyHp => enemyHp;
    public int EnemyAttackDamage => enemyAttackDamage;


    void FixedUpdate()
    {
        if(enemyHp <= 0)
        {
            Destroy(this.gameObject);
            Debug.Log("敵撃破");
        }

    }







    public void SetEnemyHp(int damage)
    {
        enemyHp -= damage;
    }
    
}
