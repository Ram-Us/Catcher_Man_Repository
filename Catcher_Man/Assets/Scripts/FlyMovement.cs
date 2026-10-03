using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;
public class FlyMovement : MonoBehaviour,IEnemyShootProvider,IEnemyProvider
{
    [SerializeField] private int addHeight;
    private Rigidbody rb;
    private EnemyController enemyData;

    private enum EnemyState
    {
        Idle,
        Chase,
        Shoot,
        Cooldown
    }
    private EnemyState currentState;
    [SerializeField] private float idleDistance,battleDistance;
    private float currentDistance;
    [SerializeField] private Transform player;
    [SerializeField] private int chaseSpeed;
    private ShootEnemyContorller shooter;
    private int currentSpeed;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity =  false;
        this.transform.position += new Vector3(0,addHeight,0);
        enemyData = this.GetComponent<EnemyController>();
        currentState = EnemyState.Idle;
        rb = GetComponent<Rigidbody>();

    }

    void Update()
    {
        currentDistance = GetDistancePlayer();
        switch (currentState)
        {
            
            case EnemyState.Idle:
                
                if(currentDistance < idleDistance)
                {   
                    currentState = EnemyState.Chase;
                }

                break;
            case EnemyState.Chase:

                if(currentDistance < battleDistance)
                {
                    currentState = EnemyState.Shoot;
                }
                else if(currentDistance > idleDistance)
                {
                    currentState = EnemyState.Idle;
                }

                break;

            case EnemyState.Shoot:
                shooter = GetComponent<ShootEnemyContorller>();
                if (shooter != null && shooter.TryShoot())
                {
                    currentState = EnemyState.Cooldown;
                }
                else if (currentDistance > battleDistance)
                {
                    currentState = EnemyState.Chase;
                }
                break;
            
            case EnemyState.Cooldown:
                if (currentDistance > battleDistance)
                {
                    currentState = EnemyState.Chase;
                }
                else if (currentDistance > idleDistance)
                {
                    currentState = EnemyState.Idle;
                }
                else if (shooter != null && !shooter.IsCoolingDown)
                {
                    currentState = EnemyState.Shoot;
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
                Debug.Log(direction);
                //Debug.Log("今"+currentSpeed+"で"+battleDistance+"になるまで走行中");
                break;

            case EnemyState.Shoot:
                

                break;
            case EnemyState.Cooldown:
                currentSpeed = chaseSpeed;
                direction = Math.Sign(player.position.z - transform.position.z);
                velocity =  rb.linearVelocity;
                velocity.z = -direction * currentSpeed;
                velocity.x = 0;
                rb.linearVelocity = velocity;
                Debug.Log(direction);
                Debug.Log("今"+currentSpeed+"で"+battleDistance+"になるまで走行中");
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
        }
        
    }
    public bool CurrentCooldown
    {
        get
        {
            return currentState == EnemyState.Cooldown;
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
                EnemyState.Shoot   => 2,
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

    
        
    }


