using UnityEngine;
using System;
using UnityEngine.Rendering.Universal;
using Unity.AppUI.UI;

public class ShootEnemyContorller : MonoBehaviour
{   
    private EnemyController enemyData;
    [SerializeField] private Transform player,bulletPivot;
    [SerializeField] private int bulletSpeed;
    [SerializeField] private float fireInterval = 2f;
    private float nextFireTime;
    private int currentSpeed;
    [SerializeField] private GameObject preBullet;
    private GameObject bullet;
    private bool isCooldowned;
    [SerializeField] private bool isCrossed;
    private Rigidbody rb,buRb;
    private IEnemyShootProvider shootProvider;


    void Awake()
    {
        shootProvider = GetComponent<IEnemyShootProvider>();
        enemyData = this.GetComponent<EnemyController>();
        rb = GetComponent<Rigidbody>();
        nextFireTime = 0f;
    }

    public bool IsCoolingDown => Time.time < nextFireTime;

    public bool TryShoot()
    {
        if (Time.time < nextFireTime)
        {
            return false;
        }

        float direction = gameObject.GetComponent<IEnemyProvider>().DirectionToPlayer;
        Vector3 pivotPosition = bulletPivot.position;
        pivotPosition.z =transform.position.z +direction * 1f;
        bulletPivot.position = pivotPosition;

        bullet = Instantiate(preBullet, bulletPivot.transform.position, Quaternion.Euler(0f,90f,0f));

        buRb = bullet.GetComponent<Rigidbody>();
        if (buRb != null)
        {
            if (!isCrossed)
            {
                buRb.linearVelocity =Vector3.forward *direction *bulletSpeed;
            }
            else
            {
                Vector3 crossDirection = gameObject.GetComponent<IEnemyProvider>().CrossDirectionToPlayer;
                buRb.linearVelocity =crossDirection  * bulletSpeed;
            }
            
        }

        nextFireTime = Time.time + fireInterval;
        isCooldowned = true;
        return true;
    }

    public void ShootBulllet()
    {
        TryShoot();
    }



}
