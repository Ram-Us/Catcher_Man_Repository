using System.Collections.Generic;
using UnityEngine;

public class ButtonPressGimmick : MonoBehaviour
{
    [SerializeField] private Animator buttonAnimator;
    [SerializeField] private string pressTrigger = "PushDown";
    [SerializeField] private string releaseTrigger = "PushUp";
    [SerializeField] private string holdPressedParameter = "HoldPressed";
    private bool hasHoldPressedParameter;
    private bool holdPressedPose;

    [SerializeField] private Renderer buttonRenderer;
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material pressedMaterial;
    [SerializeField, Min(0)] private int materialIndex;
    [SerializeField] private Renderer[] wallRenderers = new Renderer[0];
    [SerializeField] private Material wallNormalMaterial;
    [SerializeField] private Material wallPressedMaterial;
    [SerializeField, Min(0)] private int wallMaterialIndex;
    private bool materialSynchronized;
    private bool lastMaterialPressed;

    // Colliderごとに「どのセンサーが接触しているか」を管理する。
    private readonly Dictionary<Collider, HashSet<int>> contacts = new();
    private readonly List<Collider> invalidContacts = new();
    private bool lastAnimatedPressed;
    private bool animationSynchronized;

    public bool IsPressed => isActiveAndEnabled && contacts.Count > 0;

    private void Awake()
    {
        if (buttonAnimator == null)
        {
            buttonAnimator = GetComponentInChildren<Animator>(true);
        }

        if (buttonAnimator == null || buttonAnimator.runtimeAnimatorController == null)
        {
            Debug.LogError("ボタンのAnimatorとControllerを設定してください。", this);
            return;
        }

        ValidateTrigger(pressTrigger);
        ValidateTrigger(releaseTrigger);
        foreach (AnimatorControllerParameter parameter in buttonAnimator.parameters)
        {
            if (parameter.name == holdPressedParameter &&
                parameter.type == AnimatorControllerParameterType.Bool)
            {
                hasHoldPressedParameter = true;
                break;
            }
        }
        if (!hasHoldPressedParameter)
        {
            Debug.LogWarning(
                $"ボタンAnimatorにBool '{holdPressedParameter}' を追加し、押下状態からの自動遷移条件にfalseを設定してください。",
                this);
        }
    }

    private void OnEnable()
    {
        animationSynchronized = false;
        materialSynchronized = false;
        UpdateMaterial(IsPressed);
    }

    private void FixedUpdate()
    {
        invalidContacts.Clear();
        foreach (var contact in contacts)
        {
            Collider other = contact.Key;
            if (other == null || !other.enabled ||
                !other.gameObject.activeInHierarchy || !CanPressButton(other))
            {
                invalidContacts.Add(other);
            }
        }
        foreach (Collider other in invalidContacts)
        {
            contacts.Remove(other);
        }
    }

    private void LateUpdate()
    {
        bool pressed = IsPressed;
        // Animatorの有無とは独立して、押下状態に合わせて表示を切り替える。
        UpdateMaterial(pressed);
        if (buttonAnimator == null || !buttonAnimator.isActiveAndEnabled ||
            buttonAnimator.runtimeAnimatorController == null)
        {
            animationSynchronized = false;
            return;
        }

        // 接触の最終結果から保持条件も更新し、Triggerと食い違わせない。
        if (hasHoldPressedParameter)
        {
            buttonAnimator.SetBool(holdPressedParameter, holdPressedPose && pressed);
        }

        // 初期状態では不要なPushUpを残さない。
        if (!animationSynchronized && !pressed)
        {
            if (HasTrigger(pressTrigger)) buttonAnimator.ResetTrigger(pressTrigger);
            if (HasTrigger(releaseTrigger)) buttonAnimator.ResetTrigger(releaseTrigger);
            lastAnimatedPressed = false;
            animationSynchronized = true;
            return;
        }
        if (animationSynchronized && lastAnimatedPressed == pressed)
        {
            return;
        }

        // 同じフレーム内のEnter/Exitをまとめ、最後の状態だけ反映する。
        if (HasTrigger(pressTrigger))
        {
            buttonAnimator.ResetTrigger(pressTrigger);
        }
        if (HasTrigger(releaseTrigger))
        {
            buttonAnimator.ResetTrigger(releaseTrigger);
        }

        string nextTrigger = pressed ? pressTrigger : releaseTrigger;
        if (!HasTrigger(nextTrigger))
        {
            return;
        }

        buttonAnimator.SetTrigger(nextTrigger);
        lastAnimatedPressed = pressed;
        animationSynchronized = true;
    }

    private void OnDisable()
    {
        SetHoldPressedPose(false);
        contacts.Clear();
        invalidContacts.Clear();
        animationSynchronized = false;
        UpdateMaterial(false);
        materialSynchronized = false;
    }

    private void UpdateMaterial(bool pressed)
    {
        if (materialSynchronized && lastMaterialPressed == pressed)
        {
            return;
        }

        Material targetMaterial = pressed ? pressedMaterial : normalMaterial;
        ApplyMaterial(buttonRenderer, targetMaterial, materialIndex);

        Material wallMaterial = pressed ? wallPressedMaterial : wallNormalMaterial;
        if (wallRenderers != null)
        {
            foreach (Renderer wallRenderer in wallRenderers)
            {
                ApplyMaterial(wallRenderer, wallMaterial, wallMaterialIndex);
            }
        }

        lastMaterialPressed = pressed;
        materialSynchronized = true;
    }

    private static void ApplyMaterial(Renderer targetRenderer, Material material, int index)
    {
        if (targetRenderer == null || material == null)
        {
            return;
        }

        Material[] materials = targetRenderer.sharedMaterials;
        if (index < 0 || index >= materials.Length)
        {
            return;
        }

        // 指定スロットだけ交換する。他のスロットやMaterial自体は変更しない。
        materials[index] = material;
        targetRenderer.sharedMaterials = materials;
    }

    // 共通Controllerを使っていても、パラメーター値は各Animatorで独立する。
    // ButtonDrivenDoorから、押下姿勢を保持するモードかどうか受け取る。
    public void SetHoldPressedPose(bool hold)
    {
        holdPressedPose = hold;
        if (buttonAnimator == null || !buttonAnimator.isActiveAndEnabled ||
            buttonAnimator.runtimeAnimatorController == null ||
            !hasHoldPressedParameter)
        {
            return;
        }
        buttonAnimator.SetBool(holdPressedParameter, hold && IsPressed);
    }

    // 同じオブジェクトのColliderで直接検知できるため、Relayや新しい子は必須ではない。
    private void OnCollisionEnter(Collision collision)
    {
        AddContact(collision.collider, GetInstanceID());
    }

    private void OnCollisionStay(Collision collision)
    {
        AddContact(collision.collider, GetInstanceID());
    }

    private void OnCollisionExit(Collision collision)
    {
        RemoveContact(collision.collider, GetInstanceID());
    }

    private void OnTriggerEnter(Collider other)
    {
        AddContact(other, GetInstanceID());
    }

    private void OnTriggerStay(Collider other)
    {
        AddContact(other, GetInstanceID());
    }

    private void OnTriggerExit(Collider other)
    {
        RemoveContact(other, GetInstanceID());
    }

    // sourceId省略時も、従来のAddContact(other)として使用できる。
    public void AddContact(Collider other, int sourceId = 0)
    {
        if (!isActiveAndEnabled || other == null)
        {
            return;
        }
        if (!CanPressButton(other))
        {
            RemoveContact(other, sourceId);
            return;
        }
        if (!contacts.TryGetValue(other, out HashSet<int> sources))
        {
            sources = new HashSet<int>();
            contacts.Add(other, sources);
        }
        sources.Add(sourceId);
    }

    public void RemoveContact(Collider other, int sourceId = 0)
    {
        if (other == null || !contacts.TryGetValue(other, out HashSet<int> sources))
        {
            return;
        }
        sources.Remove(sourceId);
        if (sources.Count == 0)
        {
            contacts.Remove(other);
        }
    }

    private static bool CanPressButton(Collider other)
    {
        // サーチ範囲や攻撃判定のTriggerは、ボタンを押す物体として数えない。
        // ボタン側のセンサーはTriggerでもよい。
        if (other.isTrigger)
        {
            return false;
        }

        // プレイヤーの子にある装備は、プレイヤー本体と区別する。
        ItemGimmick item = other.GetComponentInParent<ItemGimmick>();
        if (item != null)
        {
            return !item.IsAttached;
        }

        MoveController player = other.GetComponentInParent<MoveController>();
        if (player != null && player.CompareTag("Player"))
        {
            return true;
        }

        return other.CompareTag("Item") || other.transform.root.CompareTag("Item");
    }

    private bool HasTrigger(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName))
        {
            return false;
        }
        foreach (AnimatorControllerParameter parameter in buttonAnimator.parameters)
        {
            if (parameter.name == triggerName &&
                parameter.type == AnimatorControllerParameterType.Trigger)
            {
                return true;
            }
        }
        return false;
    }

    private void ValidateTrigger(string triggerName)
    {
        if (!HasTrigger(triggerName))
        {
            Debug.LogError($"ボタンAnimatorにTrigger '{triggerName}' がありません。", this);
        }
    }
}
