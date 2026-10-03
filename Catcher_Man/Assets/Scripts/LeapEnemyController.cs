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
    private float currentDistance,currentTimer;
    [SerializeField] private Transform player;
    [SerializeField] private int chaseSpeed;
    private int currentSpeed;
    private Rigidbody rb;
    [SerializeField] private bool isGrounded = false,isColided = false,ShooterType = false;
    private bool leapRequested = false;

    void Awake()
    {
        enemyData = this.GetComponent<EnemyController>();
        currentState = EnemyState.Idle;
        rb = GetComponent<Rigidbody>();
        currentTimer = coolDownTimer;

    }

    // Update is called once per frame
    void Update()
    {
        currentDistance = GetDistancePlayer();
        switch (currentState)
        {
            
            case EnemyState.Idle:
                
                if(currentDistance < idleDistance)
                {   
                    //Debug.Log("チェイスへ");
                    currentState = EnemyState.Chase;
                    
                }

                break;
            case EnemyState.Chase:

                if(currentDistance < battleDistance)
                {
                    //Debug.Log("バトルへ");
                    currentState = EnemyState.Leap;
                    leapRequested = true;
                    
                }
                else if(currentDistance > idleDistance)
                {
                    //Debug.Log("アイドルへ");
                    currentState = EnemyState.Idle;
                }

                break;

            case EnemyState.Leap:

                if (!leapRequested)
                {
                    currentState = EnemyState.Cooldown;
                }
                if(currentDistance > battleDistance)
                {
                    //Debug.Log("チェイスへ");
                    currentState = EnemyState.Chase;
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
                        }
                        else
                        {
                            currentState = EnemyState.Chase;
                        }
                        currentTimer = coolDownTimer;
                        
                    }
                    
                }
                
                break;
        }
    }

    void FixedUpdate()
    {
        switch (currentState)
        {
            
            case EnemyState.Idle:
                currentSpeed = 0;
                //Debug.Log("今"+currentDistance+"に対して、アイドルは"+idleDistance);

                break;
            case EnemyState.Chase:
                currentSpeed = chaseSpeed;
                float direction = Math.Sign(player.position.z - transform.position.z);
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
                        currentState = EnemyState.Chase;
                    }
                }
                if (leapRequested)
                {
                    direction = Mathf.Sign(player.position.z - transform.position.z);
                    velocity = rb.linearVelocity;
                    velocity.x = 0f;
                    velocity.y = jumpPower;
                    velocity.z = direction * leapSpeed;
                    rb.linearVelocity = velocity;
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

    private float GetDistancePlayer()
    {

        return Math.Abs(transform.position.z - player.position.z);
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

    public int CurrentStateCode
    {
        get
        {
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
