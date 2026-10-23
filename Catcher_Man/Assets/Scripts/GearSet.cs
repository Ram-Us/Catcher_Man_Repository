using JetBrains.Rider.Unity.Editor;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class GearSet : MonoBehaviour
{
    [SerializeField] private ItemDataBase db;
    private int id;
    private bool isTouchedGear = false,isAttached;
    public bool IsTouchedGear => isTouchedGear;


    void OnTriggerEnter(Collider other)
    {
        id = db.GetId(other.gameObject);
        isAttached = other.gameObject.GetComponent<ItemGimmick>().IsAttached;
        Debug.Log(isAttached);
        if(other.gameObject.CompareTag("Item") && id == 3 && !isAttached)
        {
            isTouchedGear = true;
            Vector3 parentPosition = other.gameObject.transform.position;
            parentPosition = this.gameObject.transform.parent.transform.position;
            parentPosition.x = 0.3f;
            other.gameObject.transform.position = parentPosition;
            other.gameObject.transform.localScale = new Vector3(5f,5f,5f); 
            Rigidbody rb = other.gameObject.GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezePosition;
            rb.useGravity = false;
            //Debug.Log("歯車を置いてるよ"+other.gameObject.transform.position);
        }
    }
    void OnTriggerExit(Collider other)
    {
        id = db.GetId(other.gameObject);
        if(other.gameObject.CompareTag("Item") && id == 3)
        {
            isTouchedGear = false;
            //Debug.Log("歯車を置いてるよ");
        }
    }
}
