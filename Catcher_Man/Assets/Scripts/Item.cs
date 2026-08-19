using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private GameObject frame;

    void Start()
    {
        frame.SetActive(false);
    }


    public void emphasisItems(bool sw)
    {
        frame.SetActive(sw);
        
    }
    

    
}
