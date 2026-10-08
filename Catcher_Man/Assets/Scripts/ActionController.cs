using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UIElements;
using JetBrains.Annotations;
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
    private float equippedItemScale,beforeMoveDirection,moveDirection,weaponPositionX,weaponRotationZ;
    [SerializeField]private List<int> getItems = new();
    [SerializeField] private GameObject shootPoint,weapon;
    private GameObject weaponObject;
    public bool IsLadderEquipped
    {
        get
        {
            if (weaponObject == null || !weaponObject.activeInHierarchy || dataBase == null)
            {
                return false;
            }
            ItemGimmick equippedItem = weaponObject.GetComponent<ItemGimmick>();
            return equippedItem != null && equippedItem.IsAttached &&
                   dataBase.GetItemTypeById(equippedItem.Id) == ItemType.Ladder;
        }
    }
    private Animator weaponAnimator;
    private Animator playerAnimator;
    private WeaponColliderController weaponColliderController;
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
        weaponColliderController = GetComponent<WeaponColliderController>();
        // PlayerではなくWeaponPivot側に付けた場合にも対応する。
        if (weaponColliderController == null && weapon != null)
        {
            weaponColliderController = weapon.GetComponent<WeaponColliderController>();
        }
        beforeMoveDirection = gameObject.GetComponent<MoveController>().MoveInput;
        weaponPositionX = weapon.transform.localPosition.x;
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
    void FixedUpdate()
    {
        moveDirection = gameObject.GetComponent<MoveController>().MoveInput;
        //Debug.Log(beforeMoveDirection +"==" +moveDirection);
        if(moveDirection != 0 && beforeMoveDirection != moveDirection)
        {
            beforeMoveDirection = moveDirection;
            WeaponDirection(moveDirection);
            
            
        }
        
            

            
        
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
        rGb.transform.position = weapon.transform.position;
        if(beforeMoveDirection == 1)
        {
            rGb.transform.position += new Vector3(0f,0f,1f);
        }else if(beforeMoveDirection == -1)
        {
            rGb.transform.position += new Vector3(0f,0f,-1f);
        }
        //rGb.transform.position = this.transform.position + new Vector3(0f,0f,1f);
        rGb.transform.localEulerAngles = new Vector3(0f,90f,0f);
        itemGimmick.Initialize(getItems[selectNumber]);
        itemGimmick.SetAttach(false);
        //rGb.transform.rotation = this.transform.rotation * Quaternion.Euler(0f,180f,0f);
        rGb.SetActive(true);
        if (sc.StockCount[selectNumber] <= 1)
        {
            sc.DeleteUI(selectNumber);
            getItems[selectNumber]=0;
        }
        else
        {
            ShowWeaponForSelectedSlot();
        }
        sc.SubStock(selectNumber);
        //Debug.Log(selectNumber+"番目のオブジェクトを設置！");
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
    int selectedItemId = getItems[selectNumber];

    if (selectedItemId <= 0)
    {
        return;
    }

    weapon.SetActive(false);

    GameObject prefab = dataBase.GetIntanceById(selectedItemId);
    GameObject rgb = Instantiate(prefab);

    ItemGimmick itemGimmick = rgb.GetComponent<ItemGimmick>();
    Rigidbody rg = rgb.GetComponent<Rigidbody>();

    if (itemGimmick == null)
    {
        Debug.LogError(
            "生成したオブジェクトにItemGimmickがありません。",
            rgb
        );

        Destroy(rgb);
        return;
    }

    if (rg == null)
    {
        Debug.LogError(
            "生成したオブジェクトにRigidbodyがありません。",
            rgb
        );

        Destroy(rgb);
        return;
    }

    rgb.transform.position = weapon.transform.position;
    if(beforeMoveDirection == 1)
    {
        rgb.transform.position += new Vector3(0f,0f,1f);
    }else if(beforeMoveDirection == -1)
    {
        rgb.transform.position += new Vector3(0f,0f,-1f);
    }

    // 角度はQuaternion.Eulerで設定する
    rgb.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

    itemGimmick.Initialize(selectedItemId);
    itemGimmick.SetAttach(false);
    itemGimmick.SetThrown(true);

    rgb.SetActive(true);

    rg.isKinematic = false;
    rg.constraints &= ~RigidbodyConstraints.FreezePositionZ;

    if ((int)dataBase.GetItemTypeById(selectedItemId) != 6)
    {
        rg.useGravity = false;
    }

    rg.linearVelocity = Vector3.zero;
    rg.angularVelocity = Vector3.zero;

    // プレイヤーの向いている方向へ投げる
    rg.AddForce(
        beforeMoveDirection * -transform.right * dataBase.GetSpeedById(selectedItemId),
        ForceMode.Impulse
    );

    // ItemTypeが5なら回転を加える
    if ((int)dataBase.GetItemTypeById(selectedItemId) == 5)
    {
        // オブジェクト自身のZ回転だけを許可;
        rg.constraints |= RigidbodyConstraints.FreezePositionY;
        rg.constraints &= ~RigidbodyConstraints.FreezeRotationZ;


        // angularVelocityはワールド座標なので、
        // transform.forwardでローカルZ軸をワールド方向へ変換する
        rg.angularVelocity = rgb.transform.forward * 20f;
    }

    if (sc.StockCount[selectNumber] <= 1)
    {
        sc.DeleteUI(selectNumber);
        getItems[selectNumber] = 0;
    }
    else
    {
        ShowWeaponForSelectedSlot();
    }

    sc.SubStock(selectNumber);
    }
    private void OnSwing(InputAction.CallbackContext context)
    {
        // 武器がまだ出ていなければ出す
        if (!weapon.activeSelf)
        {
            ShowWeaponForSelectedSlot();
            //Debug.Log("武器を出した");
            // 装備に失敗した場合だけ中断する。成功時は同じ入力でそのまま振る。
            if (!weapon.activeSelf)
            {
                return;
            }
        }
        // アイテムの種類でトリガーを変える
        int selectedItemId = GetSelectedItemId();
        if (selectedItemId <= 0)
        {
            return;
        }
        if (selectedItemId > 0 && (int)dataBase.GetItemTypeById(selectedItemId) == 4)
        {
            if (weaponAnimator == null)
            {
                Debug.LogWarning("鉄球のAnimatorが見つかりません。");
                return;
            }
            weaponAnimator.SetTrigger("IronBall");
        }
        else
        {
            if (weaponColliderController == null)
            {
                Debug.LogWarning("WeaponColliderControllerが見つかりません。");
                return;
            }
            weaponColliderController.SwingWeapon();
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
        if (weaponColliderController != null)
        {
            weaponColliderController.StopSwing();
        }
        // 前の武器を消す
        for (int i = weapon.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(weapon.transform.GetChild(i).gameObject);
        }
        int selectedItemId = GetSelectedItemId();
        // ID6だけZ軸を140度傾ける。それ以外は0度。
        weaponRotationZ = selectedItemId == 6 ? 140f : 0f;
        weapon.transform.localRotation = Quaternion.Euler(0f, 0f, weaponRotationZ);

        Debug.Log(weaponRotationZ);
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
        weaponObject = Instantiate(weaponPrefab);
        weaponObject.transform.SetParent(weapon.transform, false);
        
        weaponObject.SetActive(false);
        // 生成した武器の見た目とAnimatorを初期化する
        ItemGimmick weaponItem = weaponObject.GetComponent<ItemGimmick>();
        if (weaponItem != null)
        {
            weaponItem.InitializeForWeapon(selectedItemId);
        }
        if (weaponItem != null)
        {
            weaponItem.SetAttach(true);
        }
        Rigidbody weaponRigidbody = weaponObject.GetComponent<Rigidbody>();
        if (weaponRigidbody != null)
        {
            weaponRigidbody.isKinematic = true;
            weaponRigidbody.useGravity = false;
            weaponRigidbody.linearVelocity = Vector3.zero;
            weaponRigidbody.angularVelocity = Vector3.zero;
        }
        weaponObject.SetActive(true);
        weapon.SetActive(true);
        Debug.Log((int)dataBase.GetItemTypeById(selectedItemId));
        weaponAnimator = weaponObject.GetComponentInChildren<Animator>(true);
        if (weaponAnimator != null)
        {
            weaponAnimator.enabled = true;
            weaponAnimator.Rebind();
            weaponAnimator.Update(0f);
        }
        //weaponRigidbody.constraints &= ~RigidbodyConstraints.FreezeRotationZ;
        weaponObject.transform.localPosition = new Vector3(0f, 0f, 0f);
        weaponObject.transform.localScale = dataBase.GetEquippedItemScaleById(getItems[selectNumber]);
        BoxCollider cl = weaponObject.GetComponent<BoxCollider>();
        cl.center = dataBase.GetEquippedColliderCenterById(getItems[selectNumber]);
        cl.size = dataBase.GetEquippedColliderSizeById(getItems[selectNumber]);
        WeaponDirection(beforeMoveDirection);
        Debug.Log("武器を表示: ID = " + selectedItemId);
        if (weaponColliderController != null)
        {
            weaponColliderController.SwingInitialize(weapon.transform);
        }
    }
    private void SetItem(GameObject gb, int i, bool isexisted)
    {
        if (!isexisted)
        {
            sc.RefreshUI(gb);
        }
        ic.DeleteSearchedItem(gb);
        MoveController moveController = GetComponent<MoveController>();
        if (moveController != null)
        {
            moveController.ForgetWorldItem(gb);
        }
        Destroy(gb);
        isTouched = false;
        this.gb = null;
        sc.AddStock(i);
        selectNumber = i;
        sc.MoveFrame(selectNumber);
        ShowWeaponForSelectedSlot();
    }
    private void WeaponDirection(float moveDirection)
    {
        if (moveDirection == -1)
        {
            Debug.Log("左向き");
            SetWeaponRotation(180f, -weaponRotationZ);
            //Debug.Log(weapon.transform.rotation.eulerAngles);
            Vector3 LocalPosition = weapon.transform.localPosition;
            LocalPosition.x = -weaponPositionX;
            weapon.transform.localPosition = LocalPosition;
        }
        else if (moveDirection == 1)
        {
            Debug.Log("右向き");
            SetWeaponRotation(0f, weaponRotationZ);
            //Debug.Log(weapon.transform.rotation.eulerAngles);
            Vector3 LocalPosition = weapon.transform.localPosition;
            LocalPosition.x = weaponPositionX;
            weapon.transform.localPosition = LocalPosition;
        }
    }

    private void SetWeaponRotation(float yRotation, float zRotation)
    {
        Quaternion faceDirection = Quaternion.Euler(0f, yRotation, 0f);
        Quaternion tilt = Quaternion.Euler(0f, 0f, zRotation);
        weapon.transform.localRotation = tilt * faceDirection;
    }
}
