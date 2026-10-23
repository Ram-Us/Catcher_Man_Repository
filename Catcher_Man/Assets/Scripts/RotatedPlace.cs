using UnityEngine;

public class RotatedPlace : MonoBehaviour
{
    
    private void OnCollisionEnter(Collision collision)
    {
        
        if (collision.gameObject.CompareTag("Player") || (collision.gameObject.CompareTag("Item") && !collision.gameObject.GetComponent<ItemGimmick>().IsAttached))
        {
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") || (collision.gameObject.CompareTag("Item") && !collision.gameObject.GetComponent<ItemGimmick>().IsAttached))
        {
            collision.transform.SetParent(null);
        }
    }
}