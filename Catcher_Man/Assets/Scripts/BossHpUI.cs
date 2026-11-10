using UnityEngine;
using UnityEngine.UI;

public class BossHpUI : MonoBehaviour
{
    [SerializeField] private BossController boss;

    [SerializeField] private RawImage bossNameImage;
    [SerializeField] private RawImage backgroundImage;
    [SerializeField] private RawImage remainingHpImage;

    private EnemyController enemy;
    private RectTransform hpRect;
    private float fullWidth;
    private int maxHp;

    private void Start()
    {
        if (boss == null ||
            bossNameImage == null ||
            backgroundImage == null ||
            remainingHpImage == null)
        {
            Debug.LogError("BossHpUIの参照を設定してください。", this);
            SetImagesVisible(false);
            enabled = false;
            return;
        }

        enemy = boss.GetComponent<EnemyController>();

        if (enemy == null)
        {
            Debug.LogError("ボスにEnemyControllerがありません。", boss);
            SetImagesVisible(false);
            enabled = false;
            return;
        }

        maxHp = Mathf.Max(1, enemy.EnemyHp);

        hpRect = remainingHpImage.rectTransform;
        fullWidth = hpRect.rect.width;

        remainingHpImage.color = Color.green;
        backgroundImage.color = Color.red;

        UpdateGauge();
    }

    private void LateUpdate()
    {
        UpdateGauge();
    }

    private void UpdateGauge()
    {
        bool showGauge =
            boss != null &&
            enemy != null &&
            boss.IsBattled &&
            enemy.EnemyHp > 0;

        SetImagesVisible(showGauge);

        if (!showGauge)
        {
            return;
        }

        float hpRatio = Mathf.Clamp01(
            (float)enemy.EnemyHp / maxHp
        );

        hpRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            fullWidth * hpRatio
        );
    }

    private void SetImagesVisible(bool visible)
    {
        if (bossNameImage != null)
        {
            bossNameImage.enabled = visible;
        }

        if (backgroundImage != null)
        {
            backgroundImage.enabled = visible;
        }

        if (remainingHpImage != null)
        {
            remainingHpImage.enabled = visible;
        }
    }

    private void OnDisable()
    {
        SetImagesVisible(false);
    }
}