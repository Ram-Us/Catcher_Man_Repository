using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private int enemyHp, enemyAttackDamage;

    public int EnemyHp => enemyHp;
    public int EnemyAttackDamage => enemyAttackDamage;

    private BossController bossController;

    private void Awake()
    {
        bossController = GetComponent<BossController>();
    }

    private void FixedUpdate()
    {
        // ボスの死亡演出・破壊はBossControllerが担当する。
        if (bossController != null)
        {
            return;
        }

        if (enemyHp <= 0)
        {
            Destroy(gameObject);
            Debug.Log("敵撃破");
        }
    }

    public void SetEnemyHp(int damage)
    {
        Debug.Log(
            $"HP減少：敵={name}、ダメージ={damage}、" +
            $"HP={enemyHp}→{enemyHp - damage}\n" +
            System.Environment.StackTrace,
            this
        );

        enemyHp -= damage;
    }
}