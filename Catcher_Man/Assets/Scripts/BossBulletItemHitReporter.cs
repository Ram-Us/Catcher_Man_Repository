using UnityEngine;

// 通常弾に発射時に自動追加。ダメージや弾の破壊処理は行わない。
public class BossBulletItemHitReporter : MonoBehaviour
{
    private BossController owner;
    private bool reported;

    public void Initialize(BossController boss)
    {
        owner = boss;
        reported = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 point = collision.contactCount > 0
            ? collision.GetContact(0).point : transform.position;
        Report(collision.collider, point);
    }

    private void OnTriggerEnter(Collider other)
    {
        // サーチ用などの広いTrigger範囲を命中に数えない。
        if (other.isTrigger) return;
        Report(other, other.ClosestPoint(transform.position));
    }

    private void Report(Collider other, Vector3 point)
    {
        if (reported || owner == null || other == null) return;
        if (!owner.IsWorldItemCollider(other)) return;
        reported = true; // 複数Colliderがあっても、この弾では1回だけ。
        owner.ReportBulletBlocked(point);
    }
}
