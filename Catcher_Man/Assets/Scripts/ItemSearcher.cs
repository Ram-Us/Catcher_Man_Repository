using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;



public class ItemSearcher : MonoBehaviour
{

    private InputAction searchAction;

    [SerializeField] private List<GameObject> aroundItems = new();
    [SerializeField] private AudioClip searchClip;


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
        bool foundItem = false;
        foreach(GameObject obj in aroundItems)
        {
            if (obj == null)
            {
                continue;
            }

            ItemGimmick items = obj.GetComponent<ItemGimmick>();
            if(items != null)
            {
                Debug.Log("これはアイテムにできるぞ！");
                items.EmphasisItems(true);
                foundItem = true;
            }
            
        }

        if (foundItem && searchClip != null)
        {
            AudioSource.PlayClipAtPoint(searchClip, transform.position);
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
            


            ItemGimmick items = obj.GetComponent<ItemGimmick>();
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
        DeleteSearchedItem(other.gameObject);
    }

    public void DeleteSearchedItem(GameObject item)
    {
        aroundItems.RemoveAll(obj => obj == null || obj == item);
    }


}
