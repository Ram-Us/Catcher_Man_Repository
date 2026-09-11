using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.CompilerServices;




public class ItemGimmick : MonoBehaviour {

    [SerializeField] private GameObject frame;
    private SpriteRenderer sr,dsr,fsr;
    [SerializeField] private ItemDataBase db;
    [SerializeField] int id;
    public int Id => id;
    private bool isInitialized;

    private BoxCollider cl;

    void Awake()
    {
        if (frame != null)
        {
            frame.SetActive(false);
        }
        this.transform.rotation *= Quaternion.Euler(0f,180f,0f);
        
    }

   

    private void Start()
    {
        // ステージへ直接配置したアイテムだけ、Inspectorの値で初期化する。
        if (!isInitialized)
        {
            Initialize(id);
        }
    }

    public void Initialize(int itemId)
    {
        //db = itemDataBase;
        id = itemId;

        if (db == null)
        {
            Debug.LogError("ItemDataBaseが設定されていません。", this);
            return;
        }

        var idb = db.GetItemById(id);
        if (idb == null)
        {
            Debug.LogError($"ID {id} のアイテムがデータベースにありません。", this);
            return;
        }

        sr = this.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError("SpriteRendererがありません。", this);
            return;
        }

        sr.sprite = idb.Icon;

        if (frame != null)
        {
            fsr = frame.GetComponent<SpriteRenderer>();
            if (fsr != null)
            {
                fsr.sprite = idb.Icon;
            }
        }
        
        cl = GetComponent<BoxCollider>();
        if (cl != null)
        {
            cl.size = idb.ColliderSize;
            cl.center = idb.ColliderCenterSize;
        }
        if (idb.Rotated)
        {
            GetComponent<Rigidbody>().constraints |= RigidbodyConstraints.FreezePositionY;
            //GetComponent<Rigidbody>().isKinematic = true;
        }
        else
        {
            GetComponent<Rigidbody>().constraints &= ~RigidbodyConstraints.FreezePositionY;
            //GetComponent<Rigidbody>().isKinematic = false;
        }
        

        isInitialized = true;
    }

    public void EmphasisItems(bool sw)
    {
        frame.SetActive(sw);
        
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Item"))
        {
            Debug.Log("アイテムに触れた");
            Destroy(this.gameObject);
        }
        if(collision.gameObject.CompareTag("Ground"))
        {
            GetComponent<Rigidbody>().constraints |= RigidbodyConstraints.FreezePositionY;
        }
        
    }
    /*public void Initialized(GameObject instance, int id)
    {
        
        this.id = id;
        Item itemData = instance.GetComponent<Item>();
        itemData.Icon = db.GetSpriteById(id);
        itemData.ItemType = db.GetItemTypeById(id);
        itemData.ItemName = db.GetItemNameById(id);
        itemData.Cost = db.GetCostById(id);
        itemData.Speed = db.GetSpeedById(id);
        itemData.Attack = db.GetAttackById(id);
        
    }*/

    
}
