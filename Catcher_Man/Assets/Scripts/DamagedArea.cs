using UnityEngine;

public class DamagedArea : MonoBehaviour
{
    [SerializeField] private ItemDataBase db;
    [SerializeField] private GameObject hitEffectPrefab, player, enemy;
    [SerializeField] private AudioClip hitClip;

    [SerializeField, Min(0.1f)]
    private float effectLifetime = 3f;

    private EnemyController enemyController;
    private BossController boss;

    private void Awake()
    {
        enemyController = enemy != null
            ? enemy.GetComponentInParent<EnemyController>()
            : GetComponentInParent<EnemyController>();

        boss = GetComponentInParent<BossController>();

        if (boss == null && enemy != null)
        {
            boss = enemy.GetComponentInParent<BossController>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckHit(other, true);
    }

    private void OnTriggerStay(Collider other)
    {
        // ボスの演出を毎フレーム生成しない。
        if (boss != null)
            return;

        CheckHit(other, false);
    }

    private void CheckHit(Collider other, bool entered)
    {
        ReceiveItemHit(
            other.GetComponentInParent<ItemGimmick>(),
            entered
        );
    }

    public void ReceiveItemHit(ItemGimmick item, bool entered = false)
    {
        if (item == null || !item.CompareTag("Item"))
            return;

        if (!item.IsThrown &&
            !(item.IsAttached && item.IsAttacking))
            return;

        // ボスのダメージ処理はBossControllerに任せる。
        // ここでは従来どおり、侵入時の演出だけを扱う。
        if (boss != null)
        {
            if (entered)
                PlayHitEffect();

            return;
        }

        if (enemyController == null || db == null)
        {
            Debug.LogError(
                "EnemyまたはItemDataBaseの設定を確認してください。",
                this
            );
            return;
        }

        if (enemyController.EnemyHp <= 0)
            return;

        int damage = db.GetAttackById(item.Id);

        if (!item.TryRegisterEnemyHit(enemyController))
            return;

        // ダメージが成立したときだけ演出する。
        PlayHitEffect();
        enemyController.SetEnemyHp(damage);

        if (hitClip != null)
        {
            AudioSource.PlayClipAtPoint(
                hitClip,
                transform.position
            );
        }

        if (item.IsThrown)
        {
            Destroy(item.gameObject);
        }
    }

    private void PlayHitEffect()
    {
        if (hitEffectPrefab == null)
        {
            Debug.LogWarning("Hit Effect Prefabが未設定です。", this);
            return;
        }

        // 敵の子にはせず、敵の破壊後も残す。
        GameObject effect = Instantiate(
            hitEffectPrefab,
            transform.position,
            Quaternion.identity
        );

        effect.SetActive(true);

        foreach (ParticleSystem particle in
                 effect.GetComponentsInChildren<ParticleSystem>())
        {
            particle.Play(false);
        }

        Destroy(effect, effectLifetime);
    }
}