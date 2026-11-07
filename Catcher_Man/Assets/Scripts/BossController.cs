using UnityEngine;
using System;
using System.Collections.Generic;


public class BossController : MonoBehaviour,IEnemyProvider
{
    private enum EnemyState
    {
        Idle,
        Move,
        MeleeAttack,
        RangedAttack,
        DepthAttack,
        CoolDown,
        Defeat
    }
    private EnemyController enemyData;
    private ShootEnemyContorller shooter;
    [SerializeField] private EnemyState currentState,previousState;
    [SerializeField] private float idleDistance,moveDistance,meleeDistance,rangedDistance,jumpPower,coolDownTimer,lerpTime,specialTimer;
    private float currentDistance,currentTimer,direction;
    [SerializeField] private Transform player;
    [SerializeField] private int moveSpeed,bossAttackDamage,bossSpecialDamage;
    [SerializeField] private AudioClip moveClip, meleeAttackClip, itemHitClip;
    [Header("効果音の音量")]
    [SerializeField, Range(0f, 5f)]
    private float moveVolume = 1f;

    [SerializeField, Range(0f, 5f)]
    private float meleeAttackVolume = 1f;

    [SerializeField, Range(0f, 5f)]
    private float itemHitVolume = 1f;
    
    public int BossAttackDamage => bossAttackDamage;
    public int BossSpecialDamage => bossSpecialDamage;
    private int currentSpeed;
    [SerializeField] private int random;
    private Rigidbody rb;
    private Animator anim;
    [SerializeField] private bool isGrounded = false,isShoot = false,isSpeciealized = true,isStopped,isSpecialStarted = false,isSpecialShot = false,isBattled = false;
    public bool IsSpecialized => isSpeciealized;
    public bool IsSpecialShot => isSpecialShot;
    public bool IsBattled => isBattled;
    private bool battleStateApplied;
    private bool hasStarted;
    private bool lastBattleState;
    private float battleAnimatorSpeed = 1f;
    private bool waitingForMeleeAnimation;

    [Header("通常攻撃は近接・射撃を交互に実行")]
    [SerializeField, Range(0f, 1f)] private float doubleSpeedChance = 0.3f;
    private int approachSpeedMultiplier = 1;
    private bool nextAttackIsMelee = true;
    private int normalAttacksSinceSpecial;

    [Header("特殊攻撃の間隔")]
    [SerializeField, Min(1f)] private float specialAttackInterval = 12f;

    [Header("通常弾が遮られた場合も特殊攻撃を予約")]
    [SerializeField] private bool useBlockedShotSpecial = true;
    [SerializeField, Min(1)] private int blockedShotsForSpecial = 3;
    [SerializeField] private int blockedShotCount;
    private bool counterSpecialPending;
    private bool waitingForSpecialAnimation;
    [Header("worldItemとの接触判定")]
    [SerializeField] private string worldItemLayerName = "worldItem";
    [SerializeField, Min(0f)] private float reactiveSpecialCooldown = 2f;
    private int worldItemLayer = -1;
    private float nextReactiveSpecialTime;

    public bool IsWorldItemCollider(Collider other)
    {
        return other != null && worldItemLayer >= 0 &&
            other.gameObject.layer == worldItemLayer;
    }

    private bool CanRequestReactiveSpecial()
    {
        return isActiveAndEnabled && isBattled && !isIntroPlaying && !defeatStarted &&
            !depthAttackLocked && sp != null && player != null &&
            currentState != EnemyState.DepthAttack && Time.time >= nextReactiveSpecialTime;
    }

    private void CheckWorldItemContact(Collider other)
    {
        if (!IsWorldItemCollider(other) || !CanRequestReactiveSpecial()) return;
        if (currentState == EnemyState.Move || currentState == EnemyState.MeleeAttack)
            counterSpecialPending = true;
    }

    private void StartReactiveSpecialIfReady()
    {
        if (!counterSpecialPending || !CanRequestReactiveSpecial()) return;
        // 通常射撃中は、そのセットとクールダウンの終了を待つ。
        if (currentState != EnemyState.Move && currentState != EnemyState.MeleeAttack &&
            currentState != EnemyState.Idle) return;
        counterSpecialPending = false;
        blockedShotCount = 0;
        isAttacking = false;
        waitingForMeleeAnimation = false;
        anim.ResetTrigger("Attack");
        anim.ResetTrigger("Run");
        anim.ResetTrigger("Idle");
        StopMovement();
        currentState = EnemyState.DepthAttack;
    }


    // 弾1発につき1回だけ、BossBulletItemHitReporterから呼ばれる。
    public void ReportBulletBlocked(Vector3 hitPosition)
    {
        if (!useBlockedShotSpecial || !CanRequestReactiveSpecial() || counterSpecialPending) return;
        blockedShotCount++;
        if (blockedShotCount >= Mathf.Max(1, blockedShotsForSpecial))
        {
            blockedShotCount = 0;
            counterSpecialPending = true;
        }
    }

    // 攻撃中は中断しない。終了後のクールダウンを経て次を選ぶ。
    private EnemyState ChooseNextAction()
    {
        if (!depthAttackLocked && sp != null &&
            ((normalAttacksSinceSpecial > 0 && specialTimer <= 0f) ||
             (counterSpecialPending && Time.time >= nextReactiveSpecialTime)))
        {
            counterSpecialPending = false;
            blockedShotCount = 0;
            return EnemyState.DepthAttack;
        }
        if (nextAttackIsMelee)
            return currentDistance <= meleeDistance ? EnemyState.MeleeAttack : EnemyState.Move;
        // 射撃の順番では、距離によらず射撃準備が整うまで待つ。
        return shooter != null && !shooter.IsCoolingDown
            ? EnemyState.RangedAttack : EnemyState.Idle;
    }

    private bool isIntroPlaying;

    private bool isAttacking;
    private bool hasDamagedThisAttack;
    [Header("戦闘中の向き（ローカル回転）")]
    [SerializeField] private Vector3 leftRotation =
        new Vector3(0f, 180f, 0f);

    [SerializeField] private Vector3 rightRotation =
        new Vector3(0f, 0f, 0f);
    private AnimatorStateInfo stateInfo;
    [SerializeField] private SpecialShooter sp;

    [Header("Melee Damage")]
    [SerializeField, Range(0f, 1f)] private float meleeDamageStartNormalized = 0.3f;
    [SerializeField, Range(0f, 1f)] private float meleeDamageEndNormalized = 0.6f;

    [Header("Item Damage")]
    [SerializeField] private ItemDataBase itemDataBase;
    [SerializeField] private int gearItemId = 3;
    [SerializeField, Min(1)] private int requiredGearHits = 5;
    [SerializeField] private int depthAttackDropItemId = 7;
    [SerializeField] private Transform itemDropPoint;
    [SerializeField]private int currentGearHits;
    private bool depthAttackLocked;
    private bool depthAttackItemDropped;

    [Header("Defeat")]
    [SerializeField] private string defeatTriggerName = "Defeat";
    [SerializeField] private GameObject defeatEffectPrefab;
    [SerializeField] private Transform defeatEffectPoint;
    [SerializeField] private AudioClip defeatEffectClip;
    [SerializeField, Range(0f, 5f)] private float defeatEffectVolume = 1f;
    [SerializeField, Min(0f)] private float defeatDestroyDelay = 2f;
    private float defeatTimer;
    private bool defeatStarted;
    private BossBgmController bgmController;

   private void Awake()
    {
        worldItemLayer = LayerMask.NameToLayer(worldItemLayerName);
        if (worldItemLayer < 0)
            Debug.LogError($"レイヤー「{worldItemLayerName}」がありません。World Item Layer Nameを確認してください。", this);
        enemyData = GetComponent<EnemyController>();
        shooter = GetComponent<ShootEnemyContorller>();
        if (sp == null) sp = GetComponent<SpecialShooter>();
        rb = GetComponent<Rigidbody>();

        currentState = EnemyState.Idle;
        currentTimer = coolDownTimer;
        nextAttackIsMelee = true;
        normalAttacksSinceSpecial = 0;
        nextReactiveSpecialTime = 0f;
        approachSpeedMultiplier = 1;
        blockedShotCount = 0;
        counterSpecialPending = false;
        specialTimer = Mathf.Max(1f, specialAttackInterval);

        // BossControllerと同じGameObjectのAnimatorだけを取得する。
        anim = GetComponent<Animator>();

        if (anim == null)
        {
            Debug.LogError(
                "BossControllerと同じGameObjectにAnimatorを付けてください。",
                this
            );
            enabled = false;
            return;
        }

        if (!HasBossParameters(anim))
        {
            Debug.LogError(
                "同じGameObjectのAnimatorを有効にし、Controllerに" +
                "Trigger型のIdle・Run・Attack・Specialを設定してください。",
                this
            );
            enabled = false;
            return;
        }

        battleAnimatorSpeed = anim.speed > 0f ? anim.speed : 1f;
    }

    private void Start()
    {
        hasStarted = true;
        battleStateApplied = false;
        ApplyBattleState();
    }

    private void OnEnable()
    {
        // 無効化・再有効化された場合も現在のisBattledを反映する。
        battleStateApplied = false;
    }

    private static bool HasBossParameters(Animator animator)
    {
        if (animator == null ||
            !animator.enabled ||
            !animator.gameObject.activeInHierarchy ||
            animator.runtimeAnimatorController == null)
        {
            return false;
        }

        bool hasIdle = false;
        bool hasRun = false;
        bool hasAttack = false;
        bool hasSpecial = false;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type != AnimatorControllerParameterType.Trigger)
            {
                continue;
            }

            hasIdle |= parameter.name == "Idle";
            hasRun |= parameter.name == "Run";
            hasAttack |= parameter.name == "Attack";
            hasSpecial |= parameter.name == "Special";
        }

        return hasIdle && hasRun && hasAttack && hasSpecial;
    }

    void Update()
    {

        if (isIntroPlaying)
        {
            return;
        }
        // 撃破開始後は戦闘フラグによらず死亡演出を最後まで進める。
        if (!defeatStarted)
        {
            if (StopIfPlayerMissing()) return;
            ApplyBattleState();
            if (!battleStateApplied || !isBattled || player == null) return;
            currentDistance = GetDistancePlayer();
            if (currentState != EnemyState.DepthAttack && !depthAttackLocked && sp != null)
                specialTimer = Mathf.Max(0f, specialTimer - Time.deltaTime);
            StartReactiveSpecialIfReady();
        }

        switch (currentState)
        {
            case EnemyState.Idle:
            case EnemyState.Move:
                currentState = ChooseNextAction();
                break;

            case EnemyState.MeleeAttack:
            {

                // UpdateだけでMoveを通過しても、近接攻撃の開始を取りこぼさない。
                if (previousState != EnemyState.MeleeAttack)
                {
                    BeginMeleeAttack();
                    break;
                }
                stateInfo = anim.GetCurrentAnimatorStateInfo(0);

                if (waitingForMeleeAnimation)
                {
                    // 前回のAttackの終端を新しい攻撃の終了と誤認しない。
                    if (!anim.IsInTransition(0) && stateInfo.IsName("Attack") &&
                        stateInfo.normalizedTime < 0.95f)
                        waitingForMeleeAnimation = false;
                    else
                        break;
                }

                if (stateInfo.IsName("Attack") && !anim.IsInTransition(0))
                {
                    float animationTime = stateInfo.normalizedTime;
                    isAttacking =
                        animationTime >= meleeDamageStartNormalized &&
                        animationTime <= meleeDamageEndNormalized;
                }
                else
                {
                    isAttacking = false;
                }

                // Attackアニメーションがほぼ終了したらCooldownへ移る
                if (stateInfo.IsName("Attack") &&
                    stateInfo.normalizedTime >= 0.95f)
                {
                    isAttacking = false;
                    isShoot = false;
                    nextAttackIsMelee = false;
                    normalAttacksSinceSpecial++;
                    currentTimer = coolDownTimer;
                    currentState = EnemyState.CoolDown;
                    // 次回の攻撃では必ずAttackトリガーを再発行する。
                    previousState = EnemyState.CoolDown;
                }
                

                break;
            }

            case EnemyState.RangedAttack:
            {
                stateInfo = anim.GetCurrentAnimatorStateInfo(0);

                // 1回分の射撃アニメーションが終了
                if (isShoot &&
                    stateInfo.IsName("Shoot") &&
                    !anim.IsInTransition(0) &&
                    stateInfo.normalizedTime >= 0.95f)
                {
                    isShoot = false;
                    if (random <= 0)
                    {
                        nextAttackIsMelee = true;
                        normalAttacksSinceSpecial++;
                        currentTimer = coolDownTimer;
                        currentState = EnemyState.CoolDown;
                        previousState = EnemyState.CoolDown;
                    }
                }

                break;
            }

            case EnemyState.CoolDown:

                currentTimer -= Time.deltaTime;

                if (currentTimer <= 0f)
                {
                    currentTimer = coolDownTimer;

                    currentState = ChooseNextAction();

                }

                break;

            case EnemyState.DepthAttack:
                if (previousState != EnemyState.DepthAttack) break;
                stateInfo = anim.GetCurrentAnimatorStateInfo(0);
                if (waitingForSpecialAnimation)
                {
                    if (!anim.IsInTransition(0) && stateInfo.IsName("Special") &&
                        stateInfo.normalizedTime < 0.95f)
                        waitingForSpecialAnimation = false;
                    else
                        break;
                }
                if (stateInfo.IsName("Special"))
                {
                    if (!isSpecialShot &&
                        !anim.IsInTransition(0) &&
                        stateInfo.normalizedTime >= 0.95f)
                    {
                        //Debug.Log("発射するぞ");
                        sp.EnergyBallShoot();
                        specialTimer = Mathf.Max(1f, specialAttackInterval);
                        normalAttacksSinceSpecial = 0;
                        nextReactiveSpecialTime = Time.time + reactiveSpecialCooldown;
                        currentTimer = coolDownTimer;
                        currentState = EnemyState.CoolDown;
                        previousState = EnemyState.CoolDown;
                        isSpecialShot = true;
                        isSpecialStarted = false;
                    }
                }
                else if (!isSpecialStarted)
                {
                    isSpecialShot = false;
                    currentState = currentDistance > idleDistance
                        ? EnemyState.Idle
                        : EnemyState.Move;
                }

                break;

            case EnemyState.Defeat:
                if (!defeatStarted)
                {
                    break;
                }

                defeatTimer -= Time.deltaTime;
                if (defeatTimer <= 0f)
                {
                    if (defeatEffectPrefab != null && defeatEffectClip != null)
                    {
                        Vector3 effectPosition = defeatEffectPoint != null
                            ? defeatEffectPoint.position
                            : transform.position;

                        AudioSource.PlayClipAtPoint(
                            defeatEffectClip,
                            effectPosition,
                            Mathf.Clamp(defeatEffectVolume, 0f, 5f));
                    }

                    if (bgmController != null)
                    {
                        bgmController.PlayVictoryBgm();
                    }
                    Destroy(gameObject);
                }
                break;
        }
    }

    void FixedUpdate()
    {
        if (isIntroPlaying)
        {
            return;
        }
        if (defeatStarted) return;
        if (StopIfPlayerMissing()) return;
        ApplyBattleState();
        if (!battleStateApplied || !isBattled || player == null)
        {
            StopMovement();
            return;
        }

        currentDistance = GetDistancePlayer();
        direction = Math.Sign(player.position.z - transform.position.z);
        //Debug.Log(direction);
        switch (currentState)
        {
            
            case EnemyState.Idle:

                StopMovement();
                PlayerDirection();

                // Idleへ入ったときだけTriggerを呼ぶ
                if (previousState != EnemyState.Idle)
                {
                    anim.SetTrigger("Idle");
                    previousState = EnemyState.Idle;
                }
                

                break;

            case EnemyState.Move:
            {
                PlayerDirection();
                // Moveへ入ったときだけTriggerを呼ぶ
                if (previousState != EnemyState.Move)
                {
                    // 接近開始時に1回だけ抽選し、接近中の速度を一定にする。
                    approachSpeedMultiplier = UnityEngine.Random.value < doubleSpeedChance ? 2 : 1;
                    anim.ResetTrigger("Idle");
                    anim.SetTrigger("Run");
                    previousState = EnemyState.Move;
                    isShoot = false;
                    if (moveClip != null)
                    {
                        AudioSource.PlayClipAtPoint(moveClip, transform.position,moveVolume);
                    }
                }

                currentSpeed = moveSpeed * approachSpeedMultiplier;

                

                Vector3 velocity = rb.linearVelocity;
                velocity.x = 0f;
                velocity.z = direction * currentSpeed;
                rb.linearVelocity = velocity;
                

                break;
            }

            case EnemyState.MeleeAttack:
                PlayerDirection();
                // 攻撃開始前に停止
                StopMovement();

                // MeleeAttackへ入ったときだけTriggerを呼ぶ
                if (previousState != EnemyState.MeleeAttack)
                {
                    BeginMeleeAttack();
                }

                break;

            case EnemyState.RangedAttack:
                PlayerDirection();
                StopMovement();

                // RangedAttackへ入った瞬間にだけ射撃回数を決める
                if (previousState != EnemyState.RangedAttack)
                {
                    // 整数版は最大値を含まないので1～3なら4を指定
                    random = UnityEngine.Random.Range(1, 4);

                    isShoot = false;
                    previousState = EnemyState.RangedAttack;

                    Debug.Log("射撃回数：" + random);
                }

                // 前の射撃が終了し、ShootEnemyContorllerも発射可能なら次を撃つ
                if (!isShoot &&
                    shooter != null &&
                    !shooter.IsCoolingDown)
                {
                    /*if (shooter.TryShoot())
                    {
                        // 同じShootステートを先頭から再生する
                        anim.Play("Shoot", 0, 0f);
                        isShoot = true;
                    }*/
                    // 成功した1発だけを数える。クールダウン中の連続呼び出しはしない。
                    if (shooter.TryShoot())
                    {
                        random--;
                        anim.ResetTrigger("Idle");
                        anim.ResetTrigger("Run");
                        anim.Play("Shoot", 0, 0f);
                        isShoot = true;
                    }
                }

                break;

            case EnemyState.DepthAttack:
            if(previousState != EnemyState.DepthAttack)
                {
                    StopMovement();
                    //PlayerDirection();
                    anim.ResetTrigger("Idle");
                    anim.ResetTrigger("Run");
                    if (anim.HasState(0, Animator.StringToHash("Base Layer.Special")))
                        anim.Play("Base Layer.Special", 0, 0f);
                    else
                        anim.SetTrigger("Special");
                    waitingForSpecialAnimation = true;
                    previousState = EnemyState.DepthAttack;
                    //Debug.Log("準備するぞ");
                    isSpecialStarted = true;
                    isSpecialShot = false;
                    // 開始時のプレイヤー位置に近い発射地点を複数選ぶ。
                    if (!sp.EnergyBallChargeNearest(player.position))
                    {
                        isSpecialStarted = false;
                        specialTimer = Mathf.Max(1f, specialAttackInterval);
                        normalAttacksSinceSpecial = 0;
                        nextReactiveSpecialTime = Time.time + reactiveSpecialCooldown;
                        currentTimer = coolDownTimer;
                        currentState = EnemyState.CoolDown;
                        previousState = EnemyState.CoolDown;
                    }
                    //Debug.Log(direction+"のほうを向いてるよ");
                }
                
                break;

            case EnemyState.CoolDown:

                StopMovement();
                PlayerDirection();
                previousState = EnemyState.CoolDown;
                break;

            case EnemyState.Defeat:

                StopMovement();
                break;
        }
    }

    private void BeginMeleeAttack()
    {
        StopMovement();
        anim.ResetTrigger("Idle");
        anim.ResetTrigger("Run");
        anim.ResetTrigger("Special");
        anim.ResetTrigger("Attack");
        // このスクリプトが既に参照しているAttackステートを先頭から再生。
        // 自己遷移がないAnimatorでも2回目以降の近接攻撃を開始できる。
        if (anim.HasState(0, Animator.StringToHash("Base Layer.Attack")))
            anim.Play("Base Layer.Attack", 0, 0f);
        else
            anim.SetTrigger("Attack");

        if (meleeAttackClip != null)
        {
            AudioSource.PlayClipAtPoint(meleeAttackClip, transform.position,meleeAttackVolume);
        }

        previousState = EnemyState.MeleeAttack;
        waitingForMeleeAnimation = true;
        isAttacking = false;
        hasDamagedThisAttack = false;
    }

    private void ApplyBattleState()
    {
        if (isIntroPlaying)
        {
            return;
        }
        if (!hasStarted || defeatStarted || anim == null) return;
        if (battleStateApplied && lastBattleState == isBattled) return;
        // 無効・非アクティブ中は「適用済み」にせず、次フレームに再試行。
        if (!anim.enabled || !anim.gameObject.activeInHierarchy ||
            anim.runtimeAnimatorController == null) return;

        StopMovement();
        currentState = EnemyState.Idle;
        previousState = EnemyState.Idle;
        currentTimer = coolDownTimer;
        nextAttackIsMelee = true;
        normalAttacksSinceSpecial = 0;
        nextReactiveSpecialTime = 0f;
        approachSpeedMultiplier = 1;
        blockedShotCount = 0;
        counterSpecialPending = false;
        specialTimer = Mathf.Max(1f, specialAttackInterval);
        isAttacking = false;
        hasDamagedThisAttack = false;
        waitingForMeleeAnimation = false;
        isShoot = false;
        isSpecialStarted = false;
        isSpecialShot = false;
        if (sp != null) sp.EnergyBallDestroy();

        anim.ResetTrigger("Attack");
        anim.ResetTrigger("Run");
        anim.ResetTrigger("Special");
        anim.ResetTrigger("Idle");
        anim.speed = battleAnimatorSpeed;
        // 開始時のIdleトリガーを移動・射撃まで持ち越さない。
        int idleState = Animator.StringToHash("Base Layer.Idle");
        if (anim.HasState(0, idleState))
        {
            anim.Play(idleState, 0, 0f);
            anim.Update(0f);
        }
        else
        {
            anim.SetTrigger("Idle");
            anim.Update(0f);
            anim.ResetTrigger("Idle");
        }
        anim.speed = isBattled ? battleAnimatorSpeed : 0f;

        // 初期化の反映が終わってから記録する。
        lastBattleState = isBattled;
        battleStateApplied = true;
    }

    private float GetDistancePlayer()
    {

        return player == null
            ? float.PositiveInfinity
            : Math.Abs(transform.position.z - player.position.z);
    }

    private bool StopIfPlayerMissing()
    {
        if (player != null) return false;

        isAttacking = false;
        SetBattled(false);
        StopMovement();
        return true;
    }

    private void StopMovement()
    {
        if (rb == null || rb.isKinematic) return;
        Vector3 velocity = rb.linearVelocity;
        velocity.x = 0f;
        velocity.z = 0f;
        rb.linearVelocity = velocity;
    }
    private void PlayerDirection()
    {
        // 移動方向に応じた目標角度
        Quaternion targetRotation =
            direction < 0f
                ? Quaternion.Euler(leftRotation)
                : Quaternion.Euler(rightRotation);

        // Y軸方向を滑らかに反転
        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            Mathf.Clamp01(lerpTime * Time.fixedDeltaTime)
        );
        //Debug.Log(transform.localEulerAngles);
    }

    private void OnCollisionEnter(Collision collision)
    {
        CheckWorldItemContact(collision.collider);
        ItemGimmick hitItem = collision.gameObject.GetComponent<ItemGimmick>();
        if (hitItem != null &&
            collision.gameObject.CompareTag("Item") &&
            (hitItem.IsThrown || hitItem.IsAttached))
        {
            ReceiveItemHit(hitItem);
            return;
        }

        TryDamagePlayer(collision.gameObject);
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        CheckWorldItemContact(collision.collider);
        TryDamagePlayer(collision.gameObject);
    }

    private void TryDamagePlayer(GameObject target)
    {
        if (player == null || target == null || !isBattled || defeatStarted ||
            currentState != EnemyState.MeleeAttack ||
            !isAttacking ||
            hasDamagedThisAttack)
        {
            return;
        }

        PlayerManager playerManager = target.GetComponentInParent<PlayerManager>();
        if (playerManager == null)
        {
            return;
        }

        playerManager.SetPlayerHp(BossAttackDamage);
        hasDamagedThisAttack = true;
    }

    // EnemyControllerからボスへの命中処理を委譲するときにも使用する。
    public void ReceiveItemHit(ItemGimmick item)
    {
        if (!isBattled || item == null || defeatStarted)
        {
            return;
        }

        bool wasThrown = item.IsThrown;

        if (item.Id == gearItemId && wasThrown && !depthAttackLocked)
        {
            currentGearHits++;
            Debug.Log($"歯車命中：{currentGearHits}/{requiredGearHits}", this);

            if (currentGearHits >= requiredGearHits)
            {
                Debug.Log("武器落します");
                LockDepthAttackAndDropItem();
            }
        }

        if (itemDataBase == null || enemyData == null)
        {
            Debug.LogError(
                "BossControllerのItemDataBaseまたはEnemyControllerが設定されていません。",
                this);
            return;
        }

        int damage = itemDataBase.GetAttackById(item.Id);
        int hpBeforeHit = enemyData.EnemyHp;

        enemyData.SetEnemyHp(damage);
        if (itemHitClip != null)
        {
            AudioSource.PlayClipAtPoint(itemHitClip, transform.position,itemHitVolume);
        }

        // 攻撃値と残りHPが同じ場合もHPが0になるため撃破扱いにする。
        if (damage >= hpBeforeHit)
        {
            StartDefeat();
        }

        if (wasThrown)
        {
            Destroy(item.gameObject);
        }
    }

    private void LockDepthAttackAndDropItem()
    {
        if (depthAttackLocked)
        {
            return;
        }

        depthAttackLocked = true;
        isSpeciealized = false;

        if (currentState == EnemyState.DepthAttack)
        {
            if (sp != null)
            {
                sp.EnergyBallDestroy();
            }

            isSpecialStarted = false;
            isSpecialShot = false;
            currentState = currentDistance > idleDistance
                ? EnemyState.Idle
                : EnemyState.Move;
        }

        if (depthAttackItemDropped)
        {
            return;
        }

        if (itemDataBase == null)
        {
            Debug.LogError("BossControllerにItemDataBaseが設定されていません。", this);
            return;
        }

        GameObject dropPrefab = itemDataBase.GetIntanceById(depthAttackDropItemId);
        if (dropPrefab == null)
        {
            Debug.LogError(
                $"ドロップ用アイテムID {depthAttackDropItemId} のPrefabが見つかりません。",
                this);
            return;
        }

        Vector3 dropPosition = itemDropPoint != null
            ? itemDropPoint.position
            : transform.position + Vector3.up;

        GameObject droppedItem = Instantiate(
            dropPrefab,
            dropPosition,
            dropPrefab.transform.rotation);

        ItemGimmick droppedItemGimmick = droppedItem.GetComponent<ItemGimmick>();
        if (droppedItemGimmick != null)
        {
            droppedItemGimmick.Initialize(depthAttackDropItemId);
            droppedItemGimmick.SetAttach(false);
            droppedItemGimmick.SetThrown(false);

            // Initialize内で座標が変更されるアイテムにも対応する。
            droppedItem.transform.position = dropPosition;
        }

        depthAttackItemDropped = true;
        Debug.Log("DepthAttackを封印し、専用アイテムをドロップしました。", this);
    }

    private void StartDefeat()
    {
        if (defeatStarted)
        {
            return;
        }
        // BossController.cs の StartDefeat() 内に追加する例
        bgmController = FindFirstObjectByType<BossBgmController>();
        if (bgmController != null)
        {
            bgmController.StartBgmFadeOut();
        }

        defeatStarted = true;
        defeatTimer = defeatDestroyDelay;
        currentState = EnemyState.Defeat;
        StopMovement();

        if (sp != null)
        {
            sp.EnergyBallDestroy();
        }

        if (anim != null && !string.IsNullOrEmpty(defeatTriggerName))
        {
            anim.SetTrigger(defeatTriggerName);
        }

        if (defeatEffectPrefab != null)
        {
            Vector3 effectPosition = defeatEffectPoint != null
                ? defeatEffectPoint.position
                : transform.position;

            Instantiate(
                defeatEffectPrefab,
                effectPosition,
                Quaternion.identity);
        }

        foreach (Collider bossCollider in GetComponentsInChildren<Collider>())
        {
            bossCollider.enabled = false;
        }

        rb.isKinematic = true;
    }
    private void OnCollisionExit(Collision collision) {

        //rb.isKinematic = false;
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }

    public void BeginIntro()
    {
        isIntroPlaying = true;
        isBattled = false;
        battleStateApplied = false;

        StopMovement();

        isAttacking = false;
        hasDamagedThisAttack = false;
        waitingForMeleeAnimation = false;

        if (sp != null)
        {
            sp.EnergyBallDestroy();
        }

        // 通常は戦闘前に0になるため、登場演出では戻す。
        if (anim != null)
        {
            anim.speed = battleAnimatorSpeed;
        }
    }

    public void EndIntro(bool startBattle)
    {
        isIntroPlaying = false;
        battleStateApplied = false;
        SetBattled(startBattle);
    }

    // 引数なし：既存の呼び出し用
    public void SetBattled()
    {
        SetBattled(true);
    }
    // bool引数あり：戦闘状態を変更する
    public void SetBattled(bool value)
    {
        if (isIntroPlaying && value)
        {
            return;
        }

        isBattled = value;

        if (hasStarted && isActiveAndEnabled)
        {
            ApplyBattleState();
        }
    }
    
    public int CurrentStateCode
    {
        get
        {
            if (player == null) return 0;
            return currentState switch
            {
                EnemyState.Idle     => 0,
                EnemyState.Move    => 1,
                EnemyState.MeleeAttack   => 2,
                _ => -1
            };
        }
    }
    public float DirectionToPlayer
    {
        get
        {
            if (player == null) return 0f;
            return Mathf.Sign(
                player.position.z - transform.position.z
            );
        }
    }
    public Vector3 CrossDirectionToPlayer
    {
        get
        {
            if (player == null) return Vector3.zero;
            Vector3 a = player.position - transform.position;
            a.x = 0f;
            return a.normalized;
            
            
        }
    }
        
}
