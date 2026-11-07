using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private int playerHp;
    public int PlayerHp => playerHp;

    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private AudioClip damageClip;

    [Header("死亡演出")]
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private Transform deathEffectPoint;
    [SerializeField] private GameObject gameOverObject;

    [SerializeField, Min(0.01f)]
    private float destroyDelay = 0.7f;

    [SerializeField, Min(0.01f)]
    private float effectLifeTime = 3f;

    [Header("死亡時に停止するスクリプト・Animator")]
    [SerializeField] private Behaviour[] stopOnDeath;

    private SpriteRenderer[] playerSprites;
    private bool isDead;
    public bool IsDead => isDead;

    [SerializeField] private GameOverController gameOverController;
    

    private void Awake()
    {
        playerSprites = GetComponentsInChildren<SpriteRenderer>(true);

        if (gameOverObject != null)
        {
            gameOverObject.SetActive(false);
        }
    }

    private void Update()
    {
        // InspectorなどでHPを変更した場合にも対応する。
        if (!isDead && playerHp <= 0)
        {
            StartDeath();
        }
    }

    public void SetPlayerHp(int damage)
    {
        if (isDead)
        {
            return;
        }

        int previousHp = playerHp;
        playerHp = Mathf.Max(0, playerHp - damage);

        if (playerHp < previousHp && damageClip != null)
        {
            AudioSource.PlayClipAtPoint(damageClip, transform.position);
        }

        if (playerHp <= 0)
        {
            StartDeath();
        }
    }

    private void StartDeath()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        // 時間停止だけではUpdateや入力イベントは止まらないため、
        // プレイヤーの操作スクリプトも停止する。
        foreach (Behaviour behaviour in stopOnDeath)
        {
            if (behaviour != null && behaviour != this)
            {
                behaviour.enabled = false;
            }
        }

        if (gameOverController != null)
        {
            gameOverController.ShowGameOver();
        }
        else
        {
            Debug.LogError(
                "PlayerManagerのGame Over Controllerが未設定です。",
                this
            );
        }
    }

        
        private void OnCollisionEnter(Collision collision)
        {
            if (isDead)
            {
                return;
            }

            IEnemyProvider enemy =
                collision.gameObject.GetComponentInParent<IEnemyProvider>();

            bool enemyAttacking =
                collision.gameObject.CompareTag("Enemy") &&
                enemy != null &&
                enemy.CurrentStateCode == 2;

            if (enemyAttacking || collision.gameObject.CompareTag("Bullet"))
            {
                PlayHitEffect();
            }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (isDead)
        {
            return;
        }

        IEnemyProvider enemy =
            collision.gameObject.GetComponentInParent<IEnemyProvider>();

        if ((collision.gameObject.CompareTag("Enemy") ||
             collision.gameObject.CompareTag("Bullet")) &&
            enemy != null &&
            enemy.CurrentStateCode == 2)
        {
            PlayHitEffect();
        }
    }

    private void PlayHitEffect()
    {
        if (hitEffectPrefab == null)
        {
            return;
        }

        GameObject effect = Instantiate(
            hitEffectPrefab,
            transform.position,
            Quaternion.identity
        );

        Destroy(effect, 0.5f);
    }
}