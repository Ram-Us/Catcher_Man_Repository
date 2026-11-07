using System.Collections.Generic;
using UnityEngine;




public class ItemGimmick : MonoBehaviour {

    [SerializeField] private GameObject frame;
    private SpriteRenderer sr,dsr,fsr;
    [SerializeField] private ItemDataBase db;
    [SerializeField] int id,trapDamage;
    public int Id => id;
    private bool isInitialized;

    private BoxCollider cl;

    [SerializeField] private bool isAttached;
    public bool IsAttached => isAttached;
    [SerializeField] private bool isThrown;
    public bool IsThrown => isThrown;
    [SerializeField] private bool isTrap;
    public bool IsTrap => isTrap;
    [SerializeField] private bool isAttacking;
    public bool IsAttacking => isAttacking;
    [SerializeField] private bool isCollidedItem = false;
    public bool IsCollidedItem => isCollidedItem;
    [SerializeField] private AudioClip enemyHitClip;
    private readonly HashSet<EnemyController> hitEnemies = new();

    

    void Awake()
    {
        if (frame != null)
        {
            frame.SetActive(false);
        }
        //this.transform.rotation *= Quaternion.Euler(0f,180f,0f);
        
    }

   

    private void Start()
    {
        // ステージへ直接配置したアイテムだけ、Inspectorの値で初期化する。
        if (!isInitialized)
        {
            Initialize(id);
        }
    }

    public void Initialize(int itemId)
    {
        id = itemId;

        if (db == null)
        {
            Debug.LogError("ItemDataBaseが設定されていません。", this);
            return;
        }
        if (id != 0)
        {
             //db = itemDataBase;
            

            var idb = db.GetItemById(id);
            if (idb == null)
            {
                Debug.LogError($"ID {id} のアイテムがデータベースにありません。", this);
                return;
            }
            if (id != 9)
            {
                if (idb.ItemType == ItemType.IronBall)
                {
                    if (transform.childCount > 0)
                    {
                        sr = transform.GetChild(0).GetComponent<SpriteRenderer>();
                    }
                }
                else
                {
                    sr = GetComponent<SpriteRenderer>();
                }

                if (sr == null)
                {
                    Debug.LogError("SpriteRendererがありません。", this);
                    return;
                }

                sr.sprite = idb.Icon;
            }

            if (frame != null)
            {
                fsr = frame.GetComponent<SpriteRenderer>();
                if (fsr != null)
                {
                    fsr.sprite = idb.Icon;
                }
            }
            //Debug.Log(idb.Position.x);
            if (!isTrap)
            {
                this.transform.position = new Vector3(idb.Position.x,transform.position.y,transform.position.z);
                this.transform.localScale = idb.Size;
            }
            
            cl = GetComponent<BoxCollider>();
            if (cl != null)
            {
                cl.size = idb.ColliderSize;
                cl.center = idb.ColliderCenterSize;
                cl.isTrigger = false;
            }
            if (id != 9 && idb.Rotated)
            {
                GetComponent<Rigidbody>().constraints |= RigidbodyConstraints.FreezePositionY;
                //GetComponent<Rigidbody>().isKinematic = true;
            }
            else
            {
                GetComponent<Rigidbody>().constraints &= ~RigidbodyConstraints.FreezePositionY;
                //GetComponent<Rigidbody>().isKinematic = false;
            }
            isAttached = false;
            EndAttack();
            SetAnimatorEnabled(false);

        }
       
        isInitialized = true;
    }

    public void InitializeForWeapon(int itemId)
    {
        id = itemId;

        if (db == null)
        {
            Debug.LogError("ItemDataBaseが設定されていません。", this);
            return;
        }

        var itemData = db.GetItemById(id);
        if (itemData == null)
        {
            Debug.LogError($"ID {id} のアイテムがデータベースにありません。", this);
            return;
        }

        if (itemData.ItemType == ItemType.IronBall)
        {
            if (transform.childCount > 0)
            {
                sr = transform.GetChild(0).GetComponent<SpriteRenderer>();
            }
        }
        else
        {
            sr = GetComponent<SpriteRenderer>();
        }

        if (sr != null)
        {
            sr.sprite = itemData.Icon;
        }

        this.transform.localScale = itemData.Size;

        Animator weaponAnimator = GetComponentInChildren<Animator>(true);
        if (weaponAnimator != null)
        {
            weaponAnimator.enabled = true;
        }

        isAttached = false;
        isInitialized = true;
        isThrown = false;
        EndAttack();
    }

    public void EmphasisItems(bool sw)
    {
        if(id != -1 && !isAttached)
        {
            frame.SetActive(sw);
        }
        
        
    }
    private void OnCollisionEnter(Collision collision)
    {
        ForwardEnemyHit(collision);
        if(isAttached)
        {
            //Debug.Log("装備してものに当たってるよ");
            isCollidedItem = true;
        }
        //Debug.Log("当たった");
        if(collision.gameObject.CompareTag("Player") && isTrap)
        {
            PlayerManager playerManager =
                collision.gameObject.GetComponent<PlayerManager>();
            

            if (playerManager != null)
            {
                //Debug.Log(trapDamage+"ダメージ与えます");
                playerManager.SetPlayerHp(trapDamage);
            }
        }

        // ベルト上では、地面に触れても物理挙動を固定しない。
        bool isConveyor =
            collision.collider.GetComponentInParent<ConveyorBelt>() != null;

        if (collision.gameObject.CompareTag("Ground") &&
            !isThrown && !isAttached && !isConveyor)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.constraints |= RigidbodyConstraints.FreezePositionY;
            rb.isKinematic = true;
        }
        

    }

    private void OnTriggerEnter(Collider other)
    {
        ForwardSwingHit(other);
        
        if(isAttached && (other.gameObject.CompareTag("Ground") || other.gameObject.CompareTag("Item")))
        {
            //Debug.Log(db.GetItemNameById(id) + "のアイテムは" + other.gameObject + "に衝突してるよ");
            //Debug.Log("装備してものに当たってるよ");
            isCollidedItem = true;
        }
    }
    private void OnTriggerStay(Collider other)
    {
        // すでに重なっている状態で振り始めても命中させる。
        ForwardSwingHit(other);
    }

    private void ForwardSwingHit(Collider other)
    {
        if (!isAttached || !isAttacking || isThrown)
            return;

        // 敵の索敵用Triggerでは命中させない。
        if (other.isTrigger && other.GetComponent<DamagedArea>() == null)
            return;

        BossController boss = other.GetComponentInParent<BossController>();
        if (boss != null)
        {
            EnemyController enemy = boss.GetComponent<EnemyController>();
            if (TryRegisterEnemyHit(enemy))
                boss.ReceiveItemHit(this);
            return;
        }

        ForwardEnemyHit(other);
    }

    private void OnCollisionStay(Collision collision)
    {
        ForwardEnemyHit(collision);
        if(isAttached)
        {
            isCollidedItem = true;
        }
        
    }

    
    
    private void OnCollisionExit(Collision collision)
    {
        if(isAttached)
        {
            isCollidedItem = false;
        }
        if(!isAttached && !collision.gameObject.CompareTag("Ground"))
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.isKinematic = false;
        }
        
    }
    private void OnTriggerExit(Collider other)
    {
        if(isAttached && (other.gameObject.CompareTag("Ground") || other.gameObject.CompareTag("Item")))
        {
            Debug.Log("装備してものから離れたよ");
            isCollidedItem = false;
        }
    }
    public void SetTrap(bool flag)
    {
        if (flag)
        {
            isTrap = true;
        }
        else
        {
            isTrap = false;
        }
        
    }
    /*public void Initialized(GameObject instance, int id)
    {
        
        this.id = id;
        Item itemData = instance.GetComponent<Item>();
        itemData.Icon = db.GetSpriteById(id);
        itemData.ItemType = db.GetItemTypeById(id);
        itemData.ItemName = db.GetItemNameById(id);
        itemData.Cost = db.GetCostById(id);
        itemData.Speed = db.GetSpeedById(id);
        itemData.Attack = db.GetAttackById(id);
        
    }*/

    public bool GetAttach()
    {
        return IsAttached;
    }
    public void SetAttach(bool isChecked)
    {
        isAttached = isChecked;

        if (cl == null)
        {
            cl = GetComponent<BoxCollider>();
        }

        if (cl != null)
        {
            // 装備中はTrigger、置く・投げるときは通常の衝突。
            cl.isTrigger = isAttached;
        }

        if (!isAttached)
        {
            EndAttack();
        }

        SetAnimatorEnabled(isAttached && !isThrown);
    }
    public bool GetThrown()
    {
        return IsThrown;
    }
    public void SetThrown(bool isChecked)
    {
        if (isChecked && !isThrown)
        {
            hitEnemies.Clear();
        }

        isThrown = isChecked;

        if (isThrown)
        {
            EndAttack();
        }

        SetAnimatorEnabled(isAttached && !isThrown);
    }

    public void BeginAttack()
    {
        if (!isAttached || isThrown)
        {
            return;
        }

        hitEnemies.Clear();
        isAttacking = true;
    }

    public void EndAttack()
    {
        isAttacking = false;
    }

    // 同じ攻撃で、同じ敵に複数回ダメージを与えない。
    public bool TryRegisterEnemyHit(EnemyController enemy)
    {
        if (enemy == null)
            return false;

        if (!isThrown && !(isAttached && isAttacking))
            return false;

        return hitEnemies.Add(enemy);
    }

    private void SetAnimatorEnabled(bool isEnabled)
    {
        Animator itemAnimator = GetComponentInChildren<Animator>(true);
        if (itemAnimator != null)
        {
            itemAnimator.enabled = isTrap || isEnabled;
        }
    }
    private void ForwardEnemyHit(Collision collision)
    {
        ForwardEnemyHit(collision.collider);
    }

    private void ForwardEnemyHit(Collider other)
    {
        if (!isThrown && !(isAttached && isAttacking))
            return;

        // ボスのダメージは既存のBossControllerに任せる。
        if (other.GetComponentInParent<BossController>() != null)
            return;

        EnemyController enemy =
            other.GetComponentInParent<EnemyController>();

        if (enemy == null)
            return;

        foreach (DamagedArea area in
                enemy.GetComponentsInChildren<DamagedArea>())
        {
            if (area.isActiveAndEnabled)
            {
                area.ReceiveItemHit(this);
                break;
            }
        }
    }

}
