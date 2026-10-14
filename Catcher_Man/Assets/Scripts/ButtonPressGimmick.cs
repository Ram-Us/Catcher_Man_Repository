using System.Collections.Generic;
using UnityEngine;

public class ButtonPressGimmick : MonoBehaviour
{
    [SerializeField] private Animator buttonAnimator;
    [SerializeField] private string pressTrigger = "PushDown";
    [SerializeField] private string releaseTrigger = "PushUp";

    private readonly HashSet<Collider> contacts = new();

    public bool IsPressed => contacts.Count > 0;

    private void Awake()
    {
        if (buttonAnimator == null)
        {
            buttonAnimator = GetComponentInChildren<Animator>();
        }

        if (buttonAnimator == null)
        {
            Debug.LogError("ボタンのAnimatorが設定されていません。", this);
            return;
        }

        ValidateTrigger(pressTrigger);
        ValidateTrigger(releaseTrigger);
    }

    private void FixedUpdate()
    {
        bool wasPressed = IsPressed;
        contacts.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
        UpdateAnimation(wasPressed);
    }

    private void OnDisable()
    {
        bool wasPressed = IsPressed;
        contacts.Clear();
        UpdateAnimation(wasPressed);
    }

    public void AddContact(Collider other)
    {
        if (other == null || !CanPressButton(other))
        {
            return;
        }

        bool wasPressed = IsPressed;
        contacts.Add(other);
        UpdateAnimation(wasPressed);
    }

    public void RemoveContact(Collider other)
    {
        if (other == null)
        {
            return;
        }

        bool wasPressed = IsPressed;
        contacts.Remove(other);
        UpdateAnimation(wasPressed);
    }

    private static bool CanPressButton(Collider other)
    {
        MoveController player = other.GetComponentInParent<MoveController>();
        if (player != null && player.gameObject.tag == "Player")
        {
            return true;
        }

        ItemGimmick item = other.GetComponentInParent<ItemGimmick>();
        if (item != null)
        {
            return !item.IsAttached;
        }

        return other.gameObject.tag == "Item" || other.transform.root.gameObject.tag == "Item";
    }

    private void UpdateAnimation(bool wasPressed)
    {
        bool isPressed = IsPressed;
        if (wasPressed == isPressed || buttonAnimator == null)
        {
            return;
        }

        buttonAnimator.ResetTrigger(isPressed ? releaseTrigger : pressTrigger);
        buttonAnimator.SetTrigger(isPressed ? pressTrigger : releaseTrigger);
    }

    private void ValidateTrigger(string triggerName)
    {
        foreach (AnimatorControllerParameter parameter in buttonAnimator.parameters)
        {
            if (parameter.name == triggerName && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                return;
            }
        }

        Debug.LogError($"ボタンAnimatorにTrigger '{triggerName}' がありません。", this);
    }
}
