using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private GameObject frame;
    [SerializeField] private Sprite icon;

    void Start()
    {
        frame.SetActive(false);
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
