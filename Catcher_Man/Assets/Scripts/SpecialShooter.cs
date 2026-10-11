using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.LightTransport;
using Unity.VisualScripting;
using UnityEngine.UI;
using UnityEngine.InputSystem.Controls;
using System.Data.Common;

public class SpecialShooter : MonoBehaviour
{
    [SerializeField] private List<GameObject> shootPoints = new(),energyBalls = new(),energyBallParts = new();
    [SerializeField] private GameObject prefabEnergyBall;
    private Animator anim;
    private GameObject energyBall;
    private int random = 0,shootCount = 3,listNumber = 0,scaleCount = 4;
    [SerializeField] private float BigTime = 3.0f,ShootSpeed = 10f;
    [SerializeField] private Vector3 beforeScale = new Vector3(0f,0f,0f),afterScale = new Vector3(3f,3f,3f);



    public void EnergyBallCharge()
    {
        for(int i = 0;i < shootCount; i++)
        {
            GetRandom();
            //Debug.Log(listNumber);
            energyBall = Instantiate(prefabEnergyBall,shootPoints[listNumber].transform.position,Quaternion.identity);
            anim = energyBall.GetComponent<Animator>();
            anim.Play("ChangeScale");
            //anim.SetTrigger("Big");
            Debug.Log("準備開始");
            energyBalls.Add(energyBall);
            
        }
        //EnergyBallShoot();
        
        
    }
    public void EnergyBallShoot()
    {
        energyBalls.RemoveAll(obj => obj == null);
        foreach(GameObject gb in energyBalls)
        {
            Rigidbody rigidbody = gb.GetComponent<Rigidbody>();
            rigidbody.AddForce(transform.right*ShootSpeed,ForceMode.Impulse);
            Destroy(gb,10f);
            Debug.Log("発射");
            
        }
        
        Debug.Log("発射終了");
    }
    public void EnergyBallDestroy()
    {
        if(energyBalls != null)
        {
            foreach(GameObject i in energyBalls)
            {
                Debug.Log(i+"を破壊したぞ");
                Destroy(i);
            }
        }
    }
    private void GetRandom()
    {
        random = UnityEngine.Random.Range(0,7);
        if(listNumber == random)
        {
            GetRandom();
        }
        else
        {
            listNumber = random;
        }
    }

}
