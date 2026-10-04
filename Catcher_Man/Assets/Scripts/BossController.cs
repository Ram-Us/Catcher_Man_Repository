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
    [SerializeField] private float idleDistance,moveDistance,meleeDistance,rangedDistance,jumpPower,coolDownTimer,lerpTime;
    private float currentDistance,currentTimer;
    [SerializeField] private Transform player;
    [SerializeField] private int moveSpeed;
    private int currentSpeed,random;
    private Rigidbody rb;
    private Animator anim;
    [SerializeField] private bool isGrounded = false,isColided = false,isShoot = false;
    private Vector3 leftRotation = new Vector3(0f,180f,0f),rightRotation = new Vector3(0f,0f,0f);

    void Awake()
    {
        enemyData = gameObject.GetComponent<EnemyController>();
        shooter = GetComponent<ShootEnemyContorller>();
        currentState = EnemyState.Idle;
        rb = GetComponent<Rigidbody>();
        currentTimer = coolDownTimer;
        anim = GetComponent<Animator>();
    }
    void Update()
    {
        currentDistance = GetDistancePlayer();
        
        switch (currentState)
        {
            case EnemyState.Idle:

                // プレイヤーが行動範囲へ入ったら距離に応じて行動する
                if (currentDistance <= meleeDistance)
                {
                    currentState = EnemyState.MeleeAttack;
                }
                else if (currentDistance < moveDistance)
                {
                    currentState = EnemyState.Move;
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

                AnimatorStateInfo stateInfo =
                    anim.GetCurrentAnimatorStateInfo(0);
                // Attackアニメーションがほぼ終了したらCooldownへ移る
                if (stateInfo.IsName("Attack") &&
                    stateInfo.normalizedTime >= 0.95f)
                {
                    isShoot = false;
                    currentTimer = coolDownTimer;
                    currentState = EnemyState.CoolDown;
                }
                

                break;
            }

            case EnemyState.RangedAttack:
            {
                AnimatorStateInfo stateInfo =
                    anim.GetCurrentAnimatorStateInfo(0);

                // 1回分の射撃アニメーションが終了
                if (isShoot &&
                    stateInfo.IsName("Shoot") &&
                    !anim.IsInTransition(0) &&
                    stateInfo.normalizedTime >= 0.95f)
                {
                    random--;

                    if (random <= 0)
                    {
                        // MoveのFixedUpdateが実行されるまで再射撃を防ぐ
                        currentState = EnemyState.Move;
                    }
                    else
                    {
                        // 次の射撃を許可
                        isShoot = false;
                    }
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
                break;

            case EnemyState.Defeat:
                break;
        }
    }

    void FixedUpdate()
    {
        Debug.Log(currentDistance);
        switch (currentState)
        {
            
            case EnemyState.Idle:

                StopMovement();

                // Idleへ入ったときだけTriggerを呼ぶ
                if (previousState != EnemyState.Idle)
                {
                    anim.SetTrigger("Idle");
                    previousState = EnemyState.Idle;
                }

                break;

            case EnemyState.Move:
            {
                // Moveへ入ったときだけTriggerを呼ぶ
                if (previousState != EnemyState.Move)
                {
                    anim.SetTrigger("Run");
                    previousState = EnemyState.Move;
                    isShoot = false;
                }

                currentSpeed = moveSpeed;

                float direction =
                    Math.Sign(player.position.z - transform.position.z);

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

                Vector3 velocity = rb.linearVelocity;
                velocity.x = 0f;
                velocity.z = direction * currentSpeed;
                rb.linearVelocity = velocity;

                break;
            }

            case EnemyState.MeleeAttack:

                // 攻撃開始前に停止
                StopMovement();

                // MeleeAttackへ入ったときだけTriggerを呼ぶ
                if (previousState != EnemyState.MeleeAttack)
                {
                    anim.SetTrigger("Attack");
                    previousState = EnemyState.MeleeAttack;
                }

                break;

            case EnemyState.RangedAttack:

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
                    if (shooter.TryShoot())
                    {
                        // 同じShootステートを先頭から再生する
                        anim.Play("Shoot", 0, 0f);
                        isShoot = true;
                    }
                }

                break;

            case EnemyState.DepthAttack:

                StopMovement();
                break;

            case EnemyState.CoolDown:

                StopMovement();

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

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            int damage = this.gameObject.GetComponent<EnemyController>().EnemyAttackDamage;
            collision.gameObject.GetComponent<PlayerManager>().SetPlayerHp(damage);
            isColided = true;
        }
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }
    private void OnCollisionExit(Collision collision) {
        
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
        if (collision.gameObject.CompareTag("Player"))
        {
            isColided = false;
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
