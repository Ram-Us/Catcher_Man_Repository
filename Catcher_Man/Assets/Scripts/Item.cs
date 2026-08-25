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
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Item"))
        {
            Destroy(this.gameObject);
        }
        
    }

    
}
