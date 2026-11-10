using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VictoryResultUI : MonoBehaviour
{
    [SerializeField] private BossController boss;

    [SerializeField] private GameObject[] resultObjects =
        new GameObject[3];

    [SerializeField] private TMP_Text resultText;
    [SerializeField, Min(0f)] private float displayDelay = 5f;

    // ID0（アイテムなし）を含めた総数を設定する。
    [SerializeField, Min(1)] private int totalItemCount = 1;

    private readonly HashSet<int> collectedItemIds = new();

    private EnemyController enemy;
    private bool victoryStarted;

    private void Awake()
    {
        SetResultVisible(false);
    }

    private void Start()
    {
        if (boss == null || resultText == null)
        {
            Debug.LogError(
                "VictoryResultUIのBossとResult Textを設定してください。",
                this
            );
            enabled = false;
            return;
        }

        enemy = boss.GetComponent<EnemyController>();

        if (enemy == null)
        {
            Debug.LogError(
                "ボスにEnemyControllerがありません。",
                boss
            );
            enabled = false;
        }
    }

    private void Update()
    {
        if (victoryStarted || enemy == null)
        {
            return;
        }

        if (enemy.EnemyHp <= 0)
        {
            victoryStarted = true;
            StartCoroutine(ShowResultAfterDelay());
        }
    }

    // ActionControllerから、取得したアイテムのIDを受け取る。
    public void AddCollectedItem(int itemId)
    {
        if (victoryStarted || itemId <= 0)
        {
            return;
        }

        // 同じIDを何度追加しても、登録数は増えない。
        collectedItemIds.Add(itemId);
    }

    private IEnumerator ShowResultAfterDelay()
    {
        yield return new WaitForSecondsRealtime(displayDelay);

        int targetCount = Mathf.Max(0, totalItemCount - 1);

        resultText.text =
            $"{collectedItemIds.Count}/{targetCount}";

        SetResultVisible(true);
    }

    private void SetResultVisible(bool visible)
    {
        foreach (GameObject resultObject in resultObjects)
        {
            if (resultObject != null)
            {
                resultObject.SetActive(visible);
            }
        }

        if (resultText != null)
        {
            resultText.gameObject.SetActive(visible);
        }
    }
}