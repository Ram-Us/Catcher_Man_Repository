using System.Collections;
using UnityEngine;

public class WeaponColliderController : MonoBehaviour
{
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private float swingAngle = 60f;
    [SerializeField, Min(0f)] private float swingDrop = 0.1f;

    [Min(0.01f)]
    [SerializeField] private float duration = 0.4f;

    private bool isSwinging;
    public bool IsSwinging => isSwinging;
    private bool hasStartRotation;
    private Coroutine swingCoroutine;
    private Quaternion startRotation;
    private Vector3 startPosition;

    public void SwingInitialize()
    {
        SwingInitialize(weaponPivot);
    }

    public void SwingInitialize(Transform targetWeaponPivot)
    {
        // 古いstartRotationへ戻さず、Coroutineだけ停止する。
        CancelSwingCoroutine();

        weaponPivot = targetWeaponPivot;

        if (weaponPivot == null)
        {
            Debug.LogError(
                "WeaponPivotが設定されていません。",
                this
            );
            return;
        }

        // ActionControllerが設定した現在角度を新しい基準にする。
        startRotation = weaponPivot.localRotation;
        startPosition = weaponPivot.localPosition;
        hasStartRotation = true;
    }

    public void SwingWeapon()
    {
        if (isSwinging)
        {
            return;
        }

        if (weaponPivot == null)
        {
            Debug.LogWarning(
                "WeaponPivotが設定されていないため、武器を振れません。",
                this
            );
            return;
        }

        if (!weaponPivot.gameObject.activeInHierarchy ||
            weaponPivot.childCount == 0)
        {
            return;
        }

        swingCoroutine = StartCoroutine(SwingRoutine());
    }

    private IEnumerator SwingRoutine()
    {
        isSwinging = true;

        startRotation = weaponPivot.localRotation;
        startPosition = weaponPivot.localPosition;
        hasStartRotation = true;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float progress =
                Mathf.Clamp01(elapsedTime / duration);

            // 基準角度→60度回転→基準角度
            float angle =
                Mathf.Sin(progress * Mathf.PI) *
                swingAngle;

            weaponPivot.localRotation =
                startRotation *
                Quaternion.Euler(0f, 0f, angle);
            Vector3 position = startPosition;
            position.y -= swingDrop * Mathf.Sin(progress * Mathf.PI);
            weaponPivot.localPosition = position;

            yield return null;
        }

        weaponPivot.localRotation = startRotation;
        weaponPivot.localPosition = startPosition;

        isSwinging = false;
        swingCoroutine = null;
    }

    public void StopSwing()
    {
        CancelSwingCoroutine();

        // 装備変更前や無効化時には基準角度へ戻す。
        if (weaponPivot != null && hasStartRotation)
        {
            weaponPivot.localRotation = startRotation;
            weaponPivot.localPosition = startPosition;
        }
    }

    private void CancelSwingCoroutine()
    {
        if (swingCoroutine != null)
        {
            StopCoroutine(swingCoroutine);
            swingCoroutine = null;
        }

        isSwinging = false;
    }

    private void OnDisable()
    {
        StopSwing();
    }
}
