using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleTransitionController : MonoBehaviour
{
    [Header("シーン")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Input Actions")]
    [SerializeField] private string gameplayMapName = "PlayerActions";

    [Header("タイトルのGlobal Volume")]
    [SerializeField] private Volume titleVolume;

    [Header("ゲーム開始前に表示する画像")]
    [SerializeField] private Sprite instructionSprite;

    [Header("演出")]
    [SerializeField, Min(0.1f)]
    private float transitionDuration = 0.8f;

    [SerializeField, Range(-1f, 1f)]
    private float distortionAmount = -0.65f;

    [SerializeField, Min(0.1f)]
    private float whiteFadeDuration = 0.3f;

    [SerializeField, Min(0.1f)]
    private float imageFadeDuration = 0.5f;

    [Header("タイトル決定音")]
    [SerializeField] private AudioClip titleConfirmClip;

    private float titleConfirmVolume = 1f;

    [Header("説明画像の表示音")]
    [SerializeField] private AudioClip instructionShowClip;

    private float instructionShowVolume = 1f;

    [Header("ゲーム開始音")]
    [SerializeField] private AudioClip gameStartClip;

    private float gameStartVolume = 1f;

    private enum State
    {
        Title,
        Loading,
        Instructions,
        Fading,
        Playing
    }

    private static TitleTransitionController instance;

    private State state;
    private InputAction confirmAction;
    private InputActionMap gameplayMap;

    private ChromaticAberration chromatic;
    private LensDistortion distortion;

    private Canvas screenCanvas;
    private Image instructionImage;
    private Image whiteImage;
    private AudioSource soundSource;

    private bool initialized;
    private bool readyForConfirm;
    private bool ownsPause;
    private float soundEndTime;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning(
                "TitleTransitionControllerが重複しています。" +
                "タイトルシーンの専用オブジェクトに1つだけ配置してください。",
                this);

            // 誤配置されていてもオブジェクト全体は削除しない。
            enabled = false;
            Destroy(this);
            return;
        }

        // プレイヤーを持ち越さないため、誤配置を止める。
        if (GetComponentInParent<MoveController>() != null ||
            GetComponentInChildren<MoveController>(true) != null)
        {
            Debug.LogError(
                "TitleTransitionControllerはプレイヤーと親子関係のない" +
                "専用の空オブジェクトに付けてください。",
                this);

            enabled = false;
            return;
        }

        instance = this;

        // 専用オブジェクトだけをシーン間で維持する。
        transform.SetParent(null, false);
        DontDestroyOnLoad(gameObject);

        if (instructionSprite == null)
        {
            Fail("Instruction Spriteに説明画像を設定してください。");
            return;
        }

        if (InputSystem.actions == null)
        {
            Fail("Project-wide Actionsが未設定です。");
            return;
        }

        gameplayMapName = gameplayMapName.Trim();

        InputActionMap transitionMap =
            InputSystem.actions.FindActionMap("Transition", false);

        confirmAction = transitionMap != null
            ? transitionMap.FindAction("Confirm", false)
            : null;

        gameplayMap =
            InputSystem.actions.FindActionMap(gameplayMapName, false);

        if (confirmAction == null || gameplayMap == null)
        {
            Fail(
                "Transition/Confirmと、ゲーム操作用Map「" +
                gameplayMapName + "」を確認してください。");
            return;
        }

        if (confirmAction.actionMap == gameplayMap)
        {
            Fail("Confirmはゲーム操作とは別のMapにしてください。");
            return;
        }

        PrepareVolume();
        CreateScreenUI();

        // 効果音は専用の子オブジェクトで再生。
        GameObject soundObject = new GameObject("TransitionSE");
        soundObject.transform.SetParent(transform, false);

        soundSource = soundObject.AddComponent<AudioSource>();
        soundSource.playOnAwake = false;
        soundSource.loop = false;
        soundSource.spatialBlend = 0f;
        soundSource.ignoreListenerPause = true;
        soundSource.volume = 1f;

        gameplayMap.Disable();
        confirmAction.Enable();

        Time.timeScale = 1f;
        state = State.Title;

        SceneManager.sceneLoaded += OnSceneLoaded;
        initialized = true;
    }

    private void PrepareVolume()
    {
        if (titleVolume == null)
        {
            Debug.LogWarning(
                "Title Volumeが未設定なので、ゆがみ演出を省略します。" +
                "白フェードと説明画像の表示は実行します。",
                this);
            return;
        }

        VolumeProfile profile = titleVolume.profile;

        if (!profile.TryGet(out chromatic))
        {
            chromatic = profile.Add<ChromaticAberration>(true);
        }

        if (!profile.TryGet(out distortion))
        {
            distortion = profile.Add<LensDistortion>(true);
        }

        chromatic.active = true;
        chromatic.intensity.Override(0f);

        distortion.active = true;
        distortion.intensity.Override(0f);
        distortion.center.Override(new Vector2(0.5f, 0.5f));
        distortion.xMultiplier.Override(1f);
        distortion.yMultiplier.Override(1f);
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        if (state != State.Title && state != State.Instructions)
        {
            return;
        }

        // 遷移先の初期化で無効になっても決定入力を受け取れるようにする。
        if (!confirmAction.enabled)
        {
            confirmAction.Enable();
        }

        if (!readyForConfirm)
        {
            if (!confirmAction.IsPressed())
            {
                readyForConfirm = true;
            }

            return;
        }

        if (!confirmAction.WasPressedThisFrame())
        {
            return;
        }

        if (state == State.Title)
        {
            if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                Debug.LogError(
                    $"シーン「{gameSceneName}」をBuild Profilesの" +
                    "Scene Listに登録してください。",
                    this);
                return;
            }

            readyForConfirm = false;

            PlaySound(
                titleConfirmClip,
                titleConfirmVolume,
                "タイトル決定");

            StartCoroutine(ChangeScene());
        }
        else
        {
            readyForConfirm = false;

            PlaySound(
                gameStartClip,
                gameStartVolume,
                "ゲーム開始");

            StartCoroutine(StartGameplay());
        }
    }

    private void LateUpdate()
    {
        if (!initialized || !ownsPause)
        {
            return;
        }

        // 説明中に別のStart処理などが時間・入力を戻しても再度停止する。
        Time.timeScale = 0f;
        gameplayMap.Disable();
    }

    private IEnumerator ChangeScene()
    {
        state = State.Loading;
        gameplayMap.Disable();

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(elapsed / transitionDuration);

            float smooth = Mathf.SmoothStep(0f, 1f, progress);

            if (chromatic != null)
            {
                chromatic.intensity.value = smooth;
            }

            if (distortion != null)
            {
                distortion.intensity.value =
                    Mathf.Lerp(0f, distortionAmount, smooth);
            }

            SetAlpha(
                whiteImage,
                Mathf.InverseLerp(0.35f, 1f, progress));

            yield return null;
        }

        SetAlpha(whiteImage, 1f);
        yield return null;

        ownsPause = true;
        Time.timeScale = 0f;

        AsyncOperation loading = SceneManager.LoadSceneAsync(
            gameSceneName,
            LoadSceneMode.Single);

        while (!loading.isDone)
        {
            yield return null;
        }

        // 遷移先の初期化を待つ。
        yield return null;

        gameplayMap.Disable();
        confirmAction.Enable();
        Time.timeScale = 0f;

        // Canvasが失われた場合も再生成する。
        if (screenCanvas == null ||
            instructionImage == null ||
            whiteImage == null)
        {
            if (screenCanvas != null)
            {
                Destroy(screenCanvas.gameObject);
            }

            CreateScreenUI();
        }

        screenCanvas.gameObject.SetActive(true);
        screenCanvas.enabled = true;

        instructionImage.gameObject.SetActive(true);
        instructionImage.enabled = true;
        instructionImage.sprite = instructionSprite;
        instructionImage.color = Color.white;

        whiteImage.gameObject.SetActive(true);
        whiteImage.enabled = true;
        whiteImage.transform.SetAsLastSibling();
        SetAlpha(whiteImage, 1f);

        Canvas.ForceUpdateCanvases();

        Debug.Log(
            $"説明画像を表示：{instructionSprite.name}／" +
            $"シーン：{SceneManager.GetActiveScene().name}",
            this);

        PlaySound(
            instructionShowClip,
            instructionShowVolume,
            "説明画像表示");

        elapsed = 0f;

        while (elapsed < whiteFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            SetAlpha(
                whiteImage,
                1f - Mathf.Clamp01(elapsed / whiteFadeDuration));

            yield return null;
        }

        // 白いImageが説明画像を覆わないよう、最後に無効化する。
        SetAlpha(whiteImage, 0f);
        whiteImage.gameObject.SetActive(false);

        readyForConfirm = false;
        state = State.Instructions;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (state != State.Loading)
        {
            return;
        }

        ownsPause = true;
        Time.timeScale = 0f;
        gameplayMap.Disable();
    }

    private IEnumerator StartGameplay()
    {
        state = State.Fading;

        float elapsed = 0f;

        while (elapsed < imageFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            SetAlpha(
                instructionImage,
                1f - Mathf.Clamp01(elapsed / imageFadeDuration));

            yield return null;
        }

        SetAlpha(instructionImage, 0f);

        // 決定入力をジャンプへ持ち越さない。
        while (confirmAction.IsPressed())
        {
            yield return null;
        }

        state = State.Playing;
        ownsPause = false;

        confirmAction.Disable();
        Time.timeScale = 1f;
        gameplayMap.Enable();

        if (screenCanvas != null)
        {
            Destroy(screenCanvas.gameObject);
        }

        // ゲーム操作は再開した状態で、再生中の効果音だけ待つ。
        while (Time.unscaledTime < soundEndTime)
        {
            yield return null;
        }

        // 削除するのは専用の遷移オブジェクトだけ。
        Destroy(gameObject);
    }

    private void CreateScreenUI()
    {
        // Controllerの親やCanvasGroupの影響を受けない独立したCanvas。
        GameObject canvasObject = new GameObject(
            "TransitionCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));

        DontDestroyOnLoad(canvasObject);

        screenCanvas = canvasObject.GetComponent<Canvas>();
        screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        screenCanvas.sortingOrder = 32767;
        screenCanvas.targetDisplay = 0;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        instructionImage =
            CreateImage(canvasObject.transform, "Instructions");

        instructionImage.sprite = instructionSprite;
        instructionImage.type = Image.Type.Simple;
        instructionImage.preserveAspect = false;
        SetAlpha(instructionImage, 0f);

        whiteImage =
            CreateImage(canvasObject.transform, "WhiteFade");

        SetAlpha(whiteImage, 0f);
    }

    private Image CreateImage(Transform parent, string objectName)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        return image;
    }

    private void PlaySound(AudioClip clip, float volume, string label)
    {
        if (clip == null)
        {
            Debug.LogWarning($"{label}のAudioClipが未設定です。", this);
            return;
        }

        if (volume <= 0f)
        {
            return;
        }

        soundSource.PlayOneShot(clip, volume);

        soundEndTime = Mathf.Max(
            soundEndTime,
            Time.unscaledTime + clip.length + 0.2f);
    }

    private void SetAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = Mathf.Clamp01(alpha);
        image.color = color;
    }

    private void Fail(string message)
    {
        Debug.LogError(message, this);
        initialized = false;
        enabled = false;
        Destroy(this);
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (screenCanvas != null)
        {
            Destroy(screenCanvas.gameObject);
        }

        if (initialized)
        {
            confirmAction.Disable();

            // 予期せず破壊された場合も時間停止を残さない。
            if (ownsPause)
            {
                Time.timeScale = 1f;
                gameplayMap.Enable();
            }
        }

        instance = null;
    }
}