using UnityEngine;

public class EnergyBallManager : MonoBehaviour
{
    [SerializeField] private BossController boss;
    [SerializeField] private int ballDamage = 50;

    private bool isBossAttack;
    private bool hasHit;
    private bool initialized;

    private void OnEnable()
    {
        hasHit = false;
        initialized = false;
    }

    // SpecialShooterが生成直後に設定。Inspectorでの追加設定は不要。
    public void InitializeOwner(bool bossAttack)
    {
        isBossAttack = bossAttack;
        initialized = true;
    }

    private void Start()
    {
        TryInitialize();
    }

    private bool TryInitialize()
    {
        if (initialized) return true;
        if (boss == null) boss = FindFirstObjectByType<BossController>();
        if (boss == null)
        {
            Debug.LogError("エネルギー弾が参照するボスが見つかりません。", this);
            return false;
        }
        // SpecialShooter以外の既存の生成処理向け。
        isBossAttack = boss.IsSpecialShot;
        initialized = true;
        return true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit || other.isTrigger || !TryInitialize()) return;
        ItemGimmick item = other.GetComponentInParent<ItemGimmick>();
        if (item != null && item.CompareTag("Item"))
        {
            Destroy(item.gameObject);
            Debug.Log($"エネルギー弾でアイテムを破壊：{item.name}");
            return;
        }
        if (isBossAttack)
        {
            PlayerManager player = other.GetComponentInParent<PlayerManager>();
            if (player == null || !player.CompareTag("Player")) return;
            hasHit = true;
            player.SetPlayerHp(ballDamage);
            Destroy(gameObject);
            Debug.Log("プレイヤーに必殺技が命中");
        }
        else
        {
            EnemyController enemy = other.GetComponentInParent<EnemyController>();
            if (enemy == null || !enemy.CompareTag("Enemy")) return;
            hasHit = true;
            enemy.SetEnemyHp(ballDamage);
            Destroy(gameObject);
            Debug.Log("敵にエネルギー弾が命中");
        }
    }
}
