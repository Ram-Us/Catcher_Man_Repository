using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverController : MonoBehaviour
{
    [Header("画面")]
    [SerializeField] private Image darkImage;
    [SerializeField] private GameObject menuRoot;


    [Header("遷移先")]
    [SerializeField] private string titleSceneName = "Title";

    [Header("暗転")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 1f;
    [SerializeField, Range(0f, 1f)] private float darkness = 0.85f;

    [Header("選択肢のQuad")]
    [SerializeField] private Transform retryQuad;
    [SerializeField] private Transform quitQuad;

    [SerializeField, Min(1f)]
    private float selectedScale = 1.15f;

    private Vector3 retryOriginalScale;
    private Vector3 quitOriginalScale;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference navigateAction;
    [SerializeField] private InputActionReference confirmAction;

    [Header("ゲームオーバー時に隠すUI")]
    [SerializeField] private GameObject[] hideOnGameOver;

    [Header("ゲームオーバー時に停止するBGM")]
    [SerializeField] private BossBgmController bossBgmController;

    [Header("効果音")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip gameOverClip;
    [SerializeField] private AudioClip selectClip;
    [SerializeField] private AudioClip confirmClip;

    [SerializeField, Min(0f)]
    private float sceneChangeDelay = 0.3f;

    private InputActionMap gameplayMap;

    private bool isShowing;
    private bool canSelect;
    private bool isLoading;
    private bool stickReturned;
    private bool confirmReleased;

    // 0：リトライ、1：やめる
    private int selectedIndex;

   private void Awake()
    {

        if (uiAudioSource != null)
        {
            uiAudioSource.playOnAwake = false;
            uiAudioSource.loop = false;
            uiAudioSource.spatialBlend = 0f;

            // AudioListener.pauseがONでもUI音は鳴らす。
            uiAudioSource.ignoreListenerPause = true;
        }
        // 入力などに設定漏れがあっても、画面は最初に隠す。
        if (darkImage != null)
        {
            SetDarkness(0f);
            darkImage.raycastTarget = false;
        }

        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }

        // 設定漏れの場所を個別に表示する。
        bool hasError = false;

        if (darkImage == null)
        {
            Debug.LogError("Dark Imageが未設定です。", this);
            hasError = true;
        }

        if (menuRoot == null)
        {
            Debug.LogError("Menu Rootが未設定です。", this);
            hasError = true;
        }

        if (retryQuad == null || quitQuad == null)
        {
            Debug.LogError("Retry QuadまたはQuit Quadが未設定です。", this);
            hasError = true;
        }

        if (navigateAction == null || navigateAction.action == null ||
            confirmAction == null || confirmAction.action == null)
        {
            Debug.LogError("Navigate ActionまたはConfirm Actionが未設定です。", this);
            hasError = true;
        }

        if (hasError)
        {
            enabled = false;
            return;
        }

        retryOriginalScale = retryQuad.localScale;
        quitOriginalScale = quitQuad.localScale;

        gameplayMap = InputSystem.actions != null
            ? InputSystem.actions.FindActionMap("PlayerActions", false)
            : null;

        Debug.Log("ゲームオーバー画面の初期化完了", this);
    }

    public void ShowGameOver()
    {
        Debug.Log(
            $"ゲームオーバー呼び出し：" +
            $"表示開始済み={isShowing}, " +
            $"スクリプト有効={enabled}, " +
            $"オブジェクト有効={gameObject.activeInHierarchy}",
            this
        );

        if (isShowing)
        {
            return;
        }

        if (!isActiveAndEnabled)
        {
            Debug.LogError(
                "GameOverManagerまたはGameOverControllerが無効です。" +
                "設定漏れのエラーとStop On Deathを確認してください。",
                this
            );
            return;
        }

        isShowing = true;

        if (bossBgmController == null)
            bossBgmController = FindFirstObjectByType<BossBgmController>();

        if (bossBgmController != null)
            bossBgmController.StopBgmForGameOver();
        else
            Debug.LogWarning("停止するBossBgmControllerが見つかりません。Inspectorで設定してください。", this);

        foreach (GameObject ui in hideOnGameOver)
        {
            if (ui != null)
            {
                ui.SetActive(false);
            }
        }

        PlayUiSound(gameOverClip);

        gameplayMap?.Disable();
        Time.timeScale = 0f;

        darkImage.gameObject.SetActive(true);
        darkImage.raycastTarget = true;

        navigateAction.action.Enable();
        confirmAction.action.Enable();

        StartCoroutine(FadeToGameOver());
    }

    private IEnumerator FadeToGameOver()
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            // ゲーム内時間が止まっていても暗転させる。
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            SetDarkness(Mathf.Lerp(0f, darkness, progress));

            yield return null;
        }

        SetDarkness(darkness);

        selectedIndex = 0;
        UpdateSelection();
        menuRoot.SetActive(true);

        // 死亡直前の入力で勝手に選択・決定しないよう、
        // 一度スティックと決定ボタンを離してから受け付ける。
        stickReturned = false;
        confirmReleased = false;
        canSelect = true;
    }

    private void Update()
    {
        if (!canSelect || isLoading)
        {
            return;
        }

        Vector2 input = navigateAction.action.ReadValue<Vector2>();

        float strength = Mathf.Max(
            Mathf.Abs(input.x),
            Mathf.Abs(input.y)
        );

        if (strength < 0.3f)
        {
            stickReturned = true;
        }
        else if (stickReturned && strength > 0.6f)
        {
            int nextIndex;

            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                // 左：リトライ、右：やめる
                nextIndex = input.x < 0f ? 0 : 1;
            }
            else
            {
                // 上：リトライ、下：やめる
                nextIndex = input.y > 0f ? 0 : 1;
            }

            stickReturned = false;

            if (selectedIndex != nextIndex)
            {
                selectedIndex = nextIndex;
                UpdateSelection();
                PlayUiSound(selectClip);
            }
        }

        if (!confirmAction.action.IsPressed())
        {
            confirmReleased = true;
        }

        if (confirmReleased &&
            confirmAction.action.WasPressedThisFrame())
        {
            LoadSelectedScene();
        }
    }

    private void UpdateSelection()
    {
        retryQuad.localScale = retryOriginalScale *
            (selectedIndex == 0 ? selectedScale : 1f);

        quitQuad.localScale = quitOriginalScale *
            (selectedIndex == 1 ? selectedScale : 1f);
    }

    private void SetDarkness(float alpha)
    {
        darkImage.color = new Color(0f, 0f, 0f, alpha);
    }

    private void LoadSelectedScene()
    {
        if (isLoading)
        {
            return;
        }

        string sceneName = selectedIndex == 0
            ? SceneManager.GetActiveScene().name
            : titleSceneName;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"シーン「{sceneName}」をBuild ProfilesのScene Listに登録してください。",
                this
            );
            return;
        }

        isLoading = true;
        canSelect = false;

        navigateAction.action.Disable();
        confirmAction.action.Disable();

        StartCoroutine(PlayConfirmAndLoad(sceneName));
    }

    private IEnumerator PlayConfirmAndLoad(string sceneName)
    {
        PlayUiSound(confirmClip);

        float waitTime = sceneChangeDelay;

        if (uiAudioSource != null && confirmClip != null)
        {
            waitTime = Mathf.Max(waitTime, confirmClip.length);
        }

        // Time.timeScaleが0でも進む待機。
        yield return new WaitForSecondsRealtime(waitTime);

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (selectedIndex == 0)
        {
            gameplayMap?.Enable();
        }

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (!isShowing)
        {
            return;
        }

        navigateAction?.action?.Disable();
        confirmAction?.action?.Disable();

        Time.timeScale = 1f;
    }
    private void PlayUiSound(AudioClip clip)
    {
        if (uiAudioSource != null && clip != null)
        {
            uiAudioSource.PlayOneShot(clip);
        }
    }
}