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
    private GameObject weaponObject;
    private Animator weaponAnimator;
    private Animator playerAnimator;
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
        playerAnimator = GetComponent<Animator>();
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
        rGb.transform.position = this.transform.position + new Vector3(0f,0f,1f);
        itemGimmick.Initialize(getItems[selectNumber]);
        //rGb.transform.rotation = this.transform.rotation * Quaternion.Euler(0f,180f,0f);
        
        
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
        // スロットを1つ進める
        selectNumber = (selectNumber + 1) % 4;

        // 選択中の枠を移動する
        sc.MoveFrame(selectNumber);

        // 選択中のアイテムIDを見て、武器を切り替える
        ShowWeaponForSelectedSlot();
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
        rgb.transform.position = this.transform.position + new Vector3(0f,0f,1f);
        itemGimmick.Initialize(getItems[selectNumber]);
        //rgb.transform.rotation = this.transform.rotation* Quaternion.Euler(0f,180f,0f);
        //rgb.transform.rotation = this.transform.rotation* Quaternion.Euler(0f,180f,0f);
        //rgb.GetComponent<ItemGimmick>().Initialize(rgb,getItems[selectNumber]);
        
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
        // 武器がまだ出ていなければ出す
        if (!weapon.activeSelf)
        {
            ShowWeaponForSelectedSlot();
            Debug.Log("武器を出した");
            return;
        }

        // 振る武器のAnimatorを取得
        
         weaponAnimator = weaponObject.GetComponentInChildren<Animator>();
        

        if (weaponAnimator == null)
        {
            Debug.LogWarning("武器のAnimatorが見つかりません。");
            return;
        }

        // アイテムの種類でトリガーを変える
        int selectedItemId = GetSelectedItemId();
        if (selectedItemId > 0 && (int)dataBase.GetItemTypeById(selectedItemId) == 4)
        {
            weaponAnimator.SetTrigger("IronBall");
        }
        else
        {
            weaponAnimator.SetTrigger("Weapon");
        }

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
    private int GetSelectedItemId()
    {
        // 何も持っていないときは 0 とする
        if (selectNumber < 0 || selectNumber >= getItems.Count)
        {
            return 0;
        }

        return getItems[selectNumber];
    }

    private void ShowWeaponForSelectedSlot()
    {
        if (weapon == null)
        {
            Debug.LogWarning("Weaponがまだ設定されていません。");
            return;
        }

        // 前の武器を消す
        for (int i = weapon.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(weapon.transform.GetChild(i).gameObject);
        }

        int selectedItemId = GetSelectedItemId();

        // 空のスロットなら武器を隠す
        if (selectedItemId <= 0)
        {
            weaponObject = null;
            weaponAnimator = null;
            weapon.SetActive(false);
            return;
        }

        // アイテムIDからPrefabを探す
        GameObject weaponPrefab = dataBase.GetIntanceById(selectedItemId);
        if (weaponPrefab == null)
        {
            Debug.LogWarning("ID " + selectedItemId + " のPrefabが見つかりません。");
            weaponObject = null;
            weaponAnimator = null;
            weapon.SetActive(false);
            return;
        }

        // 武器を生成して、weaponの子にする
        weaponObject = Instantiate(weaponPrefab, weapon.transform);
        weaponObject.SetActive(false);
        weaponObject.transform.localPosition = Vector3.zero;
        weaponObject.transform.localRotation = Quaternion.identity;
        weaponObject.transform.localScale = Vector3.one;

        // 生成した武器の見た目とAnimatorを初期化する
        ItemGimmick weaponItem = weaponObject.GetComponent<ItemGimmick>();
        if (weaponItem != null)
        {
            weaponItem.InitializeForWeapon(selectedItemId);
        }

        // 持っている間はCollider同士がぶつからないようにする
        Collider[] weaponColliders = weaponObject.GetComponentsInChildren<Collider>(true);
        foreach (Collider weaponCollider in weaponColliders)
        {
            weaponCollider.enabled = false;
        }

        Rigidbody weaponRigidbody = weaponObject.GetComponent<Rigidbody>();
        if (weaponRigidbody != null)
        {
            weaponRigidbody.isKinematic = true;
            weaponRigidbody.useGravity = false;
            weaponRigidbody.linearVelocity = Vector3.zero;
            weaponRigidbody.angularVelocity = Vector3.zero;
        }

        // 生成直後にAnimatorを有効にしてから表示する
        weaponAnimator =weaponObject.GetComponentInChildren<Animator>(true);

        if (weaponAnimator == null)
        {
            Debug.LogWarning("武器のAnimatorが見つかりません。");
            weaponObject.SetActive(true);
            weapon.SetActive(true);
            return;
        }

        weaponObject.SetActive(true);
        weapon.SetActive(true);

        weaponAnimator.enabled = true;
        weaponAnimator.Rebind();
        weaponAnimator.Update(0f);

        Debug.Log("武器を表示: ID = " + selectedItemId);
    }
        
    
    private void SetItem(GameObject gb, int i, bool isexisted)
{
    if (!isexisted)
    {
        sc.RefreshUI(gb);
    }

    ic.DeleteSearchedItem(gb);
    Destroy(gb);

    sc.AddStock(i);

    selectNumber = i;
    sc.MoveFrame(selectNumber);
    ShowWeaponForSelectedSlot();
}
    
}
