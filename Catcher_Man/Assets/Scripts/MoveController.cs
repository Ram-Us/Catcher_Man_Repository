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
    private readonly HashSet<Collider> supportContacts = new();
    private readonly HashSet<Collider> ladderContacts = new();

    private float moveInput;
    private bool jumpRequested;
    private bool jumpHeld;
    private bool jumpConsumed;
    private bool wasClimbing;
    [SerializeField] private bool isGround;
    [SerializeField] private bool isTouchingLadder;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        actionController = GetComponent<ActionController>();
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
    }

    private void OnEnable()
    {
        moveAction.performed += OnMove;
        moveAction.canceled += OnMoveCanceled;
        jumpAction.started += OnJump;
        jumpAction.canceled += OnJumpCanceled;
    }

    private void OnDisable()
    {
        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMoveCanceled;
        jumpAction.started -= OnJump;
        jumpAction.canceled -= OnJumpCanceled;
        jumpRequested = false;
        jumpHeld = false;
        wasClimbing = false;
        jumpConsumed = false;
        supportContacts.Clear();
        ladderContacts.Clear();
        isGround = false;
        isTouchingLadder = false;
    }

    private void FixedUpdate()
    {
        // Destroy済みの接触相手が残っても接地やはしご扱いを続けない。
        supportContacts.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
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

        Move();

        bool jumpedThisStep = jumpRequested && isGround && !jumpConsumed;
        if (jumpedThisStep)
        {
            Jump();
            jumpConsumed = true;
        }
        // startedは一度だけ処理する。着地後まで入力要求を持ち越さない。
        jumpRequested = false;

        bool climbing = isTouchingLadder && jumpHeld && !jumpedThisStep;
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
        velocity.z = moveInput * moveSpeed;
        velocity.x = 0f;
        rb.linearVelocity = velocity;
    }

    private void Jump()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.y = jumpPower;
        rb.linearVelocity = velocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
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

        if (collision.gameObject.CompareTag("Ground") || item != null)
        {
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
