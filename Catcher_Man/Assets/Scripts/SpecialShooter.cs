using UnityEngine;
using System.Collections.Generic;

public class SpecialShooter : MonoBehaviour
{
    [SerializeField] private List<GameObject> shootPoints = new(),energyBalls = new(),energyBallParts = new();
    [SerializeField] private GameObject prefabEnergyBall;
    private Animator anim;
    private GameObject energyBall;
    [SerializeField, Min(1)] private int shootCount = 3;
    private int listNumber = -1;
    [SerializeField] private float ShootSpeed = 10f;
    [SerializeField] private Vector3 beforeScale = new Vector3(0f,0f,0f),afterScale = new Vector3(3f,3f,3f);



    public void EnergyBallCharge()
    {
        if (prefabEnergyBall == null)
        {
            Debug.LogError("エネルギーボールのPrefabが設定されていません。", this);
            return;
        }

        if (shootPoints == null || shootPoints.Count == 0)
        {
            Debug.LogError("発射地点が設定されていません。", this);
            return;
        }

        energyBalls.RemoveAll(obj => obj == null);

        for(int i = 0;i < shootCount; i++)
        {
            GetRandom();

            if (shootPoints[listNumber] == null)
            {
                Debug.LogWarning($"発射地点 {listNumber} が設定されていません。", this);
                continue;
            }

            energyBall = Instantiate(prefabEnergyBall,shootPoints[listNumber].transform.position,Quaternion.identity);
            anim = energyBall.GetComponent<Animator>();
            if (anim != null)
            {
                anim.Play("ChangeScale");
            }
            //Debug.Log("準備開始");
            energyBalls.Add(energyBall);
            
        }
        //EnergyBallShoot();
        
        
    }
    public void EnergyBallShoot()
    {
        energyBalls.RemoveAll(obj => obj == null);
        foreach(GameObject gb in energyBalls)
        {
            if (gb == null)
            {
                continue;
            }

            Rigidbody rigidbody = gb.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                Debug.LogWarning("エネルギーボールにRigidbodyがありません。", gb);
                continue;
            }

            rigidbody.AddForce(transform.right*ShootSpeed,ForceMode.Impulse);
            Destroy(gb,10f);
            //Debug.Log("発射");
            
        }
        
        energyBalls.Clear();
        //Debug.Log("発射終了");
    }
    public void EnergyBallDestroy()
    {
        if(energyBalls != null)
        {
            foreach(GameObject i in energyBalls)
            {
                if (i != null)
                {
                    Debug.Log(i+"を破壊したぞ");
                    Destroy(i);
                }
            }

            energyBalls.Clear();
        }
    }
    private void GetRandom()
    {
        if (shootPoints.Count == 1)
        {
            listNumber = 0;
            return;
        }

        int nextNumber = listNumber < 0
            ? Random.Range(0, shootPoints.Count)
            : Random.Range(0, shootPoints.Count - 1);

        if (listNumber >= 0 && nextNumber >= listNumber)
        {
            nextNumber++;
        }

        listNumber = nextNumber;
    }

}
