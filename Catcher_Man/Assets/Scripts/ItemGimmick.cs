using System.Collections.Generic;
using UnityEngine;
using System;




public class ItemGimmick : MonoBehaviour {

    [SerializeField] private Sprite icon;
    [SerializeField] private GameObject frame;
    private SpriteRenderer sr,dsr,fsr;
    [SerializeField] private ItemDataBase db;
    [SerializeField] int id;

    void Awake()
    {
        frame.SetActive(false);
        var idb = db.GetItemById(id);
        sr = this.GetComponent<SpriteRenderer>();
        fsr = frame.GetComponent<SpriteRenderer>();
        sr.sprite = idb.icon;
        fsr.sprite =idb.icon;
        this.transform.rotation *= Quaternion.Euler(0f,180f,0f);

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
        
    }

    
}
