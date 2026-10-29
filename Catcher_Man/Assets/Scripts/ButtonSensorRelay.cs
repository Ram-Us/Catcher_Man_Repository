using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ButtonSensorRelay : MonoBehaviour
{
    [SerializeField] private ButtonPressGimmick button;

    private readonly HashSet<Collider> contacts = new();
    private int sourceId;

    private void Awake()
    {
        sourceId = GetInstanceID();
        if (button == null)
        {
            button = GetComponentInParent<ButtonPressGimmick>();
        }
        if (button == null)
        {
            Debug.LogError("親オブジェクトにButtonPressGimmickがありません。", this);
        }
    }

    

    // Triggerでも通常Colliderでも検出する。Inspectorの設定は変更しない。
    private void OnTriggerEnter(Collider other)
    {
        RegisterContact(other);
    }

    private void OnTriggerStay(Collider other)
    {
        RegisterContact(other);
    }

    private void OnTriggerExit(Collider other)
    {
        RemoveContact(other);
    }

    private void OnDisable()
    {
        foreach (Collider contact in contacts)
        {
            if (button != null)
            {
                button.RemoveContact(contact, sourceId);
            }
        }
        contacts.Clear();
    }

    private void RegisterContact(Collider other)
    {
        if (!isActiveAndEnabled || button == null || other == null)
        {
            return;
        }
        contacts.Add(other);
        // Stayで再通知しても、同じセンサーIDは重複登録されない。
        button.AddContact(other, sourceId);
    }

    private void RemoveContact(Collider other)
    {
        contacts.Remove(other);
        if (button != null)
        {
            button.RemoveContact(other, sourceId);
        }
    }
}
