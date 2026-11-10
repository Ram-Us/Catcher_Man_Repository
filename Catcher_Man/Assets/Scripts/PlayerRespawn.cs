using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerRespawn : MonoBehaviour
{
    [Header("判定")]
    [SerializeField] private string savePointTag = "SavePoint";
    [SerializeField] private string fallZoneTag = "FallZone";
    [SerializeField] private float fallHeight = -20f;
    [SerializeField] private BossPassedWall bossPassedWall;

    [Header("復帰エフェクト")]
    [SerializeField] private GameObject respawnEffectPrefab;
    [SerializeField] private Vector3 effectOffset;
    [SerializeField, Min(0.1f)] private float effectLifetime = 3f;

    private Rigidbody rb;
    private Vector3 savedPosition;
    private SavePoint currentSavePoint;
    private bool respawnRequested;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // セーブポイント未到達時は、ゲーム開始位置へ戻る。
        savedPosition = rb.position;
    }

    private void FixedUpdate()
    {
        bool battled = bossPassedWall.GetComponent<BossController>().IsBattled;
        if (!battled && (respawnRequested || rb.position.y < fallHeight))
        {
            Respawn();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(fallZoneTag))
        {
            // 物理処理のタイミングで復帰する。
            respawnRequested = true;
            return;
        }

        if (!other.CompareTag(savePointTag))
        {
            return;
        }

        SavePoint savePoint = other.GetComponent<SavePoint>();

        if (savePoint == null || currentSavePoint == savePoint)
        {
            return;
        }

        currentSavePoint = savePoint;
        savedPosition = savePoint.RespawnPosition;

        Debug.Log("セーブポイントを更新しました。", savePoint);
    }

    private void Respawn()
    {
        respawnRequested = false;

        // 落下や移動の勢いをリセットする。
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // ワールド座標で復帰位置を設定する。
        rb.position = savedPosition;
        transform.position = savedPosition;

        if (respawnEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                respawnEffectPrefab,
                savedPosition + effectOffset,
                Quaternion.identity
            );

            foreach (ParticleSystem particle
                     in effect.GetComponentsInChildren<ParticleSystem>())
            {
                particle.Play(false);
            }

            Destroy(effect, effectLifetime);
        }
    }
}