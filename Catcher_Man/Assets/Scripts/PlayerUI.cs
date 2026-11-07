using UnityEngine;
using UnityEngine.UI;

public class PlayerHpUI : MonoBehaviour
{
    [SerializeField] private PlayerManager player;
    [SerializeField] private RawImage backgroundImage;
    [SerializeField] private RawImage remainingHpImage;

    private RectTransform hpRect;
    private float fullWidth;
    private int maxHp;

    private void Start()
    {
        if (player == null ||
            backgroundImage == null ||
            remainingHpImage == null)
        {
            Debug.LogError(
                "PlayerHpUIの参照を設定してください。",
                this
            );
            enabled = false;
            return;
        }

        // 開始時のHPと幅を、最大値として保存する。
        maxHp = Mathf.Max(1, player.PlayerHp);

        hpRect = remainingHpImage.rectTransform;
        fullWidth = hpRect.rect.width;

        backgroundImage.color = Color.red;
        remainingHpImage.color = Color.green;

        UpdateGauge();
    }

    private void LateUpdate()
    {
        UpdateGauge();
    }

    private void UpdateGauge()
    {
        bool showGauge = player != null && player.PlayerHp > 0;

        backgroundImage.enabled = showGauge;
        remainingHpImage.enabled = showGauge;

        if (!showGauge)
        {
            return;
        }

        float hpRatio = Mathf.Clamp01(
            (float)player.PlayerHp / maxHp
        );

        hpRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            fullWidth * hpRatio
        );
    }
}