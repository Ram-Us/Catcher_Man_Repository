using UnityEngine;

public class ShootEnemyContorller : MonoBehaviour
{
    [SerializeField] private Transform player, bulletPivot;
    [SerializeField] private int bulletSpeed;
    [SerializeField] private float fireInterval = 2f;
    [SerializeField] private GameObject preBullet1, preBullet2;
    [SerializeField] private bool isCrossed;
    [SerializeField] private AudioClip shootClip;

    private float nextFireTime;
    private IEnemyProvider enemyProvider;
    private bool hasReportedError;

    void Awake()
    {
        enemyProvider = GetComponent<IEnemyProvider>();
        nextFireTime = 0f;
    }

    public bool IsCoolingDown => Time.time < nextFireTime;

    public bool TryShoot()
    {
        if (IsCoolingDown) return false;

        if (enemyProvider == null)
            enemyProvider = GetComponent<IEnemyProvider>();

        if (enemyProvider == null)
            return ReportError("同じオブジェクトにIEnemyProviderを実装したBossController等がありません。");
        if (bulletPivot == null)
            return ReportError("Bullet Pivotが未設定です。");
        if (bulletSpeed <= 0)
            return ReportError("Bullet Speedを0より大きい値にしてください。");

        float direction = enemyProvider.DirectionToPlayer;
        // Z座標が完全に同じ場合は前回の弾を使い回さず、発射を見送る。
        if (Mathf.Abs(direction) < 0.001f) return false;
        direction = Mathf.Sign(direction);

        GameObject prefab = direction < 0f ? preBullet1 : preBullet2;
        if (prefab == null)
            return ReportError(direction < 0f
                ? "Pre Bullet 1が未設定です。" : "Pre Bullet 2が未設定です。");
        if (prefab.GetComponent<Rigidbody>() == null)
            return ReportError("弾PrefabのルートにRigidbodyがありません。");

        Vector3 shootDirection = isCrossed
            ? enemyProvider.CrossDirectionToPlayer.normalized
            : Vector3.forward * direction;
        if (shootDirection.sqrMagnitude < 0.0001f) return false;

        GameObject bullet = Instantiate(prefab, bulletPivot.position,
            Quaternion.Euler(0f, 90f, 0f));
        BossController boss = GetComponent<BossController>();
        if (boss != null)
        {
            BossBulletItemHitReporter reporter = bullet.GetComponent<BossBulletItemHitReporter>();
            if (reporter == null) reporter = bullet.AddComponent<BossBulletItemHitReporter>();
            reporter.Initialize(boss);
        }
        bullet.SetActive(true);
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        bulletRb.isKinematic = false;
        bulletRb.constraints &= ~RigidbodyConstraints.FreezePositionZ;
        if (isCrossed)
            bulletRb.constraints &= ~RigidbodyConstraints.FreezePositionY;

        // 銃口がボスのCollider内でも、発射した本人に弾が引っかからない。
        foreach (Collider bulletCollider in bullet.GetComponentsInChildren<Collider>())
        {
            foreach (Collider ownerCollider in GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(bulletCollider, ownerCollider, true);
        }

        bulletRb.linearVelocity = shootDirection * bulletSpeed;
        nextFireTime = Time.time + Mathf.Max(0f, fireInterval);
        hasReportedError = false;
        if (shootClip != null)
        {
            AudioSource.PlayClipAtPoint(shootClip, bulletPivot.position);
        }
        return true;
    }

    private bool ReportError(string message)
    {
        if (!hasReportedError)
        {
            Debug.LogError("ShootEnemyContorller: " + message, this);
            hasReportedError = true;
        }
        return false;
    }

    // 既存のAnimation Event等から呼ぶメソッド名は維持。
    public void ShootBulllet()
    {
        TryShoot();
    }
}
