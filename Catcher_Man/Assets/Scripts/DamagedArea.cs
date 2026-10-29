using UnityEngine;

public class DamagedArea : MonoBehaviour
{
    [SerializeField] private ItemDataBase db;
    [SerializeField] private GameObject hitEffectPrefab,player,enemy;
    private GameObject pre;

    private void OnTriggerEnter(Collider collision)
    {
        // この判定が敵の子に付いていても、親のBossControllerを検出する。
        if (GetComponentInParent<BossController>() != null)
        {
            return;
        }

        ItemGimmick item =
            collision.GetComponentInParent<ItemGimmick>();

        if (item == null || !item.CompareTag("Item"))
        {
            return;
        }

        // 投げたアイテム、または振っている最中の装備だけを攻撃扱いにする。
        if (!item.IsThrown && !(item.IsAttached && item.IsAttacking))
        {
            return;
        }

        if (enemy == null || db == null)
        {
            Debug.LogError("EnemyまたはItemDataBaseが未設定です。", this);
            return;
        }

        EnemyController enemyController =
            enemy.GetComponent<EnemyController>();

        if (enemyController == null)
        {
            Debug.LogError("EnemyにEnemyControllerがありません。", enemy);
            return;
        }

        int damage = db.GetAttackById(item.Id);
        enemyController.SetEnemyHp(damage);

        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                hitEffectPrefab,
                enemy.transform.position,
                Quaternion.identity
            );

            Destroy(effect, 0.2f);
        }

        if (item.IsThrown)
        {
            Destroy(item.gameObject);
        }
    }
    }

