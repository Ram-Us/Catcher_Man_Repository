using System.Collections;
using UnityEngine;

public class BossBgmController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private BossController bossController;
    [SerializeField] private MoveController playerMove;

    [Tooltip("BGM専用のAudioSource。足音やTimeline用と分けてください")]
    [SerializeField] private AudioSource bgmAudioSource;

    [Header("通常BGM")]
    [SerializeField] private AudioClip normalBgmClip;

    [SerializeField, Range(0f, 1f)]
    private float normalBgmVolume = 0.7f;

    [Header("ボス戦BGM")]
    [SerializeField] private AudioClip battleBgmClip;

    [SerializeField, Range(0f, 1f)]
    private float battleBgmVolume = 0.8f;

    [Header("勝利BGM")]
    [SerializeField] private AudioClip victoryBgmClip;
    [SerializeField, Range(0f, 5f)] private float victoryBgmVolume = 1f;

    [Header("撃破時のフェードアウト")]
    [SerializeField, Min(0f)]
    private float fadeOutDuration = 2f;

    private bool stoppedForGameOver;
    private bool hasBattleStarted;
    private bool hasDefeatStarted;
    private bool introMuted;
    private Coroutine fadeOutCoroutine;

    private void Start()
    {
        if (bossController == null)
        {
            bossController = FindFirstObjectByType<BossController>();
        }

        if (playerMove == null)
        {
            playerMove = FindFirstObjectByType<MoveController>();
        }

        if (bgmAudioSource == null)
        {
            bgmAudioSource = GetComponent<AudioSource>();
        }

        if (bgmAudioSource == null)
        {
            bgmAudioSource = gameObject.AddComponent<AudioSource>();
        }

        bgmAudioSource.playOnAwake = false;
        bgmAudioSource.loop = true;
        bgmAudioSource.spatialBlend = 0f;
        bgmAudioSource.Stop();

        if (bossController == null || playerMove == null)
        {
            Debug.LogError(
                "BossBgmControllerのBoss ControllerとPlayer Moveを設定してください。",
                this);

            enabled = false;
            return;
        }

        // 最初から演出中・戦闘中の場合も状態に合わせる。
        UpdateBgmState();
    }

    private void Update()
    {
        UpdateBgmState();
    }

    private void UpdateBgmState()
    {
        if (stoppedForGameOver || hasDefeatStarted)
        {
            return;
        }

        // 戦闘開始後、ボスが破壊・非表示になったら停止する。
        // null判定で先にreturnせず、フェードアウトにつなげる。
        if (hasBattleStarted &&
            (bossController == null ||
             !bossController.gameObject.activeInHierarchy))
        {
            StartBgmFadeOut();
            return;
        }

        if (hasBattleStarted)
        {
            // Inspectorでの音量変更を即時反映。
            bgmAudioSource.volume = battleBgmVolume;
            return;
        }

        // 自動歩行開始からムービー終了まで通常BGMを止める。
        if (playerMove != null && playerMove.IsIntroControlled)
        {
            if (!introMuted)
            {
                bgmAudioSource.Stop();
                introMuted = true;
            }

            return;
        }

        // 演出制御が解除され、ボスが戦闘開始したら再生する。
        if (bossController != null && bossController.IsBattled)
        {
            SwitchToBattleBgm();
            return;
        }

        // 通常時。演出が失敗・中断して戻った場合も通常BGMへ戻す。
        introMuted = false;
        PlayNormalBgm();
    }

    private void PlayNormalBgm()
    {
        bgmAudioSource.volume = normalBgmVolume;

        if (normalBgmClip == null)
        {
            return;
        }

        if (bgmAudioSource.clip != normalBgmClip)
        {
            bgmAudioSource.Stop();
            bgmAudioSource.clip = normalBgmClip;
        }

        if (!bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Play();
        }
    }

    private void SwitchToBattleBgm()
    {
        // 一度だけ切り替える。
        hasBattleStarted = true;
        introMuted = false;

        bgmAudioSource.Stop();

        if (battleBgmClip == null)
        {
            Debug.LogError("Battle Bgm Clipが未設定です。", this);
            return;
        }

        bgmAudioSource.clip = battleBgmClip;
        bgmAudioSource.volume = battleBgmVolume;
        bgmAudioSource.Play();
    }

    // ゲームオーバー時は即座に停止し、その後の自動再生も防ぐ。
    public void StopBgmForGameOver()
    {
        stoppedForGameOver = true;

        if (fadeOutCoroutine != null)
        {
            StopCoroutine(fadeOutCoroutine);
            fadeOutCoroutine = null;
        }

        if (bgmAudioSource == null)
            bgmAudioSource = GetComponent<AudioSource>();

        if (bgmAudioSource != null)
            bgmAudioSource.Stop();
    }

    // ボスの撃破処理から呼ぶこともできる。
    public void StartBgmFadeOut()
    {
        if (stoppedForGameOver || hasDefeatStarted)
        {
            return;
        }

        hasDefeatStarted = true;

        if (fadeOutCoroutine != null)
        {
            StopCoroutine(fadeOutCoroutine);
        }

        fadeOutCoroutine = StartCoroutine(FadeOutAndStopBgm());
    }

    public void PlayVictoryBgm()
    {
        if (stoppedForGameOver) return;

        if (victoryBgmClip == null)
        {
            Debug.LogError("Victory Bgm Clipが未設定です。", this);
            return;
        }

        hasDefeatStarted = true;
        if (fadeOutCoroutine != null)
        {
            StopCoroutine(fadeOutCoroutine);
            fadeOutCoroutine = null;
        }

        bgmAudioSource.Stop();
        bgmAudioSource.clip = victoryBgmClip;
        bgmAudioSource.loop = true;
        bgmAudioSource.volume = Mathf.Clamp(victoryBgmVolume, 0f, 5f);
        bgmAudioSource.Play();
    }

    private IEnumerator FadeOutAndStopBgm()
    {
        if (bgmAudioSource == null)
        {
            yield break;
        }

        float startVolume = bgmAudioSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            bgmAudioSource.volume = Mathf.Lerp(
                startVolume,
                0f,
                Mathf.Clamp01(elapsed / fadeOutDuration));

            yield return null;
        }

        bgmAudioSource.volume = 0f;
        bgmAudioSource.Stop();
        fadeOutCoroutine = null;
    }
}