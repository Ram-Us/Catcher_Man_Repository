using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class MoveController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpPower = 7f;
    [SerializeField] private float climbSpeed = 1f;

    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction jumpAction;

    private float moveInput;
    private bool jumpRequested, isGround,isItem, isTouchedItem, isTouchingLadder, climbRequested;
    [SerializeField] private ItemDataBase db;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
    }

    private void OnEnable()
    {
        moveAction.performed += OnMove;
        moveAction.canceled += OnMoveCanceled;
        jumpAction.started += OnJump;
        jumpAction.performed += OnUp;
        jumpAction.canceled += OnJumpCanceled;
    }

    private void OnDisable()
    {
        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMoveCanceled;
        jumpAction.started -= OnJump;
        jumpAction.performed -= OnUp;
        jumpAction.canceled -= OnJumpCanceled;
    }

    private void FixedUpdate()
    {
        
        Move();
        
        

        
        //Debug.Log(jumpRequested + "and" + isGround);

        if ((jumpRequested && isGround)||(jumpRequested && isItem))
        {
            if (!isTouchingLadder)
            {
                Jump();
                Debug.Log("ホップステップジャンプ！");
            }
            if (isGround)
            {
                isGround = false;
            }else if (isItem)
            {
                isItem = false;
            }
            
            jumpRequested = false;
        }
        

        if (isTouchingLadder && climbRequested)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = climbSpeed;
            rb.linearVelocity = velocity;
        }
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();

        // 入力上の左右方向を、ワールド座標のZ軸に使う
        moveInput = input.x;
        
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = 0f;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isTouchingLadder)
        {
            climbRequested = true;
        }
        else
        {
            jumpRequested = true;
        }
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        climbRequested = false;
    }
    private void OnUp(InputAction.CallbackContext context)
    {
        
    }

    private void Move()
    {
        Vector3 velocity = rb.linearVelocity;

        velocity.z = moveInput * moveSpeed;

        // X方向には移動させない
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
        if (collision.gameObject.CompareTag("Ground"))
        {
            //Debug.Log(isGround);
            isGround = true;
        }
        if (collision.gameObject.CompareTag("Item"))
        {
            isItem = true;
        }

         ItemGimmick item = collision.gameObject.GetComponentInParent<ItemGimmick>();

        if (item != null &&
            db.GetItemTypeById(item.Id) == ItemType.Ladder)
        {
            isTouchingLadder = true;
        }
    }

    
    
    private void OnCollisionExit(Collision collision)
    {
        
        ItemGimmick item = collision.gameObject.GetComponentInParent<ItemGimmick>();

        if (item != null &&
            db.GetItemTypeById(item.Id) == ItemType.Ladder)
        {
            isTouchingLadder = false;
            climbRequested = false;
        }
        
    }
}