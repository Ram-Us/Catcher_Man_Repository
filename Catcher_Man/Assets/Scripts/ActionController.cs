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
    private int selectNumber,id = 0;
    public int SelectNumber => selectNumber;
    [SerializeField]float shootSpeed = 5f;

    [SerializeField]private List<int> getItems = new();
    [SerializeField] private GameObject shootPoint,weapon;
    GameObject childWeapon;
    private Animator animator;
    SpriteRenderer PresentWeaponVisual;
    Sprite choicedWeaponVisual;
    [SerializeField] ItemDataBase dataBase;
    
    private void Awake()
    {
        catchAction = InputSystem.actions.FindAction("Catch");
        putAction = InputSystem.actions.FindAction("Put");
        selectAction = InputSystem.actions.FindAction("Select");
        throwAction = InputSystem.actions.FindAction("Throw");
        swingAction = InputSystem.actions.FindAction("Swing");
        animator = GetComponent<Animator>();
        childWeapon = weapon.transform.GetChild(0).gameObject;

        
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
            id = dataBase.GetId(gb);
            Debug.Log(id+"を取得");
            if (sc.TryAdd())//空きがある状態での同アイテムに加算、もしくは空いた枠にアイテムを格納
            {
                //Debug.Log("獲得！");
                for(int i = 0; i < getItems.Count; i++)
                {
                    if (getItems[i] == id)
                    {
                        SetItem(gb,i,true);
                        Debug.Log("同じアイテムが入ってるので足したぞ！");
                        break;
                    }else if (getItems[i] == 0)
                    {
                        getItems[i] = id;
                        SetItem(gb,i,false);
                        Debug.Log("空き枠に新しいアイテムを入れたぞ！");
                        break;
                    }
                }

            }
            else
            {
                for(int i = 0; i<getItems.Count; i++)
                {
                    if (getItems[i] == id)
                    {
                        SetItem(gb,i,true);
                        Debug.Log("アイテムは満帆だけど、同じアイテムがあったので足したぞ！");
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
        weapon.SetActive(false);
        GameObject rGb = Instantiate(dataBase.GetIntanceById(getItems[selectNumber]));
        ItemGimmick itemGimmick = rGb.GetComponent<ItemGimmick>();
        if (itemGimmick == null)
        {
            Debug.LogError("生成したオブジェクトにItemGimmickがありません。", rGb);
            Destroy(rGb);
            return;
        }
        itemGimmick.Initialize(getItems[selectNumber]);
        //rGb.GetComponent<ItemGimmick>().Initialize(rGb,getItems[selectNumber]);
        rGb.transform.position = this.transform.position + new Vector3(0f,0f,1f);
        rGb.transform.rotation = this.transform.rotation * Quaternion.Euler(0f,180f,0f);
        rGb.SetActive(true);
        if (sc.StockCount[selectNumber] <= 1)
        {
            sc.DeleteUI(selectNumber);
            getItems[selectNumber]=0;
        }
        sc.SubStock(selectNumber);
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
        WeaponSwap();
        sc.MoveFrame(selectNumber);
        Debug.Log(selectNumber+"を選択中");
        
    }
    private void OnThrow(InputAction.CallbackContext context)
    {
        weapon.SetActive(false);
        GameObject rgb = Instantiate(dataBase.GetIntanceById(getItems[selectNumber]));
        ItemGimmick itemGimmick = rgb.GetComponent<ItemGimmick>();
        if (itemGimmick == null)
        {
            Debug.LogError("生成したオブジェクトにItemGimmickがありません。", rgb);
            Destroy(rgb);
            return;
        }
        itemGimmick.Initialize(getItems[selectNumber]);
        //rgb.GetComponent<ItemGimmick>().Initialize(rgb,getItems[selectNumber]);
        rgb.transform.position = this.transform.position + new Vector3(0f,0f,1f);
        rgb.transform.rotation = this.transform.rotation* Quaternion.Euler(0f,180f,0f);
        rgb.SetActive(true);
        ItemGimmick rg = rgb.GetComponent<ItemGimmick>();
        //rg.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePosition;
        rg.GetComponent<Rigidbody>().constraints &= ~RigidbodyConstraints.FreezePositionY;
        rg.GetComponent<Rigidbody>().constraints &= ~RigidbodyConstraints.FreezePositionZ;
        if ((int)dataBase.GetItemTypeById(getItems[selectNumber] )== 2){
            rg.GetComponent<Rigidbody>().useGravity = false;
        }
        rg.GetComponent<Rigidbody>().AddForce(-this.transform.right * shootSpeed, ForceMode.Impulse);
        
        
        
        if (sc.StockCount[selectNumber] <= 1)
        {
            sc.DeleteUI(selectNumber);
            getItems[selectNumber]=0;
        }
        sc.SubStock(selectNumber);
        
        
        
        //Destroy(rgb,5f);

    }
    private void OnSwing(InputAction.CallbackContext context)
    {
        if(!(weapon.activeSelf))
        {
            WeaponSwap();
            Debug.Log("武器を出した");
        }
        else
        {
            animator.SetTrigger("Weapon");
            Debug.Log("武器をしまった");
        }
        
        
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
    private void WeaponSwap()
    {
        if (getItems[selectNumber] == null)
        {
            weapon.SetActive(false);
            Debug.Log("アイテムはなかったよ");
        }
        else
        {
            PresentWeaponVisual = childWeapon.GetComponent<SpriteRenderer>();
            choicedWeaponVisual = dataBase.GetSpriteById(getItems[selectNumber]);
            PresentWeaponVisual.sprite = choicedWeaponVisual;
            weapon.SetActive(true);
        }
        
    }
    private void SetItem(GameObject gb,int i,bool isexisted)
    {
        if (!isexisted)
        {
            sc.RefreshUI(gb);

        }
        ic.DeleteSearchedItem(gb);
        Destroy(gb);
        WeaponSwap();
        sc.AddStock(i);
        
        
    }
    
}
