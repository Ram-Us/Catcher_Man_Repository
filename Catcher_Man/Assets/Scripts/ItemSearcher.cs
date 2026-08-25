using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;



public class ItemSearcher : MonoBehaviour
{

    private InputAction searchAction;

    [SerializeField] private List<GameObject> aroundItems = new();


    private void Awake()
    {
        searchAction = InputSystem.actions.FindAction("Search");

        
        

    }
    private void OnEnable()
    {
        //searchAction.started += OnSearch;
        searchAction.performed += OnSearch;
        searchAction.canceled += OnSearchCanceled;
    }
    private void OnDisable()
    {
        //searchAction.started -= OnSearch;
        searchAction.performed -= OnSearch;
        searchAction.canceled -= OnSearchCanceled;
    }

    private void OnSearch(InputAction.CallbackContext context)
    {
        Debug.Log("Qが押されたぞ");
        foreach(GameObject obj in aroundItems)
        {
            Item items = obj.GetComponent<Item>();
            if(items != null)
            {
                Debug.Log("これはアイテムにできるぞ！");
                items.EmphasisItems(true);
            }
            
        }
    }

    private void OnSearchCanceled(InputAction.CallbackContext context)
    {
        
        foreach(GameObject obj in aroundItems)
        {
            if (obj == null)
            {
                continue;
            }
            


            Item items = obj.GetComponent<Item>();
            if(items != null)
            {
                items.EmphasisItems(false);
            }
            
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Item"))
        {
            aroundItems.Add(other.gameObject);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        aroundItems.Remove(other.gameObject);
    }

    public void DeleteSearchedItem(GameObject item)
    {
        aroundItems.RemoveAll(obj => obj == null || obj == item);
    }


}
