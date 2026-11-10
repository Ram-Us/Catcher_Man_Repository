using UnityEngine;
using System;
using System.Runtime.InteropServices.WindowsRuntime;

public class LeapEnemyController : MonoBehaviour,IEnemyProvider
{
    private EnemyController enemyData;
    private enum EnemyState
    {
        Idle,
        Chase,
        Leap,
        Cooldown
    }
    private EnemyState currentState;
    [SerializeField] private float idleDistance,battleDistance,jumpPower,leapSpeed,coolDownTimer;
    [SerializeField] private AudioClip leapAttackClip;
    private float currentDistance,currentTimer;
    [SerializeField] private Transform player;
    [SerializeField] private int chaseSpeed;
    private int currentSpeed;
    private Rigidbody rb;
    [SerializeField] private bool isGrounded = false,isColided = false,ShooterType = false;
    private bool leapRequested = false;
    private bool isAttacking;
    private Animator anim;

    void Awake()
    {
        enemyData = this.GetComponent<EnemyController>();
        currentState = EnemyState.Idle;
        rb = GetComponent<Rigidbody>();
        currentTimer = coolDownTimer;
        anim = GetComponent<Animator>();

    }

    // Update is called once per frame
    void Update()
    {
        if (StopIfPlayerMissing()) return;

        currentDistance = GetDistancePlayer();
        switch (currentState)
        {
            
            case EnemyState.Idle:
                
                if(currentDistance < idleDistance)
                {   
                    //Debug.Log("チェイスへ");
                    currentState = EnemyState.Chase;
                    anim.SetTrigger("Chase");
                    
                }

                break;
            case EnemyState.Chase:

                if(currentDistance < battleDistance)
                {
                    //Debug.Log("バトルへ");
                    anim.SetTrigger("Leap");
                    currentState = EnemyState.Leap;
                    leapRequested = true;
                    
                    
                }
                else if(currentDistance > idleDistance)
                {
                    //Debug.Log("アイドルへ");
                    currentState = EnemyState.Idle;
                    anim.SetTrigger("Idle");
                }

                break;

            case EnemyState.Leap:

                if (!leapRequested)
                {
                    currentState = EnemyState.Cooldown;
                    anim.SetTrigger("Idle");
                }
                if(currentDistance > battleDistance)
                {
                    //Debug.Log("チェイスへ");
                    currentState = EnemyState.Chase;
                    anim.SetTrigger("Chase");
                }
                break;
            
            case EnemyState.Cooldown:
                if (isGrounded)
                {
                    //Debug.Log("クールダウン"+currentTimer);
                    currentTimer -= Time.deltaTime;
                    if(currentTimer <= 0f)
                    {
                        if(currentDistance > idleDistance)
                        {
                            //Debug.Log("アイドルへ");
                            currentState = EnemyState.Idle;
                            anim.SetTrigger("Idle");
                        }
                        else
                        {
                            currentState = EnemyState.Chase;
                            anim.SetTrigger("Chase");
                        }
                        currentTimer = coolDownTimer;
                        
                    }
                    
                }
                
                break;
        }

    }

    void FixedUpdate()
    {
        if (StopIfPlayerMissing()) return;

        float direction = Math.Sign(player.position.z - transform.position.z);
        if(direction <= -1)
        {
            transform.eulerAngles = new Vector3 (0f,90f,0f);
        }else if(direction >= 1)
        {
            transform.eulerAngles = new Vector3 (0f,270f,0f);
        }
        switch (currentState)
        {
            
            case EnemyState.Idle:
                currentSpeed = 0;
                //Debug.Log("今"+currentDistance+"に対して、アイドルは"+idleDistance);

                break;
            case EnemyState.Chase:
                currentSpeed = chaseSpeed;
                direction = Math.Sign(player.position.z - transform.position.z);
                Vector3 velocity =  rb.linearVelocity;
                velocity.z = direction * currentSpeed;
                velocity.x = 0;
                rb.linearVelocity = velocity;
                //Debug.Log(direction);
                //Debug.Log("今"+currentSpeed+"で"+battleDistance+"になるまで走行中");
                break;

            case EnemyState.Leap:
                if (ShooterType)
                {
                    ShootEnemyContorller shooter = GetComponent<ShootEnemyContorller>();
                    if (shooter != null && shooter.TryShoot())
                    {
                        currentState = EnemyState.Cooldown;
                    }
                    else if (currentDistance > battleDistance)
                    {
                        anim.SetTrigger("Chase");
                        currentState = EnemyState.Chase;
                    }
                }
                if (leapRequested)
                {
                    isAttacking = !ShooterType;
                    direction = Mathf.Sign(player.position.z - transform.position.z);
                    velocity = rb.linearVelocity;
                    velocity.x = 0f;
                    velocity.y = jumpPower;
                    velocity.z = direction * leapSpeed;
                    rb.linearVelocity = velocity;
                    if (!ShooterType && leapAttackClip != null)
                    {
                        AudioSource.PlayClipAtPoint(leapAttackClip, transform.position);
                    }
                    leapRequested = false;
                    /*Debug.Log(
                        $"Battle開始：request={leapRequested}, " +
                        $"kinematic={rb.isKinematic}, " +
                        $"constraints={rb.constraints}, " +
                        $"jump={jumpPower}, leap={leapSpeed}");*/

                }

                break;
            case EnemyState.Cooldown:
                
                
                break;
        }
    }

    // UpdateだけでなくFixedUpdateからも確認する。
    private bool StopIfPlayerMissing()
    {
        if (player != null) return false;

        leapRequested = false;
        isAttacking = false;
        isColided = false;
        if (anim != null && anim.runtimeAnimatorController != null &&
            currentState != EnemyState.Idle)
        {
            anim.ResetTrigger("Chase");
            anim.ResetTrigger("Leap");
            anim.SetTrigger("Idle");
        }
        currentState = EnemyState.Idle;
        currentSpeed = 0;
        if (rb != null && !rb.isKinematic)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            rb.linearVelocity = velocity;
        }
        return true;
    }

    private float GetDistancePlayer()
    {

        return player == null
            ? float.PositiveInfinity
            : Math.Abs(transform.position.z - player.position.z);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryDamagePlayer(collision.gameObject);
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            isAttacking = false;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        
        if(collision.gameObject.CompareTag("Player") && CurrentStateCode == 2)
        {
            Debug.Log("当たり続けてるよ");
            TryDamagePlayer(collision.gameObject);
        }
        
    }

    private void TryDamagePlayer(GameObject target)
    {
        if (player == null || target == null || enemyData == null ||
            !isAttacking || isColided || !target.CompareTag("Player"))
        {
            return;
        }

        PlayerManager playerManager = target.GetComponent<PlayerManager>();
        if (playerManager == null)
        {
            Debug.LogWarning("PlayerタグのオブジェクトにPlayerManagerがありません。", target);
            return;
        }
        playerManager.SetPlayerHp(enemyData.EnemyAttackDamage);
        isColided = true;
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

    public int CurrentStateCode
    {
        get
        {
            if (player == null) return 0;
            return currentState switch
            {
                EnemyState.Idle     => 0,
                EnemyState.Chase    => 1,
                EnemyState.Leap   => 2,
                EnemyState.Cooldown => 3,
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
        public void NotifyShotFired()
    {
        if (currentState != EnemyState.Leap)
        {
            return;
        }

        currentTimer = coolDownTimer;
        currentState = EnemyState.Cooldown;
    }
}
