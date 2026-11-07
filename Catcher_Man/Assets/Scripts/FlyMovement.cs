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
    [SerializeField] private AudioClip shootStateClip;
    private ShootEnemyContorller shooter;
    private int currentSpeed;

    [Header("移動中のループ音")]
    [SerializeField] private AudioClip moveLoopClip;
    [SerializeField, Range(0f, 1f)] private float moveVolume = 1f;
    [SerializeField, Min(0.1f)] private float moveSoundMinDistance = 10f;
    [SerializeField, Min(0.1f)] private float moveSoundMaxDistance = 100f;
    [SerializeField, Min(0f)] private float moveSoundThreshold = 0.02f;
    private AudioSource moveAudioSource;
    private Vector3 previousAudioPosition;

    void Awake()
    {
        GameObject soundObject = new GameObject("FlyMovementAudio");
        soundObject.transform.SetParent(transform, false);
        moveAudioSource = soundObject.AddComponent<AudioSource>();
        moveAudioSource.playOnAwake = false;
        moveAudioSource.loop = true;
        moveAudioSource.spatialBlend = 1f;
        moveAudioSource.dopplerLevel = 0f;
        moveAudioSource.rolloffMode = AudioRolloffMode.Linear;
        rb = GetComponent<Rigidbody>();
        rb.useGravity =  false;
        this.transform.position += new Vector3(0,addHeight,0);
        enemyData = this.GetComponent<EnemyController>();
        currentState = EnemyState.Idle;
        rb = GetComponent<Rigidbody>();

    }

    void Update()
    {
        if (StopIfPlayerMissing()) return;

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
                    if (shootStateClip != null)
                    {
                        AudioSource.PlayClipAtPoint(shootStateClip, transform.position);
                    }
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
                //Debug.Log("今"+currentSpeed+"で"+battleDistance+"になるまで走行中");
                break;
        }
    }
    private void OnEnable()
    {
        previousAudioPosition = transform.position;
    }

    private void LateUpdate()
    {
        Vector3 position = transform.position;
        float distance = Vector3.Distance(position, previousAudioPosition);
        previousAudioPosition = position;
        if (moveAudioSource == null) return;

        moveAudioSource.volume = moveVolume;
        moveAudioSource.minDistance = Mathf.Max(0.1f, moveSoundMinDistance);
        moveAudioSource.maxDistance = Mathf.Max(moveAudioSource.minDistance, moveSoundMaxDistance);
        if (moveAudioSource.clip != moveLoopClip)
        {
            moveAudioSource.Stop();
            moveAudioSource.clip = moveLoopClip;
        }

        bool moving = Time.timeScale > 0f && player != null &&
                      moveLoopClip != null &&
                      ((rb != null && !rb.isKinematic &&
                        rb.linearVelocity.sqrMagnitude > moveSoundThreshold * moveSoundThreshold) ||
                       distance > moveSoundThreshold * Mathf.Max(Time.deltaTime, 0.0001f));
        if (moving)
        {
            if (!moveAudioSource.isPlaying) moveAudioSource.Play();
        }
        else
        {
            moveAudioSource.Stop();
        }
    }

    private void OnDisable()
    {
        if (moveAudioSource != null) moveAudioSource.Stop();
    }

    // UpdateだけでなくFixedUpdateからも確認する。
    private bool StopIfPlayerMissing()
    {
        if (player != null) return false;

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
        if (player == null || enemyData == null) return;
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerManager manager =
                collision.gameObject.GetComponentInParent<PlayerManager>();
            if (manager != null)
                manager.SetPlayerHp(enemyData.EnemyAttackDamage);
        }
        
    }
    public bool CurrentCooldown
    {
        get
        {
            return player == null || currentState == EnemyState.Cooldown;
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

