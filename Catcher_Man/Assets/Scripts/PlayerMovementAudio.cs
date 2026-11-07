using UnityEngine;

[RequireComponent(typeof(MoveController), typeof(Rigidbody))]
public class PlayerMovementAudio : MonoBehaviour
{
    [Header("足音専用のAudioSource")]
    [Tooltip("未設定なら自動作成します。BGM用とは分けてください")]
    [SerializeField] private AudioSource audioSource;

    [Header("プレイヤー本体のCollider")]
    [Tooltip("足元・サーチ用ではなく、Is TriggerがOFFの本体")]
    [SerializeField] private BoxCollider bodyCollider;

    [Header("確認用")]
    [SerializeField] private bool showFootstepLog;

    private MoveController moveController;
    private Rigidbody rb;
    private AudioSource jumpSource;

    private AudioClip[] defaultFootsteps;
    private AudioClip jumpClip;
    private AudioClip currentFootstep;

    private Vector3 previousPosition;
    private bool skipStep;

    private void Awake()
    {
        moveController = GetComponent<MoveController>();
        rb = GetComponent<Rigidbody>();

        if (bodyCollider == null)
        {
            foreach (BoxCollider candidate in GetComponents<BoxCollider>())
            {
                if (!candidate.isTrigger)
                {
                    bodyCollider = candidate;
                    break;
                }
            }
        }

        if (bodyCollider == null || bodyCollider.isTrigger)
        {
            Debug.LogError(
                "PlayerMovementAudioのBody Colliderに、" +
                "Is TriggerがOFFの本体BoxColliderを設定してください。",
                this);
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f;
        audioSource.Stop();

        jumpSource = gameObject.AddComponent<AudioSource>();
        jumpSource.playOnAwake = false;
        jumpSource.loop = false;
        jumpSource.spatialBlend = 0f;
        jumpSource.volume = 1f;
    }

    private void OnEnable()
    {
        moveController.Jumped += PlayJump;
        previousPosition = rb.position;
        skipStep = false;
    }

    private void OnDisable()
    {
        if (moveController != null)
        {
            moveController.Jumped -= PlayJump;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        if (jumpSource != null)
        {
            jumpSource.Stop();
        }
    }

    private void Update()
    {
        // 停止中でも、エリアから出た・エリアが消えた場合は音を戻す。
        RefreshFootstepClip();

        audioSource.volume = moveController.FootstepVolume;
        audioSource.pitch = moveController.IsCrouching
            ? moveController.CrouchFootstepPitch
            : moveController.NormalFootstepPitch;

        if (Time.timeScale <= 0f ||
            !moveController.isActiveAndEnabled)
        {
            audioSource.Stop();
            previousPosition = rb.position;
        }
    }

    private void RefreshFootstepClip()
    {
        AudioClip areaClip =
            FootstepChangeArea.GetFootstepClip(bodyCollider);

        AudioClip selectedClip = areaClip != null
            ? areaClip
            : FirstClip(defaultFootsteps);

        if (currentFootstep == selectedClip)
        {
            return;
        }

        currentFootstep = selectedClip;
        audioSource.Stop();
        audioSource.clip = currentFootstep;

        if (showFootstepLog)
        {
            string clipName = currentFootstep != null
                ? currentFootstep.name
                : "未設定";

            string type = areaClip != null
                ? "エリアの音"
                : "標準の音";

            Debug.Log($"足音切り替え：{clipName}／{type}", this);
        }
    }

    private void FixedUpdate()
    {
        float traveled =
            Mathf.Abs(rb.position.z - previousPosition.z);

        previousPosition = rb.position;

        RefreshFootstepClip();

        bool walking =
            moveController.isActiveAndEnabled &&
            moveController.IsGrounded &&
            !moveController.IsClimbing &&
            Mathf.Abs(moveController.MoveInput) > 0.01f &&
            traveled > 0.001f;

        if (!walking || skipStep || currentFootstep == null)
        {
            audioSource.Stop();
            skipStep = false;
            return;
        }

        audioSource.volume = moveController.FootstepVolume;
        audioSource.pitch = moveController.IsCrouching
            ? moveController.CrouchFootstepPitch
            : moveController.NormalFootstepPitch;

        if (audioSource.clip != currentFootstep)
        {
            audioSource.Stop();
            audioSource.clip = currentFootstep;
        }

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    private void PlayJump()
    {
        skipStep = true;
        audioSource.Stop();

        if (jumpClip != null)
        {
            jumpSource.PlayOneShot(
                jumpClip,
                moveController.JumpVolume);
        }
    }

    // MoveControllerのAwakeから受け取る。
    public void SetDefaultClips(
        AudioClip[] footsteps,
        AudioClip jump)
    {
        defaultFootsteps = footsteps;
        jumpClip = jump;
    }

    // 既存コードとの互換用。標準の足音を変更する。
    public void SetFootstepLoop(AudioClip clip)
    {
        if (clip != null)
        {
            defaultFootsteps = new AudioClip[] { clip };
        }
    }

    public void SetFootsteps(AudioClip[] clips)
    {
        defaultFootsteps = clips;
    }

    private static AudioClip FirstClip(AudioClip[] clips)
    {
        if (clips == null)
        {
            return null;
        }

        foreach (AudioClip clip in clips)
        {
            if (clip != null)
            {
                return clip;
            }
        }

        return null;
    }

    private void OnDestroy()
    {
        if (jumpSource != null)
        {
            Destroy(jumpSource);
        }
    }
}