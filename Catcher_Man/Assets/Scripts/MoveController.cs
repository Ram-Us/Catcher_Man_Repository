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
    [Header("移動・ジャンプの効果音")]
    [Tooltip("最初の空でないクリップを足音としてループ再生します")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip jumpClip;

    [Tooltip("1が標準。1より大きい値は増幅します")]
    [SerializeField, Range(0f, 1f)]
    private float footstepVolume = 0.7f;

    [Tooltip("1が標準。1より大きい値は増幅します")]
    [SerializeField, Range(0f, 1f)]
    private float jumpVolume = 0.8f;

    [Header("足音の再生速度")]
    [SerializeField, Range(0.1f, 2f)]
    private float normalFootstepPitch = 1f;

    [SerializeField, Range(0.1f, 2f)]
    private float crouchFootstepPitch = 0.65f;

    // PlayerMovementAudioから参照する設定。
    public float FootstepVolume => footstepVolume;
    public float JumpVolume => jumpVolume;
    public float NormalFootstepPitch => normalFootstepPitch;
    public float CrouchFootstepPitch => crouchFootstepPitch;

    // 自動歩行開始から登場演出終了までtrue。
    public bool IsIntroControlled => isIntroControlled;

    private Rigidbody rb;
    private ActionController actionController;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction crouchAction;
    private InputAction reStartAction;

    [Header("セーブポイント・再開")]
    [SerializeField] private string savePointTag = "SavePoint";
    [SerializeField] private GameObject restartEffectPrefab;
    [SerializeField] private Vector3 restartEffectOffset;
    [SerializeField, Min(0.1f)] private float restartEffectLifetime = 3f;
    [SerializeField] private Vector3 savedPosition;
    [Header("落下時の自動再開")]
    [SerializeField] private bool restartOnFall = true;
    [SerializeField] private float fallHeight = -20f;
    [SerializeField] private bool useFallZone = false;
    [SerializeField] private string fallZoneTag = "FallZone";
    private bool restartRequested;
    public Vector3 SavedPosition => savedPosition;

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
    private readonly HashSet<Collider> positiveZWalls = new();
    private readonly HashSet<Collider> negativeZWalls = new();
    private readonly HashSet<Collider> ladderContacts = new();
    private readonly HashSet<Collider> conveyorContacts = new();

    private float moveInput;
    public float MoveInput => moveInput;
    private bool jumpRequested;
    private bool jumpHeld;
    private bool jumpConsumed;

    // ジャンプ直後に接地判定が残っても、連続ジャンプさせないための時間。
    private float jumpElapsedTime;
    private const float JumpRetryDelay = 0.15f;

    // Triggerで検知したはしご。
    // 通常の衝突で検知するladderContactsとは分けて管理する。
    private readonly HashSet<Collider> ladderTriggerContacts = new();

    private bool wasClimbing;
    [SerializeField] private bool isGround;
    [SerializeField] private bool isTouchingLadder;
    public bool IsGrounded => isGround;
    public bool IsClimbing => wasClimbing;
    [SerializeField] private GameObject groundArea;
    private GroundCollisionArea groundSensor;
    [SerializeField] private BossPassedWall bossPassed;

    private bool isIntroControlled;
    private float introTargetZ;
    private float introMoveSpeed;
    private bool introWalking;

    public bool HasReachedIntroPoint { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        groundSensor = groundArea != null
            ? groundArea.GetComponent<GroundCollisionArea>()
            : GetComponentInChildren<GroundCollisionArea>();
        if (groundSensor == null)
            Debug.LogError("足元のGroundCollisionAreaをGround Areaに設定してください。", this);
        savedPosition = rb.position; // 未到達時は開始位置へ戻る。
        actionController = GetComponent<ActionController>();
        PlayerMovementAudio movementAudio = GetComponent<PlayerMovementAudio>();
        if (movementAudio == null)
        {
            movementAudio = gameObject.AddComponent<PlayerMovementAudio>();
        }
        movementAudio.SetDefaultClips(footstepClips, jumpClip);
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        crouchAction = InputSystem.actions.FindAction("Crouch");
        reStartAction = InputSystem.actions.FindAction("Restart");
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
        if (reStartAction != null) reStartAction.started += OnRestart;
        if (crouchAction != null)
        {
            crouchAction.performed += OnCrouch;
            crouchAction.canceled += OnCrouchCanceled;
        }
    }

    private void OnDisable()
    {
        moveInput = 0f;

if (rb != null && !rb.isKinematic)
{
    Vector3 velocity = rb.linearVelocity;
    velocity.z = 0f;
    rb.linearVelocity = velocity;
}
        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMoveCanceled;
        jumpAction.started -= OnJump;
        jumpAction.canceled -= OnJumpCanceled;
        if (reStartAction != null) reStartAction.started -= OnRestart;
        if (crouchAction != null)
        {
            crouchAction.performed -= OnCrouch;
            crouchAction.canceled -= OnCrouchCanceled;
        }
        restartRequested = false;
        crouchHeld = false;
        jumpHeld = false;
        jumpRequested = false;
        crouchJump = false;
        // 天井の下で無効化された場合にも、無理に立ち上がらない。
        
        positiveZWalls.Clear();
        negativeZWalls.Clear();
        supportContacts.Clear();
        ladderContacts.Clear();
        ladderTriggerContacts.Clear();
        jumpConsumed = false;
        jumpElapsedTime = 0f;
        conveyorContacts.Clear();
        
    }

    private void FixedUpdate()
    {

        if (isIntroControlled)
        {
            UpdateIntroMovement();
            return;
        }

        

        if ((restartRequested ||
            (restartOnFall && rb.position.y < fallHeight)) && !bossPassed.Flag)
        {
            PerformRestart();
            return;
        }

        supportContacts.RemoveWhere(c =>
            c == null ||
            !c.enabled ||
            !c.gameObject.activeInHierarchy);

        conveyorContacts.RemoveWhere(c =>
        {
            if (c == null ||
                !c.enabled ||
                !c.gameObject.activeInHierarchy)
            {
                return true;
            }

            ConveyorBelt conveyor =
                c.GetComponentInParent<ConveyorBelt>();

            return conveyor == null || !conveyor.isActiveAndEnabled;
        });

        // 通常のColliderで接触しているはしごを確認する。
        ladderContacts.RemoveWhere(c => !IsUsableLadder(c));

        // Triggerは、本体が実際に重なっている間だけ有効にする。
        ladderTriggerContacts.RemoveWhere(c =>
            !IsUsableLadder(c) || !IsBodyOverlapping(c));

        RefreshGroundContacts();
        RefreshLadderState();

        if (jumpConsumed)
        {
            jumpElapsedTime += Time.fixedDeltaTime;

            // 空中になった場合はジャンプ済み状態を解除。
            // 頭をぶつけて離陸できなかった場合も、
            // 接地して上昇が止まれば再入力できるようにする。
            if (!isGround ||
                (jumpElapsedTime >= JumpRetryDelay &&
                rb.linearVelocity.y <= 0.1f))
            {
                jumpConsumed = false;
            }
        }

        if (crouchJump)
        {
            crouchJumpTime += Time.fixedDeltaTime;

            if (crouchJumpTime > 0.15f &&
                isGround &&
                rb.linearVelocity.y <= 0.1f)
            {
                crouchJump = false;
            }
        }

        UpdateCrouch();
        Move();

        bool jumpedThisStep =
            jumpRequested && isGround && !jumpConsumed;

        if (jumpedThisStep)
        {
            Jump();

            jumpConsumed = true;
            jumpElapsedTime = 0f;
        }

        // 1回の入力で1回だけ判定する。
        jumpRequested = false;

        bool climbing =
            isTouchingLadder &&
            jumpHeld &&
            !jumpedThisStep &&
            !IsCrouching;

        if (climbing)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = Mathf.Max(velocity.y, climbSpeed);
            rb.linearVelocity = velocity;
        }
        else if (wasClimbing &&
                !jumpedThisStep &&
                rb.linearVelocity.y > 0f)
        {
            // はしご登りの終了時だけ上昇を止める。
            // 今回実行した通常ジャンプは打ち消さない。
            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;
        }

        wasClimbing = climbing;
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleCheckpointTrigger(other);
        UpdateLadderTrigger(other);
    }

    private void OnTriggerStay(Collider other)
    {
        HandleCheckpointTrigger(other);
        UpdateLadderTrigger(other);
    }

    private void OnTriggerExit(Collider other)
    {
        // サーチ用Triggerの離脱だけで、本体の接触を消さない。
        if (!IsBodyOverlapping(other))
        {
            ladderTriggerContacts.Remove(other);
        }
    }

    private void UpdateLadderTrigger(Collider other)
    {
        // サーチ範囲に入っただけでは登れない。
        // 本体のBoxColliderがはしごに重なっている必要がある。
        if (IsUsableLadder(other) && IsBodyOverlapping(other))
        {
            ladderTriggerContacts.Add(other);
        }
        else
        {
            ladderTriggerContacts.Remove(other);
        }
    }

    private void HandleCheckpointTrigger(Collider other)
    {
        if (restartRequested || !other.isTrigger) return;

        if (useFallZone && other.CompareTag(fallZoneTag))
        {
            if (IsBodyOverlapping(other)) Restart();
            return;
        }
        SaveCheckpoint(other);
    }

    private bool IsBodyOverlapping(Collider other)
    {
        if (bodyCollider == null || !bodyCollider.enabled) return false;
        return Physics.ComputePenetration(
            bodyCollider, bodyCollider.transform.position, bodyCollider.transform.rotation,
            other, other.transform.position, other.transform.rotation,
            out _, out _);
    }

    private void SaveCheckpoint(Collider other)
    {
        if (restartRequested || !other.isTrigger ||
            !other.CompareTag(savePointTag)) return;

        if (!IsBodyOverlapping(other)) return;

        // positionは親の位置・回転・Scaleを反映済みのワールド座標。
        // 新しく追加したSavePointも、触れるだけでこの保存値を更新する。
        savedPosition = other.transform.position;
    }

    private void OnRestart(InputAction.CallbackContext context)
    {
        Restart();
    }

    // UI ButtonのOn Clickからも呼べる。
    public void Restart()
    {
        if (!isActiveAndEnabled) return;
        restartRequested = true;
    }

    private void PerformRestart()
    {
        restartRequested = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = savedPosition;
        transform.position = savedPosition;

        // 移動前の地面・はしご・コンベアの判定を持ち越さない。
        positiveZWalls.Clear();
        negativeZWalls.Clear();
        supportContacts.Clear();
        ladderContacts.Clear();
        ladderTriggerContacts.Clear();
        jumpConsumed = false;
        jumpElapsedTime = 0f;
        conveyorContacts.Clear();
        if (groundSensor != null) groundSensor.ClearContacts();
        isGround = false;
        isTouchingLadder = false;
        wasClimbing = false;
        jumpRequested = false;
        jumpHeld = false;
        jumpConsumed = false;
        crouchJump = false;
        crouchJumpTime = 0f;

        // ワープ後の位置で頭上を判定してしゃがみを更新する。
        Physics.SyncTransforms();
        UpdateCrouch();

        if (restartEffectPrefab != null)
        {
            GameObject effect = Instantiate(restartEffectPrefab,
                savedPosition + restartEffectOffset, Quaternion.identity);
            foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>())
                particle.Play(false);
            Destroy(effect, restartEffectLifetime);
        }
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        // 演出中の移動方向は、自動歩行側で決める。
        if (isIntroControlled)
        {
            return;
        }

        moveInput = context.ReadValue<Vector2>().x;
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        if (isIntroControlled)
        {
            return;
        }

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
        // 空中では壁へ押し込む速度だけを止める。Y速度や反対方向の入力は維持する。
        positiveZWalls.RemoveWhere(IsInvalidWall);
        negativeZWalls.RemoveWhere(IsInvalidWall);
        if (!isGround &&
            ((velocity.z > 0f && positiveZWalls.Count > 0) ||
             (velocity.z < 0f && negativeZWalls.Count > 0)))
        {
            velocity.z = 0f;
        }
        velocity.x = 0f;
        rb.linearVelocity = velocity;
    }

    private static bool IsInvalidWall(Collider wall)
    {
        return wall == null || !wall.enabled || !wall.gameObject.activeInHierarchy;
    }

    private void UpdateWallContacts(Collision collision)
    {
        Collider other = collision.collider;
        bool blocksPositiveZ = false;
        bool blocksNegativeZ = false;
        if (other.CompareTag("Ground") || other.CompareTag("MoveGround"))
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                // 装備など別Colliderの接触を本体の壁接触として扱わない。
                if (bodyCollider != null && contact.thisCollider != bodyCollider) continue;
                Vector3 normal = contact.normal;
                if (Mathf.Abs(normal.y) >= 0.5f) continue;
                blocksPositiveZ |= normal.z < -0.5f;
                blocksNegativeZ |= normal.z > 0.5f;
            }
        }
        if (blocksPositiveZ) positiveZWalls.Add(other);
        else positiveZWalls.Remove(other);
        if (blocksNegativeZ) negativeZWalls.Add(other);
        else negativeZWalls.Remove(other);
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
        UpdateWallContacts(collision);
        Collider other = collision.collider;
        ItemGimmick item = other.GetComponentInParent<ItemGimmick>();
        ConveyorBelt conveyor = other.GetComponentInParent<ConveyorBelt>();

        if (((collision.gameObject.CompareTag("Ground")||collision.gameObject.CompareTag("MoveGround")) || item != null || conveyor != null)
            && groundSensor != null && groundSensor.IsTouching(other))
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

            if (standingOnTop && conveyor != null && conveyor.IsPlayer(gameObject))
            {
                conveyorContacts.Add(other);
            }
            else
            {
                conveyorContacts.Remove(other);
            }
        }

        else
        {
            // 足元が離れた場合も、以前の接地・コンベア情報を必ず解除する。
            supportContacts.Remove(other);
            conveyorContacts.Remove(other);
        }

        if (IsUsableLadder(other))
        {
            ladderContacts.Add(other);
        }
        else
        {
            ladderContacts.Remove(other);
        }
    }

    private void RefreshGroundContacts()
    {
        if (groundSensor == null || !groundSensor.isActiveAndEnabled || !groundSensor.IsGround)
        {
            supportContacts.Clear();
            conveyorContacts.Clear();
            isGround = false;
            return;
        }
        supportContacts.RemoveWhere(c => !groundSensor.IsTouching(c));
        conveyorContacts.RemoveWhere(c => !supportContacts.Contains(c));
        isGround = supportContacts.Count > 0;
    }

    private void OnCollisionExit(Collision collision)
    {
        // 地面との離脱時にも呼ばれるため、ItemGimmickを前提にしない。
        positiveZWalls.Remove(collision.collider);
        negativeZWalls.Remove(collision.collider);
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

        foreach (Collider itemCollider in
                itemObject.GetComponentsInChildren<Collider>(true))
        {
            supportContacts.Remove(itemCollider);
            ladderContacts.Remove(itemCollider);
            ladderTriggerContacts.Remove(itemCollider);
            conveyorContacts.Remove(itemCollider);
        }

        RefreshGroundContacts();
        RefreshLadderState();
    }

    // 設置されたはしごかどうかを判定する。
    private bool IsUsableLadder(Collider other)
    {
        if (other == null ||
            !other.enabled ||
            !other.gameObject.activeInHierarchy ||
            db == null)
        {
            return false;
        }

        ItemGimmick item =
            other.GetComponentInParent<ItemGimmick>();

        return item != null &&
            !item.IsAttached &&
            db.GetItemTypeById(item.Id) == ItemType.Ladder;
    }

    // CollisionまたはTriggerで触れていれば、はしごに接触中。
    private void RefreshLadderState()
    {
        bool touching =
            ladderContacts.Count > 0 ||
            ladderTriggerContacts.Count > 0;

        // はしごを装備中は登れない、という既存の仕様を維持する。
        isTouchingLadder =
            touching &&
            (actionController == null ||
            !actionController.IsLadderEquipped);
    }

    public void BeginIntroWalk(float targetZ, float speed)
    {
        isIntroControlled = true;
        introWalking = true;
        HasReachedIntroPoint = false;

        introTargetZ = targetZ;
        introMoveSpeed = Mathf.Max(0.1f, speed);

        moveInput = 0f;
        jumpRequested = false;
        jumpHeld = false;
        crouchHeld = false;
        crouchJump = false;
        restartRequested = false;
        wasClimbing = false;

        UpdateCrouch();
    }

    public void EndIntroControl()
    {
        isIntroControlled = false;
        introWalking = false;
        moveInput = 0f;

        jumpRequested = false;
        jumpHeld = false;
        restartRequested = false;

        if (!rb.isKinematic)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.z = 0f;
            rb.linearVelocity = velocity;
        }
    }

    private void UpdateIntroMovement()
    {
        // 足元の接地情報を更新し、既存のSprite制御を使う。
        RefreshGroundContacts();
        UpdateCrouch();

        float difference = introTargetZ - rb.position.z;

        if (introWalking &&
            Mathf.Abs(difference) <= 0.05f &&
            isGround)
        {
            introWalking = false;
            HasReachedIntroPoint = true;
        }

        float speedZ = 0f;

        if (introWalking && Mathf.Abs(difference) > 0.05f)
        {
            // 目的地を通り越さない速度にする。
            speedZ = Mathf.Clamp(
                difference / Time.fixedDeltaTime,
                -introMoveSpeed,
                introMoveSpeed
            );
        }

        // PlayerSpriteControllerがこの値を使って左右・歩行を判断する。
        moveInput = Mathf.Abs(speedZ) > 0.01f
            ? Mathf.Sign(speedZ)
            : 0f;

        Vector3 velocity = rb.linearVelocity;
        velocity.x = 0f;
        velocity.z = speedZ;

        // Y速度は維持し、重力と接地を通常どおり働かせる。
        rb.linearVelocity = velocity;
    }
}
