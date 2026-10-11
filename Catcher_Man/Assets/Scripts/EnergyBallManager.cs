using UnityEngine;

public class EnergyBallManager : MonoBehaviour
{

    [SerializeField] private GameObject Boss;

    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log(collision.gameObject);
        if (collision.gameObject.CompareTag("Player"))
        {
            int damage = Boss.gameObject.GetComponent<BossController>().BossSpecialDamage;
            collision.gameObject.GetComponent<PlayerManager>().SetPlayerHp(damage);
            
        }else if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("当たった");
        }
        else if (collision.gameObject.CompareTag("Bullet"))
        {
            Debug.Log("弾同士");
        }
        Destroy(this.gameObject);
    }
}
