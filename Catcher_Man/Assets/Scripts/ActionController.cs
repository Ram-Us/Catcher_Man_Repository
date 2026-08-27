using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;


public class ActionController : MonoBehaviour
{
    private InputAction catchAction,putAction,selectAction,throwAction,swingAction;
    private GameObject gb;
    private bool isTouched = false;
    [SerializeField] private SlotController sc;
    [SerializeField] private ItemSearcher ic;
    int selectNumber = 0;
    [SerializeField]float shootSpeed = 5f;

    [SerializeField]private List<GameObject> getItems = new();
    [SerializeField] private GameObject shootPoint;

    private Animator animator;

    
    private void Awake()
    {
        catchAction = InputSystem.actions.FindAction("Catch");
        putAction = InputSystem.actions.FindAction("Put");
        selectAction = InputSystem.actions.FindAction("Select");
        throwAction = InputSystem.actions.FindAction("Throw");
        swingAction = InputSystem.actions.FindAction("Swing");
        animator = GetComponent<Animator>();

        
    }

    private void OnEnable() {
        catchAction.started += OnCatch;
        putAction.started += OnPut;
        selectAction.started += OnSelect;
        throwAction.started += OnThrow;
        swingAction.started += OnSwing;
        
    }
    private void OnDisable()
    {
        catchAction.started -= OnCatch;
        putAction.started -= OnPut;
        selectAction.started -= OnSelect;
        throwAction.started -= OnThrow;
        swingAction.started -= OnSwing;
    }


    private void OnCatch(InputAction.CallbackContext context)
    {
        if (isTouched)
        {   //Debug.Log("タッチ");
            if (sc.TryAdd())
            {
                //Debug.Log("獲得！");
                for(int i = 0; i < getItems.Count; i++)
                {
                    if (getItems[i] == null)
                    {
                        GameObject or = Instantiate(gb);
                        or.SetActive(false);
                        getItems[i] = or;
                        sc.RefreshUI(gb);
                        ic.DeleteSearchedItem(gb);
                        break;
                    }
                }
                
            }
            /*else
            {
                //Debug.Log("失敗");
            }*/
        }
    }
    private void OnPut(InputAction.CallbackContext context)
    {
        GameObject rGb = getItems[selectNumber];
        rGb.transform.position = this.transform.position + new Vector3(0f,0f,1f);
        rGb.transform.rotation = this.transform.rotation * Quaternion.Euler(0f,180f,0f);
        rGb.SetActive(true);
        getItems[selectNumber]=null;
        sc.DeleteUI(selectNumber);
        Debug.Log(selectNumber+"番目のオブジェクトを設置！");
    }
    private void OnSelect(InputAction.CallbackContext context)
    {
        if (selectNumber >= 3)
        {
            selectNumber = 0;
        }
        else
        {
            selectNumber++;
        }
        Debug.Log(selectNumber+"を選択中");
        
    }
    private void OnThrow(InputAction.CallbackContext context)
    {
        GameObject rgb = getItems[selectNumber];
        rgb.transform.position = this.transform.position + new Vector3(0f,0f,1f);
        rgb.transform.rotation = this.transform.rotation* Quaternion.Euler(0f,180f,0f);
        rgb.SetActive(true);
        Item rg = rgb.GetComponent<Item>();
        rg.GetComponent<Rigidbody>().constraints &= ~RigidbodyConstraints.FreezePositionY;
        rg.GetComponent<Rigidbody>().constraints &= ~RigidbodyConstraints.FreezePositionZ;
        rg.GetComponent<Rigidbody>().AddForce(-this.transform.right * shootSpeed, ForceMode.Impulse);
        
        getItems[selectNumber]=null;
        sc.DeleteUI(selectNumber);
        
        Destroy(rgb,5f);

    }
    private void OnSwing(InputAction.CallbackContext context)
    {
        animator.SetTrigger("Weapon");
        Debug.Log("武器を振った");
    }
    

    private void OnCollisionStay(Collision other) {
        if (other.gameObject.CompareTag("Item"))
        {
            isTouched = true;
            gb = other.gameObject;
            //Debug.Log("触れてるよ");

        }
        else
        {
            isTouched = false;

        }
    }
    
}
