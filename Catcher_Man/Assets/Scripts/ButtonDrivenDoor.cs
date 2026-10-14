using System.Collections;
using UnityEngine;

public class ButtonDrivenDoor : MonoBehaviour
{
    public enum DoorMode
    {
        TimedOpen,
        OscillateWhilePressed,
        RiseOnce
    }

    [SerializeField] private ButtonPressGimmick button;
    [SerializeField] private DoorMode mode;
    [SerializeField, Min(0.01f)] private float movementSpeed = 2f;
    [SerializeField, Min(0f)] private float openDuration = 3f;

    // Inspectorで設定する基準座標。実行中は変更しない。
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private Vector3 openPosition;

    // 往復時に現在の始点と目的地として使用する座標。
    [SerializeField] private Vector3 activeStartPosition;
    [SerializeField] private Vector3 activeOpenPosition;

    private bool wasPressed;
    private bool riseStarted;
    private Coroutine timedOpenRoutine;

    private void Awake()
    {
        activeStartPosition = startPosition;
        activeOpenPosition = openPosition;
        transform.position = startPosition;

        if (button == null)
        {
            Debug.LogError("ButtonDrivenDoorにボタンを設定してください。", this);
        }
    }

    private void Update()
    {
        if (button == null)
        {
            return;
        }

        bool pressed = button.IsPressed;
        bool justPressed = pressed && !wasPressed;

        switch (mode)
        {
            case DoorMode.TimedOpen:
                if (justPressed && timedOpenRoutine == null)
                {
                    timedOpenRoutine = StartCoroutine(OpenForDuration());
                }
                break;

            case DoorMode.OscillateWhilePressed:
                if (pressed)
                {
                    UpdateOscillation();
                }
                break;

            case DoorMode.RiseOnce:
                if (justPressed && !riseStarted)
                {
                    riseStarted = true;
                    StartCoroutine(MoveTo(openPosition));
                }
                break;
        }

        wasPressed = pressed;
    }

    private IEnumerator OpenForDuration()
    {
        yield return MoveTo(openPosition);
        yield return new WaitForSeconds(openDuration);
        yield return MoveTo(startPosition);
        timedOpenRoutine = null;
    }

    private IEnumerator MoveTo(Vector3 targetPosition)
    {
        float speed = Mathf.Max(0.01f, movementSpeed);

        while ((transform.position - targetPosition).sqrMagnitude > 0.0001f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                speed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPosition;
    }

    private void UpdateOscillation()
    {
        float speed = Mathf.Max(0.01f, movementSpeed);

        transform.position = Vector3.MoveTowards(
            transform.position,
            activeOpenPosition,
            speed * Time.deltaTime
        );

        if ((transform.position - activeOpenPosition).sqrMagnitude <= 0.0001f)
        {
            transform.position = activeOpenPosition;

            // 目的地へ着いたら始点と目的地を入れ替えて折り返す。
            Vector3 reachedPosition = activeOpenPosition;
            activeOpenPosition = activeStartPosition;
            activeStartPosition = reachedPosition;
        }
    }

    private void OnValidate()
    {
        movementSpeed = Mathf.Max(0.01f, movementSpeed);
        openDuration = Mathf.Max(0f, openDuration);
    }
}
