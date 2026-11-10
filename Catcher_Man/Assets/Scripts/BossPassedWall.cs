using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

[RequireComponent(typeof(BoxCollider))]
public class BossPassedWall : MonoBehaviour
{
    [Header("プレイヤー")]
    [SerializeField] private MoveController playerMove;
    [SerializeField] private Collider playerBody;
    [SerializeField] private Transform playerIntroPoint;
    [SerializeField, Min(0.1f)] private float walkSpeed = 3f;
    [SerializeField, Min(1f)] private float walkTimeout = 15f;

    [Tooltip("ActionController、ItemSearcherなど。MoveControllerは入れない")]
    [SerializeField] private Behaviour[] pauseDuringIntro;

    [Header("ボス")]
    [SerializeField] private BossController boss;
    [SerializeField] private Rigidbody bossBody;

    [Header("登場演出")]
    [SerializeField] private PlayableDirector director;
    [SerializeField] private GameObject ship;

    private BoxCollider wallCollider;
    private InputActionMap gameplayMap;

    private SkinnedMeshRenderer[] bossMeshes;
    private Collider[] bossColliders;
    private bool[] colliderStates;
    private bool[] scriptStates;

    private bool bossWasKinematic;
    private bool gameplayWasEnabled;
    private bool prepared;
    private bool isRunning;
    private bool flag;

    public bool Flag => flag;

    private void Awake()
    {
        wallCollider = GetComponent<BoxCollider>();
        wallCollider.isTrigger = true;

        if (director != null)
            director.playOnAwake = false;

        // 最初の描画前にボスの見た目だけ隠す。
        if (boss != null)
        {
            bossMeshes =
                boss.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            SetBossVisible(false);
        }
    }

    private void Start()
    {
        if (playerMove == null || playerBody == null ||
            playerIntroPoint == null || boss == null ||
            bossBody == null || director == null ||
            director.playableAsset == null)
        {
            Debug.LogError(
                "BossPassedWallの参照をすべて設定してください。",
                this);

            enabled = false;
            return;
        }

        gameplayMap = InputSystem.actions != null
            ? InputSystem.actions.FindActionMap("PlayerActions", false)
            : null;

        if (gameplayMap == null)
        {
            Debug.LogError("PlayerActionsが見つかりません。", this);
            enabled = false;
            return;
        }

        director.playOnAwake = false;
        director.extrapolationMode = DirectorWrapMode.Hold;

        bossMeshes =
            boss.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        SetBossVisible(false);

        bossColliders = boss.GetComponentsInChildren<Collider>(true);
        colliderStates = new bool[bossColliders.Length];

        for (int i = 0; i < bossColliders.Length; i++)
            colliderStates[i] = bossColliders[i].enabled;

        bossWasKinematic = bossBody.isKinematic;

        if (!bossBody.isKinematic)
        {
            bossBody.linearVelocity = Vector3.zero;
            bossBody.angularVelocity = Vector3.zero;
        }

        bossBody.isKinematic = true;

        // GameObjectとAnimatorは有効のままにする。
        SetBossColliders(false);
        boss.BeginIntro();

        prepared = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryStartIntro(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryStartIntro(other);
    }

    private void TryStartIntro(Collider other)
    {
        if (!isActiveAndEnabled || !prepared || flag || isRunning ||
            other != playerBody || !playerMove.isActiveAndEnabled)
        {
            return;
        }

        flag = true;
        isRunning = true;

        // プレイヤー本体が演出を開始した場合だけ表示する。
        if (ship != null)
            ship.SetActive(true);

        gameplayWasEnabled = gameplayMap.enabled;
        gameplayMap.Disable();

        int count = pauseDuringIntro != null
            ? pauseDuringIntro.Length
            : 0;

        scriptStates = new bool[count];

        for (int i = 0; i < count; i++)
        {
            Behaviour script = pauseDuringIntro[i];

            if (script == null || script == this ||
                script == playerMove || script == director ||
                script == boss || script is Animator ||
                script is PlayerSpriteController)
            {
                continue;
            }

            scriptStates[i] = script.enabled;
            script.enabled = false;
        }

        playerMove.BeginIntroWalk(
            playerIntroPoint.position.z,
            walkSpeed);

        StartCoroutine(PlayIntro());
    }

    private void OnTriggerExit(Collider other)
    {
        if (!prepared || !flag || other != playerBody)
            return;

        // プレイヤー本体が通り抜けたら壁を閉じる。
        wallCollider.isTrigger = false;
    }

    private IEnumerator PlayIntro()
    {
        float elapsed = 0f;

        while (!playerMove.HasReachedIntroPoint)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= walkTimeout)
            {
                Debug.LogError(
                    "演出位置へ到着できません。経路と接地判定を確認してください。",
                    this);

                FinishIntro(false);
                yield break;
            }

            yield return null;
        }

        // 自動歩行が終わった時点でボスを表示する。
        director.time = 0;
        SetBossVisible(true);
        director.Play();

        yield return null;

        double duration = director.duration;

        if (double.IsInfinity(duration) ||
            double.IsNaN(duration) || duration <= 0)
        {
            Debug.LogError("Timelineの長さが不正です。", this);
            FinishIntro(false);
            yield break;
        }

        while (director.time < duration - 0.001)
        {
            if (director.state != PlayState.Playing)
            {
                Debug.LogWarning(
                    "登場演出が途中で停止しました。",
                    this);

                FinishIntro(false);
                yield break;
            }

            yield return null;
        }

        FinishIntro(true);
    }

    private void SetBossVisible(bool visible)
    {
        if (bossMeshes == null)
            return;

        foreach (SkinnedMeshRenderer mesh in bossMeshes)
        {
            if (mesh != null)
                mesh.enabled = visible;
        }
    }

    private void SetBossColliders(bool active)
    {
        if (bossColliders == null)
            return;

        for (int i = 0; i < bossColliders.Length; i++)
        {
            if (bossColliders[i] != null)
                bossColliders[i].enabled = active && colliderStates[i];
        }
    }

    private void FinishIntro(bool startBattle)
    {
        Vector3 finalPosition = boss.transform.position;
        Quaternion finalRotation = boss.transform.rotation;

        director.Stop();

        if (startBattle)
        {
            // Idleへの切り替えを先に済ませる。
            boss.EndIntro(true);
            SetBossVisible(true);
        }
        else
        {
            boss.BeginIntro();
            SetBossVisible(false);
            SetBossColliders(false);

            flag = false;
            wallCollider.isTrigger = true;
        }

        // Timeline停止・Idle切り替え後に姿勢を復元する。
        boss.transform.SetPositionAndRotation(
            finalPosition,
            finalRotation);

        bossBody.position = finalPosition;
        bossBody.rotation = finalRotation;

        if (startBattle)
        {
            bossBody.isKinematic = bossWasKinematic;
            SetBossColliders(true);
        }

        RestorePlayerControl();
    }

    private void RestorePlayerControl()
    {
        if (playerMove != null)
            playerMove.EndIntroControl();

        if (scriptStates != null && pauseDuringIntro != null)
        {
            int count = Mathf.Min(
                scriptStates.Length,
                pauseDuringIntro.Length);

            for (int i = 0; i < count; i++)
            {
                if (pauseDuringIntro[i] != null && scriptStates[i])
                    pauseDuringIntro[i].enabled = true;
            }
        }

        if (gameplayWasEnabled)
            gameplayMap?.Enable();

        isRunning = false;
    }

    private void OnDisable()
    {
        if (!isRunning)
            return;

        StopAllCoroutines();

        if (director != null)
            director.Stop();

        SetBossVisible(false);

        flag = false;

        if (wallCollider != null)
            wallCollider.isTrigger = true;

        RestorePlayerControl();
    }
}