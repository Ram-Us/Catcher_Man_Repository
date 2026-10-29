using UnityEngine;

[RequireComponent(typeof(MoveController), typeof(Rigidbody))]
public class PlayerMovementAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] defaultFootsteps;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField, Min(0.05f)] private float stepInterval = 0.35f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float jumpVolume = 0.8f;

    private MoveController moveController;
    private Rigidbody rb;
    private AudioClip[] currentFootsteps;
    private float stepTimer;
    private Vector3 previousPosition;
    private bool skipStep;

    private void Awake()
    {
        moveController = GetComponent<MoveController>();
        rb = GetComponent<Rigidbody>();
        if (audioSource == null)
        {
            // 他のSEの設定に影響しないよう、未指定なら専用Sourceを作る。
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }
        currentFootsteps = defaultFootsteps;
    }

    private void OnEnable()
    {
        moveController.Jumped += PlayJump;
        previousPosition = rb.position;
        stepTimer = 0f;
    }

    private void OnDisable()
    {
        moveController.Jumped -= PlayJump;
    }

    private void FixedUpdate()
    {
        float traveled = Mathf.Abs(rb.position.z - previousPosition.z);
        previousPosition = rb.position;
        bool walking = moveController.IsGrounded && !moveController.IsClimbing &&
                       Mathf.Abs(moveController.MoveInput) > 0.01f && traveled > 0.001f;
        if (!walking || skipStep)
        {
            stepTimer = 0f;
            skipStep = false;
            return;
        }

        stepTimer -= Time.fixedDeltaTime;
        if (stepTimer > 0f) return;
        stepTimer = Mathf.Max(0.05f, stepInterval) * (moveController.IsCrouching ? 1.5f : 1f);
        if (currentFootsteps == null || currentFootsteps.Length == 0) return;
        AudioClip clip = currentFootsteps[Random.Range(0, currentFootsteps.Length)];
        if (clip != null) audioSource.PlayOneShot(clip, footstepVolume);
    }

    private void PlayJump()
    {
        skipStep = true;
        if (jumpClip != null) audioSource.PlayOneShot(jumpClip, jumpVolume);
    }

    // 切り替え地点を離れても維持する。次の地点で再変更する。
    public void SetFootsteps(AudioClip[] clips)
    {
        currentFootsteps = clips != null && clips.Length > 0 ? clips : defaultFootsteps;
        stepTimer = 0f;
    }
}
