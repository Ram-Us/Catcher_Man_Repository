using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    [SerializeField]private bool isAttacking;
    public bool IsAttacking => isAttacking;
    [SerializeField, Min(0.01f)] private float ironBallAttackDuration = 0.4f;
    private Coroutine attackFlagCoroutine;
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
    [SerializeField] private SpecialShooter sp;
    [SerializeField, Min(0f)] private float specialShootDelay = 3f;
    private bool isSpecialCharging;
    private Coroutine specialShootCoroutine;
    SpriteRenderer PresentWeaponVisual;
    Sprite choicedWeaponVisual;
    [SerializeField] ItemDataBase dataBase;
    private GameObject effect;
    [SerializeField] private GameObject swingEffectPrefab,swingEffectPrefab1;
    [SerializeField] private Transform swingEffectPoint;
    private GameObject currentParticlePrefab;
    private Transform effectPoint;

    private Vector3 normalWeaponPosition;
    // IDごとの右向きの基準。向き変更時もこの値から計算する。
    private Vector3 activeWeaponPosition;
    private Vector3 activeWeaponEulerAngles;
    private float pendingWeaponDirection;

    private void Awake()
    {
        catchAction = InputSystem.actions.FindAction("Catch");
        putAction = InputSystem.actions.FindAction("Put");
        selectAction = InputSystem.actions.FindAction("Select");
        throwAction = InputSystem.actions.FindAction("Throw");
        swingAction = InputSystem.actions.FindAction("Swing");
        playerAnimator = GetComponent<Animator>();
        normalWeaponPosition = weapon.transform.localPosition;
        activeWeaponPosition = normalWeaponPosition;
        weaponColliderController = GetComponent<WeaponColliderController>();
        // PlayerではなくWeaponPivot側に付けた場合にも対応する。
        if (weaponColliderController == null && weapon != null)
        {
            weaponColliderController = weapon.GetComponent<WeaponColliderController>();
        }
        if (sp == null)
        {
            sp = GetComponent<SpecialShooter>();
        }
        beforeMoveDirection = gameObject.GetComponent<MoveController>().MoveInput;
        if (beforeMoveDirection == 0f)
        {
            beforeMoveDirection = 1f;
        }
        weaponPositionX = weapon.transform.localPosition.x;
        pendingWeaponDirection = beforeMoveDirection;
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

        if (specialShootCoroutine != null)
        {
            StopCoroutine(specialShootCoroutine);
            specialShootCoroutine = null;
        }

        EndWeaponAttack();
        isSpecialCharging = false;
    }
    void FixedUpdate()
    {
        if (isAttacking && attackFlagCoroutine == null &&
            (weaponColliderController == null || !weaponColliderController.IsSwinging))
        {
            EndWeaponAttack();
        }

        moveDirection = GetComponent<MoveController>().MoveInput;
        if (moveDirection != 0f)
            pendingWeaponDirection = Mathf.Sign(moveDirection);

        // 振っている間は中断しない。最後に入力された向きを終了後に反映する。
        bool swinging = weaponColliderController != null && weaponColliderController.IsSwinging;
        if (!isAttacking && !swinging && pendingWeaponDirection != 0f &&
            beforeMoveDirection != pendingWeaponDirection)
        {
            beforeMoveDirection = pendingWeaponDirection;
            WeaponDirection(beforeMoveDirection);
        }

        if (isTouched)
        {
            Debug.Log("タッチ中");
            if(sc.TryAdd()){
                Debug.Log("アイテム化可能");
            }
        }
    }

    private void OnCatch(InputAction.CallbackContext context)
    {
        if (isTouched)
        {   //Debug.Log("タッチ");
            
            Debug.Log(id+"を取得");
            if (sc.TryAdd() && id > 0 )//空きがある状態での同アイテムに加算、もしくは空いた枠にアイテムを格納
            {
                //Debug.Log("獲得！");
                for(int i = 0; i < getItems.Count; i++)
                {
                    if (getItems[i] == id)
                    {
                        SetItem(gb,i,true);
                        //Debug.Log("同じアイテムが入ってるので足したぞ！");
                        break;
                    }else if (getItems[i] == 0)
                    {
                        getItems[i] = id;
                        SetItem(gb,i,false);
                        //Debug.Log("空き枠に新しいアイテムを入れたぞ！");
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
        
        if (!weapon.transform.GetChild(0).GetComponent<ItemGimmick>().IsCollidedItem)
        {
            Debug.Log($"OnPut実行：slot={selectNumber}, ID={GetSelectedItemId()}");
            EndWeaponAttack();
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
            Vector3 kariPositon = rGb.transform.position;
            kariPositon.y = weapon.transform.position.y;
            kariPositon.z = weapon.transform.position.z;
            rGb.transform.position = kariPositon;
            if(beforeMoveDirection == 1)
            {
                rGb.transform.position += new Vector3(0f,0f,0.5f);
            }else if(beforeMoveDirection == -1)
            {
                rGb.transform.position += new Vector3(0f,0f,-0.5f);
            }
            //rGb.transform.position = this.transform.position + new Vector3(0f,0f,1f);
            SetDroppedItemRotation(rGb.transform, getItems[selectNumber]);
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
        }
        
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
    EndWeaponAttack();
    int selectedItemId = getItems[selectNumber];

    if (selectedItemId <= 0)
    {
        return;
    }

    weapon.SetActive(false);

    GameObject prefab = dataBase.GetIntanceById(selectedItemId);
    GameObject rgb = Instantiate(prefab);
    rgb.layer = LayerMask.NameToLayer("AttackWeapon");

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

    itemGimmick.Initialize(selectedItemId);
    
    rgb.transform.position = weapon.transform.position;
    if(beforeMoveDirection == 1)
    {
        rgb.transform.position += new Vector3(0f,0f,1f);
    }else if(beforeMoveDirection == -1)
    {
        rgb.transform.position += new Vector3(0f,0f,-1f);
    }

    SetDroppedItemRotation(rgb.transform, selectedItemId);

    itemGimmick.SetAttach(false);
    itemGimmick.SetThrown(true);

    rgb.SetActive(true);

    rg.isKinematic = false;
    rg.constraints &= ~RigidbodyConstraints.FreezePositionZ;

    if ((int)dataBase.GetItemTypeById(selectedItemId) == 5)
    {
        rg.constraints |= RigidbodyConstraints.FreezePositionY;
        rg.useGravity = false;
    }


    rg.linearVelocity = Vector3.zero;
    rg.angularVelocity = Vector3.zero;

    // プレイヤーの向いている方向へ投げる
    rg.AddForce(
        beforeMoveDirection * -transform.right * dataBase.GetSpeedById(selectedItemId),
        ForceMode.Impulse
    );
    

    if (id == 5)
    {
        rg.constraints &= ~RigidbodyConstraints.FreezeRotationZ;
        rgb.transform.rotation = Quaternion.Euler(0f,90f,beforeMoveDirection*90f);
    }

    // ItemTypeが5なら回転を加える
    if ((int)dataBase.GetItemTypeById(selectedItemId) == 5)
    {
        // オブジェクト自身のZ回転だけを許可;
        
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

    private static void SetDroppedItemRotation(Transform itemTransform, int itemId)
    {
        float worldYRotation = itemId == 9 ? 0f : 90f;
        itemTransform.rotation = Quaternion.Euler(0f, worldYRotation, 0f);
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
            if (isAttacking)
            {
                return;
            }

            weaponAnimator.SetTrigger("IronBall");
            BeginWeaponAttack();
            attackFlagCoroutine = StartCoroutine(
                EndAttackAfterDelay(ironBallAttackDuration));
        }
        else if (selectedItemId == 9)
        {
            if (weaponAnimator == null)
            {
                Debug.LogWarning("3DレーザーガンのAnimatorが見つかりません。");
                return;
            }

            if (sp == null)
            {
                Debug.LogWarning("SpecialShooterが設定されていません。");
                return;
            }

            if (isSpecialCharging)
            {
                return;
            }
            
            weaponAnimator.SetTrigger("Beam");
            sp.EnergyBallCharge();
            specialShootCoroutine = StartCoroutine(ShootSpecialAfterDelay());
        }
        else
        {
            if (weaponColliderController == null)
            {
                Debug.LogWarning("WeaponColliderControllerが見つかりません。");
                return;
            }

            if (!weaponColliderController.IsSwinging)
            {
                isAttacking = true;
                weaponColliderController.SwingWeapon();

                if (weaponColliderController.IsSwinging)
                {
                    
                    BeginWeaponAttack();
                }
            }
            if (swingEffectPrefab != null)
            {
                

                if (swingEffectPoint != null)
                {
                    effectPoint = swingEffectPoint.transform;
                }
                else
                {
                    effectPoint = weapon.transform;
                }
                if(beforeMoveDirection == 1 || beforeMoveDirection == 0)
                {
                    currentParticlePrefab = swingEffectPrefab;
                }
                else
                {
                    currentParticlePrefab = swingEffectPrefab1;
                }
                effect = Instantiate(
                    currentParticlePrefab,
                    weapon.transform.position,
                    weapon.transform.localRotation);
            


                Destroy(effect, 0.5f);
            }
        }
        
        
        Debug.Log("武器を振った");
    }

    private IEnumerator ShootSpecialAfterDelay()
    {
        isSpecialCharging = true;
        yield return new WaitForSeconds(specialShootDelay);

        if (sp != null)
        {
            sp.EnergyBallShoot();
        }

        isSpecialCharging = false;
        specialShootCoroutine = null;
    }

    private IEnumerator EndAttackAfterDelay(float duration)
    {
        yield return new WaitForSeconds(duration);
        attackFlagCoroutine = null;
        
        EndWeaponAttack();
    }

    private void BeginWeaponAttack()
    {
        
        if (weaponObject == null)
        {
            return;
        }

        ItemGimmick equippedItem = weaponObject.GetComponent<ItemGimmick>();
        if (equippedItem == null || !equippedItem.IsAttached)
        {
            return;
        }

        if (attackFlagCoroutine != null)
        {
            StopCoroutine(attackFlagCoroutine);
            attackFlagCoroutine = null;
        }
        BoxCollider box = weaponObject.GetComponent<BoxCollider>();
        if (box != null)
        {
            box.isTrigger = false;
        }

        isAttacking = true;
        SetEquippedWeaponLayer("AttackWeapon");
        equippedItem.BeginAttack();
        
    }

    private void EndWeaponAttack()
    {
        
        
        if (attackFlagCoroutine != null)
        {
            StopCoroutine(attackFlagCoroutine);
            attackFlagCoroutine = null;
        }

        if (weaponObject != null)
        {
            BoxCollider box = weaponObject.GetComponent<BoxCollider>();
            if (box != null)
            {
                box.isTrigger = true;
            }
            ItemGimmick equippedItem = weaponObject.GetComponent<ItemGimmick>();
            if (equippedItem != null)
            {
                equippedItem.EndAttack();
            }
        }
        
        
        isAttacking = false;
        SetEquippedWeaponLayer("EquippedWeapon");
    }

    private void SetEquippedWeaponLayer(string layerName)
    {
        if (weaponObject == null) return;
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            Debug.LogWarning(layerName + "レイヤーを設定してください。", this);
            return;
        }
        // 子にColliderがある武器にも反映する。
        foreach (Transform part in weaponObject.GetComponentsInChildren<Transform>(true))
            part.gameObject.layer = layer;
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Item"))
        {
            isTouched = true;
            gb = collision.gameObject;
            id = dataBase.GetId(gb);
            //Debug.Log("itemで"+id+"を格納しているよ");
            //Debug.Log("触れてるよ");
        }
    }

    private void OnCollisionStay(Collision other) {
        if (other.gameObject.CompareTag("Item"))
        {
            isTouched = true;
            gb = other.gameObject;
            id = dataBase.GetId(gb);
            //Debug.Log("itemで"+id+"を格納しているよ");
            //Debug.Log("触れてるよ");
        }
        else if (other.gameObject.CompareTag("Items"))
        {
            isTouched = true;
            id = other.gameObject.GetComponent<StockItemSpace>().StockID;
            gb = other.gameObject;
            Debug.Log("itemsで"+id+"を格納しているよ");
        }
        

    }

    private void OnCollisionExit(Collision other) {
        if (other.gameObject.CompareTag("Item"))
        {
            isTouched = false;
            gb = null;
            //Debug.Log("itemで"+id+"を格納しているよ");
            //Debug.Log("触れてるよ");
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
        EndWeaponAttack();
        // 前の武器を消す
        for (int i = weapon.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(weapon.transform.GetChild(i).gameObject);
        }
        int selectedItemId = GetSelectedItemId();

        if (selectedItemId != 9 && specialShootCoroutine != null)
        {
            StopCoroutine(specialShootCoroutine);
            specialShootCoroutine = null;
            isSpecialCharging = false;

            if (sp != null)
            {
                sp.EnergyBallDestroy();
            }
        }

        // IDごとの位置・回転はここで決める（すべてローカル座標）。
        activeWeaponPosition = normalWeaponPosition;
        activeWeaponEulerAngles = Vector3.zero;
        switch (selectedItemId)
        {
            case 4:
            case 5:
                activeWeaponPosition = new Vector3(-4.02f, -1.46f, 0f);
                activeWeaponEulerAngles = new Vector3(0f, 0f, 43f);
                break;
            case 6:
            activeWeaponPosition = new Vector3(-7.69f, 0.72f, 0f);
                activeWeaponEulerAngles = new Vector3(0f, 0f, 140f);
                break;
            case 7:
                activeWeaponPosition = new Vector3(-0.82f, -2.07f, 0f);
                break;
            case 8:
                activeWeaponPosition = new Vector3(-7.1f, -2.55f, 0f);
                break;
            case 9:
                activeWeaponEulerAngles = Vector3.zero;
                break;
        }
        //weapon.transform.position = selectedItemId == 4 ? new Vector3(-3.78f,-2.59f,0f) : new Vector3(-6.78f,-0.19f,0f);

        //Debug.Log(weaponRotationZ);
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
        if (weaponObject != null)
                weaponObject.layer = LayerMask.NameToLayer("EquippedWeapon");

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
        //Debug.Log((int)dataBase.GetItemTypeById(selectedItemId));
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
        if (cl != null)
        {
            cl.isTrigger = true;
            cl.center = dataBase.GetEquippedColliderCenterById(selectedItemId);
            cl.size = dataBase.GetEquippedColliderSizeById(selectedItemId);
        }
        WeaponDirection(beforeMoveDirection);
        Debug.Log("武器を表示: ID = " + selectedItemId);

    }
    private void SetItem(GameObject gb, int i, bool isexisted)
    {
        if (!isexisted)
        {
            sc.RefreshUI(gb,id);
        }
        
        if (gb.CompareTag("Item")){
            ic.DeleteSearchedItem(gb);
            MoveController moveController = GetComponent<MoveController>();
            if (moveController != null)
            {
                moveController.ForgetWorldItem(gb);
            }
            Destroy(gb);
        }else if (gb.CompareTag("Items"))
        {
            gb.GetComponent<StockItemSpace>().SubLimitStockNumber();
        }
        isTouched = false;
        this.gb = null;
        sc.AddStock(i);
        selectNumber = i;
        sc.MoveFrame(selectNumber);
        ShowWeaponForSelectedSlot();
    }
    private void WeaponDirection(float moveDirection)
    {
        if (weapon == null || moveDirection == 0f) return;

        // 攻撃中の基準更新はコルーチンを止めるため、終了後まで待つ。
        if (isAttacking ||
            (weaponColliderController != null && weaponColliderController.IsSwinging)) return;

        bool isLeft = moveDirection < 0f;
        Vector3 localPosition = activeWeaponPosition;
        if (isLeft) localPosition.x = -localPosition.x;
        weapon.transform.localPosition = localPosition;

        Quaternion baseRotation = Quaternion.Euler(activeWeaponEulerAngles);
        if (isLeft)
        {
            // 親のローカルX方向を反転した姿勢。Z傾斜も左右反転する。
            Vector3 mirroredEuler = activeWeaponEulerAngles;
            mirroredEuler.y = -mirroredEuler.y;
            mirroredEuler.z = -mirroredEuler.z;
            baseRotation = Quaternion.Euler(mirroredEuler) * Quaternion.Euler(0f, 180f, 0f);
        }
        weapon.transform.localRotation = baseRotation;

        // 攻撃終了時に古い位置・回転へ戻らないよう、基準も更新する。
        if (weaponColliderController != null)
            weaponColliderController.SwingInitialize(weapon.transform);
    }
}
