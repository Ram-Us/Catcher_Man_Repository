using UnityEngine;

public class PlayerSpriteController : MonoBehaviour
{
    [System.Serializable]
    private class DirectionSprites
    {
        public Sprite idle;
        public Sprite crouch;
        public Sprite[] move = new Sprite[0];
        [Tooltip("0:予備動作 1:踏み切り 2:上昇 3:頂点 4:落下 5:着地")]
        public Sprite[] jump = new Sprite[6];
    }

    private enum VisualState { Idle, Move, Air, Land, Crouch }

    [SerializeField] private MoveController moveController;
    [SerializeField] private Rigidbody playerRigidbody;
    [SerializeField] private SpriteRenderer playerRenderer;
    [SerializeField] private DirectionSprites right = new DirectionSprites();
    [SerializeField] private DirectionSprites left = new DirectionSprites();
    [SerializeField] private bool startFacingLeft;
    [SerializeField, Min(1f)] private float moveFramesPerSecond = 10f;
    [SerializeField, Min(0f)] private float apexSpeed = 0.5f;
    [SerializeField, Min(0f)] private float takeoffDuration = 0.08f;
    [SerializeField, Min(0f)] private float landingDuration = 0.1f;

    private bool facingLeft;
    private VisualState state;
    private float stateElapsed;
    private bool stateInitialized;

    private void Awake()
    {
        if (moveController == null)
            moveController = GetComponentInParent<MoveController>();
        if (playerRigidbody == null && moveController != null)
            playerRigidbody = moveController.GetComponent<Rigidbody>();
        if (playerRenderer == null)
            playerRenderer = GetComponent<SpriteRenderer>();

        if (moveController == null || playerRigidbody == null || playerRenderer == null)
        {
            Debug.LogError("MoveController、プレイヤーのRigidbody、表示用SpriteRendererを設定してください。", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        facingLeft = startFacingLeft;
        stateInitialized = false;
        stateElapsed = 0f;
    }

    private void LateUpdate()
    {
        float input = moveController.MoveInput;
        if (!moveController.IsCrouchJumping && Mathf.Abs(input) > 0.01f)
            facingLeft = input < 0f;

        bool airborne = !moveController.IsGrounded && !moveController.IsClimbing;
        bool moving = Mathf.Abs(input) > 0.01f &&
                      Mathf.Abs(playerRigidbody.linearVelocity.z) > 0.05f;
        VisualState nextState;

        if (moveController.IsCrouching)
            nextState = VisualState.Crouch;
        else if (airborne)
            nextState = VisualState.Air;
        else if (!moveController.IsClimbing && stateInitialized && state == VisualState.Air)
            nextState = VisualState.Land;
        else if (stateInitialized && state == VisualState.Land &&
                 stateElapsed < landingDuration && !moving && !moveController.IsClimbing)
            nextState = VisualState.Land;
        else
            nextState = moving ? VisualState.Move : VisualState.Idle;

        if (!stateInitialized || nextState != state)
        {
            state = nextState;
            stateElapsed = 0f;
            stateInitialized = true;
        }

        DirectionSprites sprites = facingLeft ? left : right;
        Sprite selected = sprites.idle;

        switch (state)
        {
            case VisualState.Crouch:
                selected = sprites.crouch != null ? sprites.crouch : sprites.idle;
                break;

            case VisualState.Move:
                if (sprites.move != null && sprites.move.Length > 0)
                {
                    int frame = Mathf.FloorToInt(stateElapsed * moveFramesPerSecond)
                                % sprites.move.Length;
                    selected = sprites.move[frame];
                }
                break;

            case VisualState.Air:
                float speedY = playerRigidbody.linearVelocity.y;
                int jumpFrame = speedY > apexSpeed
                    ? (stateElapsed < takeoffDuration ? 1 : 2)
                    : speedY < -apexSpeed ? 4 : 3;
                selected = GetJumpSprite(sprites, jumpFrame);
                break;

            case VisualState.Land:
                selected = GetJumpSprite(sprites, 5);
                break;
        }

        // 左右は別画像を使うため、SpriteRendererによる二重反転はしない。
        playerRenderer.flipX = false;
        if (selected != null)
            playerRenderer.sprite = selected;
        else if (sprites.idle != null)
            playerRenderer.sprite = sprites.idle;

        stateElapsed += Time.deltaTime;
    }

    private static Sprite GetJumpSprite(DirectionSprites sprites, int frame)
    {
        if (sprites.jump != null && frame < sprites.jump.Length && sprites.jump[frame] != null)
            return sprites.jump[frame];
        return sprites.idle;
    }
}
