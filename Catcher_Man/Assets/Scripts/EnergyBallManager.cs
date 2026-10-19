using UnityEngine;

public class EnergyBallManager : MonoBehaviour
{

    [SerializeField] private GameObject Boss;
    [SerializeField]private int ballDamage = 50;

    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log(collision.gameObject);
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<PlayerManager>().SetPlayerHp(ballDamage);
            Debug.Log("プレイヤーに必殺技が的中");
            
        }else if (collision.gameObject.CompareTag("Enemy")&&!(Boss.GetComponent<BossController>().IsSpecialized)){
        
            collision.gameObject.GetComponent<EnemyController>().SetEnemyHp(ballDamage);
            Debug.Log("ボスに必殺技が命中");
        }
        else if (collision.gameObject.CompareTag("Bullet"))
        {
            Debug.Log("弾同士");
        }
        Destroy(this.gameObject);
    }
}
