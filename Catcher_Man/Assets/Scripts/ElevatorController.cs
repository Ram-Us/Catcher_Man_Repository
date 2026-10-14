using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ElevatorController : MonoBehaviour
{
    [SerializeField] private Vector3 startPosition,endPosition;
    [SerializeField,Min(0.01f)] private float moveTime = 2f;

    private Rigidbody rb;
    private Coroutine elevatorCoroutine;
    private Vector3 activeTargetPosition;
    private bool isMoving = false,isPlayerOnElevator = false;

    private void Awake()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.position = startPosition;
        activeTargetPosition = endPosition;
    }

    public void StartElevator()
    {
        if (isMoving)
        {
            return;
        }

        elevatorCoroutine = StartCoroutine(
            MoveElevator(activeTargetPosition)
        );
    }

    private IEnumerator MoveElevator(Vector3 targetPosition)
    {
        isMoving = true;

        yield return MoveTo(targetPosition);

        // 次にプレイヤーが乗ったときは反対側へ移動する。
        if ((targetPosition - endPosition).sqrMagnitude <= 0.0001f)
        {
            activeTargetPosition = startPosition;
        }
        else
        {
            activeTargetPosition = endPosition;
        }

        isMoving = false;
        elevatorCoroutine = null;
    }

    private IEnumerator MoveTo(Vector3 targetPosition)
    {
        Vector3 beforePosition = rb.position;
        float elapsedTime = 0f;

        while (elapsedTime < moveTime)
        {
            elapsedTime += Time.fixedDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / moveTime
            );

            // 発進時と停止時の速度を滑らかにする。
            float smoothProgress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            Vector3 nextPosition = Vector3.Lerp(
                beforePosition,
                targetPosition,
                smoothProgress
            );

            rb.MovePosition(nextPosition);

            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(targetPosition);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player") ||
            isPlayerOnElevator)
        {
            return;
        }

        bool isStandingOnElevator = false;

        for (int i = 0; i < collision.contactCount; i++)
        {
            // エレベーター側では、上に乗ったPlayerとの接触法線は下向きになる。
            if (collision.GetContact(i).normal.y < -0.5f)
            {
                isStandingOnElevator = true;
                break;
            }
        }

        if (!isStandingOnElevator)
        {
            return;
        }

        isPlayerOnElevator = true;
        StartElevator();
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerOnElevator = false;
        }
    }

    public void StopElevator()
    {
        if (elevatorCoroutine != null)
        {
            StopCoroutine(elevatorCoroutine);
            elevatorCoroutine = null;
        }

        isMoving = false;
    }

    private void OnDisable()
    {
        StopElevator();
    }

    private void OnValidate()
    {
        moveTime = Mathf.Max(0.01f,moveTime);
    }
}
