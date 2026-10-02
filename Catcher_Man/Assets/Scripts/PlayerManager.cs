using UnityEngine;
using System.Collections.Generic;
using System;
public class PlayerManager : MonoBehaviour
{
    [SerializeField] private int playerHp;
    public int PlayerHp => playerHp;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SetPlayerHp(int damage)
    {
        playerHp -= damage;
    }
}
