using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ButtonSensorRelay : MonoBehaviour
{
    [SerializeField] private ButtonPressGimmick button;

    private readonly HashSet<Collider> contacts = new();

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponentInParent<ButtonPressGimmick>();
        }

        if (button == null)
        {
            Debug.LogError("親オブジェクトにButtonPressGimmickがありません。", this);
        }

        if (GetComponent<Collider>().isTrigger)
        {
            Debug.LogError("ボタンのColliderはIs Triggerを無効にしてください。", this);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        RegisterContact(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        RegisterContact(collision.collider);
    }

    private void OnCollisionExit(Collision collision)
    {
        RemoveContact(collision.collider);
    }

    private void OnDisable()
    {
        foreach (Collider contact in contacts)
        {
            if (button != null)
            {
                button.RemoveContact(contact);
            }
        }
        contacts.Clear();
    }

    private void RegisterContact(Collider other)
    {
        if (button == null || other == null)
        {
            return;
        }

        contacts.Add(other);
        button.AddContact(other);
    }

    private void RemoveContact(Collider other)
    {
        contacts.Remove(other);
        if (button != null)
        {
            button.RemoveContact(other);
        }
    }
}
