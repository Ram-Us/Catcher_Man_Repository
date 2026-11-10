using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class AmbientSoundArea : MonoBehaviour
{
    [Header("プレイヤー本体")]
    [Tooltip("TestPlayer本体の、Is TriggerがOFFのBoxCollider")]
    [SerializeField] private BoxCollider playerBody;

    [Header("環境音")]
    [SerializeField] private AudioClip ambientClip;

    [Tooltip("環境音専用のAudioSource。未設定なら子に自動作成")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField, Range(0f, 1f)]
    private float volume = 0.7f;

    private BoxCollider area;
    private GameObject generatedSoundObject;
    private bool pausedByGame;

    private void Awake()
    {
        area = GetComponent<BoxCollider>();

        if (playerBody == null ||
            playerBody.isTrigger ||
            !area.isTrigger ||
            ambientClip == null)
        {
            Debug.LogError(
                "AmbientSoundAreaの設定を確認してください。" +
                "プレイヤー本体はIs Trigger OFF、" +
                "エリアはIs Trigger ON、環境音の登録が必要です。",
                this);

            enabled = false;
            return;
        }

        if (audioSource == null)
        {
            generatedSoundObject = new GameObject("AmbientAudio");
            generatedSoundObject.transform.SetParent(transform, false);

            audioSource =
                generatedSoundObject.AddComponent<AudioSource>();
        }

        audioSource.Stop();
        audioSource.playOnAwake = false;
        audioSource.loop = true;

        // エリア内ではカメラとの距離によらず聞こえる。
        audioSource.spatialBlend = 0f;
        audioSource.clip = ambientClip;
        audioSource.volume = volume;
    }

    private void Update()
    {
        if (playerBody == null ||
            !playerBody.enabled ||
            playerBody.isTrigger ||
            !playerBody.gameObject.activeInHierarchy ||
            !area.enabled ||
            !area.isTrigger)
        {
            StopSound();
            return;
        }

        // サーチ用Colliderではなく、本体との重なりを調べる。
        bool inside = Physics.ComputePenetration(
            playerBody,
            playerBody.transform.position,
            playerBody.transform.rotation,
            area,
            area.transform.position,
            area.transform.rotation,
            out _,
            out _
        );

        audioSource.volume = volume;

        if (!inside)
        {
            StopSound();
            return;
        }

        // ゲーム時間停止中は環境音も一時停止する。
        if (Time.timeScale <= 0f)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
                pausedByGame = true;
            }

            return;
        }

        if (pausedByGame)
        {
            audioSource.UnPause();
            pausedByGame = false;
        }

        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    private void StopSound()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        pausedByGame = false;
    }

    private void OnDisable()
    {
        StopSound();
    }

    private void OnDestroy()
    {
        if (generatedSoundObject != null)
        {
            Destroy(generatedSoundObject);
        }
    }
}