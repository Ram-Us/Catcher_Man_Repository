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
    [SerializeField] private float idleDistance,moveDistance,meleeDistance,rangedDistance,jumpPower,coolDownTimer,lerpTime,specialInterval,specialTimer;
    private float currentDistance,currentTimer,direction;
    [SerializeField] private Transform player;
    [SerializeField] private int moveSpeed,bossAttackDamage,bossSpecialDamage;
    public int BossAttackDamage => bossAttackDamage;
    public int BossSpecialDamage => bossSpecialDamage;
    private int currentSpeed;
    [SerializeField] private int random;
    private Rigidbody rb;
    [SerializeField]private Animator anim;
    [SerializeField] private bool isGrounded = false,isShoot = false,isSpeciealized = true,isStopped,isSpecialStarted = false,isSpecialShot = false;
    public bool IsSpecialized => isSpeciealized;
    private bool isAttacking;
    private bool hasDamagedThisAttack;
    private Vector3 leftRotation = new Vector3(0f,180f,0f),rightRotation = new Vector3(0f,0f,0f);
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
    [SerializeField, Min(0f)] private float defeatDestroyDelay = 2f;
    private float defeatTimer;
    private bool defeatStarted;

    void Awake()
    {
        enemyData = gameObject.GetComponent<EnemyController>();
        shooter = GetComponent<ShootEnemyContorller>();
        currentState = EnemyState.Idle;
        rb = GetComponent<Rigidbody>();
        currentTimer = coolDownTimer;
        specialTimer = specialInterval;

        if (!HasBossParameters(anim))
        {
            anim = null;
            foreach (Animator childAnimator in GetComponentsInChildren<Animator>(true))
            {
                if (HasBossParameters(childAnimator))
                {
                    anim = childAnimator;
                    break;
                }
            }
        }

        if (anim == null)
        {
            Debug.LogError(
                "BossController could not find an enabled child Animator with the boss controller parameters (Idle, Run, Attack, Special). Check the Animator's Controller and rig hierarchy.",
                this);
            enabled = false;
            return;
        }
        
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
        
        currentDistance = GetDistancePlayer();
        

        
        switch (currentState)
        {
            case EnemyState.Idle:

                // プレイヤーが行動範囲へ入ったら距離に応じて行動する
                 if (currentDistance < moveDistance)
                {
                    currentState = EnemyState.Move;
                }
                else if (currentDistance <= meleeDistance)
                {
                    currentState = EnemyState.MeleeAttack;
                }
                else if (currentDistance <= rangedDistance &&
                        shooter != null)
                {
                    currentState = EnemyState.RangedAttack;
                }
                else if (currentDistance <= idleDistance)
                {
                    currentState = EnemyState.Move;
                }

                break;

            case EnemyState.Move:

                if (currentDistance <= meleeDistance)
                {
                    currentState = EnemyState.MeleeAttack;
                }
                else if (currentDistance >= moveDistance &&
                        currentDistance <= rangedDistance &&
                        shooter != null &&
                        !shooter.IsCoolingDown && !(isShoot))
                {
                    currentState = EnemyState.RangedAttack;
                }
                else if (currentDistance > idleDistance)
                {
                    currentState = EnemyState.Idle;
                }

                // 上記のどれにも該当しなければMoveを継続する
                break;

            case EnemyState.MeleeAttack:
            {

                stateInfo = anim.GetCurrentAnimatorStateInfo(0);

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
                    currentTimer = coolDownTimer;
                    currentState = EnemyState.CoolDown;
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
                    currentState = EnemyState.Move;
                }

                break;
            }

            case EnemyState.CoolDown:

                currentTimer -= Time.deltaTime;

                if (currentTimer <= 0f)
                {
                    currentTimer = coolDownTimer;

                    if (currentDistance > idleDistance)
                    {
                        currentState = EnemyState.Idle;
                    }
                    else
                    {
                        currentState = EnemyState.Move;
                    }

                }

                break;

            case EnemyState.DepthAttack:
                stateInfo = anim.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("Special"))
                {
                    if (!isSpecialShot &&
                        !anim.IsInTransition(0) &&
                        stateInfo.normalizedTime >= 0.95f)
                    {
                        //Debug.Log("発射するぞ");
                        sp.EnergyBallShoot();
                        currentState = EnemyState.Idle;
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
                    Destroy(gameObject);
                }
                break;
        }
    }

    void FixedUpdate()
    {
        if (!depthAttackLocked &&
            currentState != EnemyState.DepthAttack &&
            currentState != EnemyState.Defeat)
        {
            specialTimer -= Time.fixedDeltaTime;
            if (specialTimer <= 0f)
            {
                //Debug.Log("必殺技発動");
                currentState = EnemyState.DepthAttack;
                specialTimer = specialInterval;
            }
        }
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
                    anim.SetTrigger("Run");
                    previousState = EnemyState.Move;
                    isShoot = false;
                }

                currentSpeed = moveSpeed;

                

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
                    anim.SetTrigger("Attack");
                    previousState = EnemyState.MeleeAttack;
                    isAttacking = false;
                    hasDamagedThisAttack = false;
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
                    for(int i = 0;i < random; i++)
                    {
                        
                        shooter.TryShoot();
                        if (i == 0)
                        {
                            anim.Play("Shoot", 0, 0f);
                            isShoot = true;
                        }
                    }
                }

                break;

            case EnemyState.DepthAttack:
            if(previousState != EnemyState.DepthAttack)
                {
                    StopMovement();
                    //PlayerDirection();
                    anim.SetTrigger("Special");
                    previousState = EnemyState.DepthAttack;
                    //Debug.Log("準備するぞ");
                    isSpecialStarted = true;
                    isSpecialShot = false;
                    sp.EnergyBallCharge();
                    //Debug.Log(direction+"のほうを向いてるよ");
                }
                
                break;

            case EnemyState.CoolDown:

                StopMovement();
                PlayerDirection();
                // previousStateは変更しない
                // 直前がMeleeかRangedかを保持するため
                break;

            case EnemyState.Defeat:

                StopMovement();
                break;
        }
    }

    private float GetDistancePlayer()
    {

        return Math.Abs(transform.position.z - player.position.z);
    }

    private void StopMovement()
    {
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
        TryDamagePlayer(collision.gameObject);
    }

    private void TryDamagePlayer(GameObject target)
    {
        if (currentState != EnemyState.MeleeAttack ||
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
        if (item == null || defeatStarted)
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

    
    
    public float DirectionToPlayer
    {
        get
        {
            return Mathf.Sign(
                player.position.z - transform.position.z
            );
        }
    }
    public Vector3 CrossDirectionToPlayer
    {
        get
        {
            Vector3 a = player.position - transform.position;
            a.x = 0f;
            return a.normalized;
            
            
        }
    }
        
}
