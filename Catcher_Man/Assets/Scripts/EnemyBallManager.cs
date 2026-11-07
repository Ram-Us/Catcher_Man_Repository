using UnityEngine;

public class EnemyBallManager : MonoBehaviour
{
    [SerializeField] private int ballDamage;
    [SerializeField] private bool isFire;
    [SerializeField] private bool isLightning;
    public bool IsFire => isFire;
    public bool IsLightning => isLightning;

    [SerializeField] private GameObject burnEffectPrefab;
    [SerializeField, Min(1f)] private float burnEffectLifetime = 3f;
    [SerializeField] private AudioClip hitClip;
    private bool hasHit;

    private void OnCollisionEnter(Collision collision)
    {
        // 複数Colliderへの同フレーム命中を一度だけ処理する。
        if (hasHit) return;
        hasHit = true;
        if (hitClip != null)
        {
            Vector3 hitPosition = collision.contactCount > 0
                ? collision.GetContact(0).point
                : transform.position;
            AudioSource.PlayClipAtPoint(hitClip, hitPosition);
        }

        PlayerManager player = collision.gameObject.GetComponentInParent<PlayerManager>();
        if (player != null && player.CompareTag("Player"))
        {
            ActionController action = player.GetComponent<ActionController>();
            bool handledByEquipment = isFire && action != null &&
                action.TryHandleFireBall(player, ballDamage,
                    burnEffectPrefab, burnEffectLifetime);

            // 無装備、または通常弾・雷弾は従来の即時ダメージ。
            if (!handledByEquipment) player.SetPlayerHp(ballDamage);
        }
        else if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyController enemy = collision.gameObject.GetComponent<EnemyController>();
            if (enemy != null) enemy.SetEnemyHp(ballDamage);
        }
        // 待機処理はプレイヤー側が持つため、弾はここで破棄できる。
        Destroy(gameObject);
    }

    private void OnValidate()
    {
        // 同時にONの場合は火属性を優先する。
        if (isFire && isLightning) isLightning = false;
    }
}
