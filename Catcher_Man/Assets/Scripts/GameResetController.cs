using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameResetController : MonoBehaviour
{
    [SerializeField] private string firstSceneName = "ProtoType";

    private InputAction resetAction;
    private bool isResetting;

    private void Awake()
    {
        resetAction = InputSystem.actions.FindAction("ResetGame");

        if (resetAction == null)
        {
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (resetAction == null)
        {
            return;
        }

        resetAction.performed += OnResetGame;
        resetAction.Enable();
    }

    private void OnDisable()
    {
        if (resetAction != null)
        {
            resetAction.performed -= OnResetGame;
        }
    }

    private void OnResetGame(InputAction.CallbackContext context)
    {
        if (isResetting)
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(firstSceneName))
        {
            Debug.LogError(
                $"シーン「{firstSceneName}」を読み込めません。" +
                "シーン名とBuild ProfilesのScene Listを確認してください。",
                this
            );
            return;
        }

        isResetting = true;

        // ポーズ中でも通常速度に戻す。
        Time.timeScale = 1f;
        AudioListener.pause = false;

        SceneManager.LoadScene(
            firstSceneName,
            LoadSceneMode.Single
        );
    }
}