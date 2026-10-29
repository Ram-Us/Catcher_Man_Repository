using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class MoveController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpPower = 7f;
    [SerializeField] private float climbSpeed = 1f;
    [SerializeField] private ItemDataBase db;

    private Rigidbody rb;
    private ActionController actionController;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction crouchAction;
    [Header("しゃがみ（本体のBoxCollider）")]
    [SerializeField] private BoxCollider bodyCollider;
    [SerializeField, Range(0.1f, 1f)] private float crouchHeightRatio = 0.5f;
    [SerializeField, Range(0f, 1f)] private float crouchMoveMultiplier = 0.5f;
    [SerializeField] private LayerMask standBlockingLayers = ~0;
    private Vector3 standingSize, standingCenter;
    private bool crouchHeld, crouchJump;
    private float crouchJumpTime;
    public bool IsCrouching { get; private set; }
    public bool IsCrouchJumping => crouchJump;
    public event System.Action Jumped;
    private readonly HashSet<Collider> supportContacts = new();
    private readonly HashSet<Collider> ladderContacts = new();
    private readonly HashSet<Collider> conveyorContacts = new();

    private float moveInput;
    public float MoveInput => moveInput;
    private bool jumpRequested;
    private bool jumpHeld;
    private bool jumpConsumed;
    private bool wasClimbing;
    [SerializeField] private bool isGround;
    [SerializeField] private bool isTouchingLadder;
    public bool IsGrounded => isGround;
    public bool IsClimbing => wasClimbing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        actionController = GetComponent<ActionController>();
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        crouchAction = InputSystem.actions.FindAction("Crouch");
        if (bodyCollider == null) bodyCollider = GetComponent<BoxCollider>();
        if (bodyCollider != null)
        {
            standingSize = bodyCollider.size;
            standingCenter = bodyCollider.center;
        }
        else Debug.LogError("しゃがみ用の本体BoxColliderを設定してください。", this);
        if (crouchAction == null) Debug.LogWarning("Input ActionsにCrouchを追加してください。", this);
        jumpRequested = false;
        jumpHeld = false;
        wasClimbing = false;
        jumpConsumed = false;
        isGround = false;
        isTouchingLadder = false;
    }

    private void OnEnable()
    {
        moveAction.performed += OnMove;
        moveAction.canceled += OnMoveCanceled;
        jumpAction.started += OnJump;
        jumpAction.canceled += OnJumpCanceled;
        if (crouchAction != null)
        {
            crouchAction.performed += OnCrouch;
            crouchAction.canceled += OnCrouchCanceled;
        }
    }

    private void OnDisable()
    {
        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMoveCanceled;
        jumpAction.started -= OnJump;
        jumpAction.canceled -= OnJumpCanceled;
        if (crouchAction != null)
        {
            crouchAction.performed -= OnCrouch;
            crouchAction.canceled -= OnCrouchCanceled;
        }
        crouchHeld = false;
        jumpHeld = false;
        jumpRequested = false;
        crouchJump = false;
        // 天井の下で無効化された場合にも、無理に立ち上がらない。
        
        supportContacts.Clear();
        ladderContacts.Clear();
        conveyorContacts.Clear();
        
    }

    private void FixedUpdate()
    {
        // Destroy済みの接触相手が残っても接地やはしご扱いを続けない。
        supportContacts.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
        conveyorContacts.RemoveWhere(c =>
        {
            if (c == null || !c.enabled || !c.gameObject.activeInHierarchy)
            {
                return true;
            }

            ConveyorBelt conveyor = c.GetComponentInParent<ConveyorBelt>();
            return conveyor == null || !conveyor.isActiveAndEnabled;
        });
        ladderContacts.RemoveWhere(c =>
        {
            if (c == null || !c.enabled || !c.gameObject.activeInHierarchy)
            {
                return true;
            }
            ItemGimmick item = c.GetComponentInParent<ItemGimmick>();
            return item == null || item.IsAttached;
        });

        isGround = supportContacts.Count > 0;
        isTouchingLadder = ladderContacts.Count > 0 &&
                           (actionController == null || !actionController.IsLadderEquipped);

        if (!isGround)
        {
            jumpConsumed = false;
        }

        if (crouchJump)
        {
            crouchJumpTime += Time.fixedDeltaTime;
            if (crouchJumpTime > 0.15f && isGround && rb.linearVelocity.y <= 0.1f)
                crouchJump = false;
        }
        UpdateCrouch();
        Move();

        bool jumpedThisStep = jumpRequested && isGround && !jumpConsumed;
        if (jumpedThisStep)
        {
            Jump();
            jumpConsumed = true;
        }
        // startedは一度だけ処理する。着地後まで入力要求を持ち越さない。
        jumpRequested = false;

        bool climbing = isTouchingLadder && jumpHeld && !jumpedThisStep && !IsCrouching;
        if (climbing)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = Mathf.Max(velocity.y, climbSpeed);
            rb.linearVelocity = velocity;
        }
        else if (wasClimbing && rb.linearVelocity.y > 0f)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;
        }
        wasClimbing = climbing;
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        moveInput = input.x;
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = 0f;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        jumpHeld = true;
        jumpRequested = true;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        jumpHeld = false;
        jumpRequested = false;
    }

    private void Move()
    {
        Vector3 velocity = rb.linearVelocity;
        float conveyorSpeed = 0f;
        float inputSpeedMultiplier = 1f;
        foreach (Collider contact in conveyorContacts)
        {
            ConveyorBelt conveyor = contact.GetComponentInParent<ConveyorBelt>();
            if (conveyor == null)
            {
                continue;
            }
            conveyorSpeed = Mathf.Max(conveyorSpeed, conveyor.BeltSpeed);
            inputSpeedMultiplier = Mathf.Min(inputSpeedMultiplier, conveyor.InputSpeedMultiplier);
        }
        velocity.z = moveInput * moveSpeed * inputSpeedMultiplier * (IsCrouching ? crouchMoveMultiplier : 1f) - conveyorSpeed;
        velocity.x = 0f;
        rb.linearVelocity = velocity;
    }

    private void OnCrouch(InputAction.CallbackContext context) => crouchHeld = true;
    private void OnCrouchCanceled(InputAction.CallbackContext context) => crouchHeld = false;

    private void UpdateCrouch()
    {
        if (bodyCollider == null) return;
        bool crouching = crouchHeld || crouchJump;
        if (!crouching && IsCrouching && !CanStand()) crouching = true;
        if (crouching == IsCrouching) return;
        IsCrouching = crouching;
        Vector3 size = standingSize;
        Vector3 center = standingCenter;
        if (crouching)
        {
            size.y *= crouchHeightRatio;
            // 足元の高さを変えずに上半分を縮める。
            center.y -= (standingSize.y - size.y) * 0.5f;
        }
        bodyCollider.size = size;
        bodyCollider.center = center;
    }

    private bool CanStand()
    {
        Transform body = bodyCollider.transform;
        // 立つと増える頭上部分のみ調べる。床は判定に含めない。
        float currentTop = bodyCollider.center.y + bodyCollider.size.y * 0.5f;
        float standingTop = standingCenter.y + standingSize.y * 0.5f;
        float height = standingTop - currentTop;
        if (height <= 0.001f) return true;
        Vector3 center = standingCenter;
        center.y = currentTop + height * 0.5f;
        Vector3 scale = body.lossyScale;
        scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        Vector3 half = Vector3.Scale(new Vector3(standingSize.x, height, standingSize.z) * 0.5f, scale);
        half = Vector3.Max(half - Vector3.one * 0.005f, Vector3.one * 0.001f);
        foreach (Collider hit in Physics.OverlapBox(body.TransformPoint(center), half,
                     body.rotation, standBlockingLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit == bodyCollider || hit.attachedRigidbody == rb ||
                hit.transform.IsChildOf(transform)) continue;
            return false;
        }
        return true;
    }

    private void Jump()
    {
        crouchJump = IsCrouching;
        crouchJumpTime = 0f;
        Vector3 velocity = rb.linearVelocity;
        velocity.y = jumpPower;
        rb.linearVelocity = velocity;
        Jumped?.Invoke();
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log(collision.gameObject+"と衝突");
        UpdateContacts(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        UpdateContacts(collision);
    }

    private void UpdateContacts(Collision collision)
    {
        Collider other = collision.collider;
        ItemGimmick item = other.GetComponentInParent<ItemGimmick>();
        ConveyorBelt conveyor = other.GetComponentInParent<ConveyorBelt>();

        if ((collision.gameObject.CompareTag("Ground")||collision.gameObject.CompareTag("MoveGround")) || item != null || conveyor != null)
        {
            jumpConsumed = false;
            bool standingOnTop = false;
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y > 0.5f)
                {
                    standingOnTop = true;
                    break;
                }
            }
            if (standingOnTop && (item == null || !item.IsAttached))
            {
                supportContacts.Add(other);
            }
            else
            {
                supportContacts.Remove(other);
            }

            if (standingOnTop && conveyor != null && conveyor.IsPlayer(gameObject))
            {
                conveyorContacts.Add(other);
            }
            else
            {
                conveyorContacts.Remove(other);
            }
        }

        if (item != null && db != null &&
            db.GetItemTypeById(item.Id) == ItemType.Ladder && !item.IsAttached)
        {
            ladderContacts.Add(other);
        }
        else
        {
            ladderContacts.Remove(other);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // 地面との離脱時にも呼ばれるため、ItemGimmickを前提にしない。
        supportContacts.Remove(collision.collider);
        ladderContacts.Remove(collision.collider);
        conveyorContacts.Remove(collision.collider);
    }

    // 拾ったオブジェクトはOnCollisionExitを経ずに破棄されることがある。
    public void ForgetWorldItem(GameObject itemObject)
    {
        if (itemObject == null)
        {
            return;
        }
        foreach (Collider itemCollider in itemObject.GetComponentsInChildren<Collider>(true))
        {
            supportContacts.Remove(itemCollider);
            ladderContacts.Remove(itemCollider);
        }
        isGround = supportContacts.Count > 0;
        isTouchingLadder = ladderContacts.Count > 0 &&
                           (actionController == null || !actionController.IsLadderEquipped);
    }
}
