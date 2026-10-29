using UnityEngine;
using System.Collections.Generic;
using System;
public class PlayerManager : MonoBehaviour
{
    [SerializeField] private int playerHp;
    public int PlayerHp => playerHp;
    [SerializeField] private GameObject hitEffectPrefab;
    private GameObject pre;

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnCollisionEnter(Collision collision)
    {
        IEnemyProvider enemy = collision.gameObject.GetComponent<IEnemyProvider>();
        if((collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("Bullet")) && enemy.CurrentStateCode==2){
            if (hitEffectPrefab != null)
            {

                pre = Instantiate(
                    hitEffectPrefab,
                    this.gameObject.transform.position,
                    Quaternion.identity
                );
                //Debug.Log(pre.transform.position+"くらってる");
                Destroy(pre,0.5f);
   
            }
        }
        
    }
    void OnCollisionStay(Collision collision)
    {
        IEnemyProvider enemy = collision.gameObject.GetComponent<IEnemyProvider>();
        if((collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("Bullet")) && enemy.CurrentStateCode==2){
            if (hitEffectPrefab != null)
            {

                pre = Instantiate(
                    hitEffectPrefab,
                    this.gameObject.transform.position,
                    Quaternion.identity
                );
                //Debug.Log(pre.transform.position+"くらってる");
                Destroy(pre,0.5f);
   
            }
        }
        
    }
    public void SetPlayerHp(int damage)
    {
        playerHp -= damage;
        //Debug.Log(damage+"HP減らしてるよ");
    }

}