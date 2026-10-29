using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public class ItemLane : MonoBehaviour
{
    [SerializeField] private Transform lanePoint;
    private BoxCollider box;
    private int i;
    [SerializeField]private int[] id;
    private bool isSummon = false;
    [SerializeField] private ItemDataBase dataBase;
    void Start()
    {
        box = GetComponent<BoxCollider>();
        StartCoroutine(SummonItem(id));
    }


    IEnumerator SummonItem(int[] id)
    {

        for(i = 0;i < id.Length; i++)
        {
            GameObject pre = Instantiate(dataBase.GetIntanceById(id[i]));
            //Debug.Log(pre+"でてるよ");
            ItemGimmick itemGimmick = pre.GetComponent<ItemGimmick>();
            itemGimmick.Initialize(id[i]);
            Vector3 kariPosition = pre.transform.position;
            kariPosition.y = lanePoint.transform.position.y;
            kariPosition.z = lanePoint.transform.position.z;
            pre.transform.rotation = Quaternion.Euler(0f,90f,0f);
            pre.transform.position = kariPosition;
            Rigidbody rb = pre.GetComponent<Rigidbody>();
            rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
            
            yield return new WaitForSeconds(10f);
            
        }
        yield return null;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Item"))
        {
            other.gameObject.transform.position = lanePoint.transform.position;
        }
    }
}
