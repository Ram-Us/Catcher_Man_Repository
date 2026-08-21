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
    

    
}
