using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;


public class ActionController : MonoBehaviour
{
    private InputAction catchAction,putAction,selectAction;
    private GameObject gb;
    private bool isTouched = false;
    [SerializeField] private SlotController sc;
    int selectNumber = 0;

    [SerializeField]private List<GameObject> getItems = new();

    
    private void Awake()
    {
        catchAction = InputSystem.actions.FindAction("Catch");
        putAction = InputSystem.actions.FindAction("Put");
        selectAction = InputSystem.actions.FindAction("Select");
        
    }

    private void OnEnable() {
        catchAction.started += OnCatch;
        putAction.started += OnPut;
        selectAction.started += OnSelect;
        
    }
    private void OnDisable()
    {
        catchAction.started -= OnCatch;
        putAction.started -= OnPut;
        selectAction.started -= OnSelect;
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
