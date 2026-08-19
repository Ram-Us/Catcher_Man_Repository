using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class MoveProtoType : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpPower = 7f;

    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction jumpAction;

    private float moveInput;
    private bool jumpRequested,isGround;


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
    }

    private void OnDisable()
    {
        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMoveCanceled;
        jumpAction.started -= OnJump;
    }

    private void FixedUpdate()
    {
        Move();

        
        //Debug.Log(jumpRequested + "and" + isGround);

        if (jumpRequested && isGround)
        {
            
            Jump();
            isGround = false;
            
            
        }
        jumpRequested = false;
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
        // 入力イベントでは要求だけを記録する
        jumpRequested = true;
        
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
        
    }
}