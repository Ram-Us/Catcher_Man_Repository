using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class IronBallColliderController : MonoBehaviour
{
    [SerializeField]
    private Vector3 colliderBeforeCenter;

    [SerializeField]
    private Vector3 colliderAfterCenter;

    [SerializeField]
    private AnimationCurve colliderMoveCurve =
        new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 0f)
        );

    private BoxCollider boxCollider;
    private Animator animator;
    private ItemGimmick itemGimmick;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        animator = GetComponentInChildren<Animator>(true);
        itemGimmick = GetComponent<ItemGimmick>();

        if (animator == null)
        {
            Debug.LogError(
                "IronBallのAnimatorが見つかりません。",
                gameObject
            );

            enabled = false;
            return;
        }

        if (itemGimmick == null)
        {
            Debug.LogError(
                "ItemGimmickが見つかりません。",
                gameObject
            );

            enabled = false;
            return;
        }

        //boxCollider.center = colliderBeforeCenter;
    }

    private void FixedUpdate()
    {
        if (itemGimmick.IsAttached)
        {
            boxCollider.center = colliderBeforeCenter;
            
        

        AnimatorStateInfo currentState =
            animator.GetCurrentAnimatorStateInfo(0);

        if (currentState.IsName("IronBall"))
        {
            UpdateColliderPosition(
                currentState.normalizedTime
            );

            return;
        }

        // IronBallへ遷移中の場合にも対応
        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextState =
                animator.GetNextAnimatorStateInfo(0);

            if (nextState.IsName("IronBall"))
            {
                UpdateColliderPosition(
                    nextState.normalizedTime
                );

                return;
            }
        }

        // 攻撃状態でなければ初期位置へ戻す
        boxCollider.center = colliderBeforeCenter;
    }
    }

    private void UpdateColliderPosition(
        float normalizedTime)
    {
        float animationProgress =
            Mathf.Clamp01(normalizedTime);

        float colliderProgress =
            colliderMoveCurve.Evaluate(
                animationProgress
            );

        colliderProgress =
            Mathf.Clamp01(colliderProgress);

        boxCollider.center = Vector3.Lerp(
            colliderBeforeCenter,
            colliderAfterCenter,
            colliderProgress
        );
    }

}