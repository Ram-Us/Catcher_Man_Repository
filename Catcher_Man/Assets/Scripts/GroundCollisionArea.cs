using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class GroundCollisionArea : MonoBehaviour
{
    [SerializeField] private bool isGround;
    private readonly HashSet<Collider> groundContacts = new();
    private BoxCollider sensor;

    public bool IsGround
    {
        get
        {
            RefreshContacts();
            return isGround;
        }
    }

    private void Awake()
    {
        sensor = GetComponent<BoxCollider>();


        sensor.isTrigger = true;
    }

    private bool IsSupport(Collider other)
    {
        if (other == null || !other.enabled || !other.gameObject.activeInHierarchy || other.isTrigger)
            return false;
        if (sensor != null && sensor.attachedRigidbody != null &&
            other.attachedRigidbody == sensor.attachedRigidbody) return false;

        ItemGimmick item = other.GetComponentInParent<ItemGimmick>();
        if (item != null) return !item.IsAttached;
        return other.CompareTag("Ground") || other.CompareTag("MoveGround") ||
               other.GetComponentInParent<ConveyorBelt>() != null;
    }

    private void RefreshContacts()
    {
        if (!isActiveAndEnabled || sensor == null || !sensor.enabled)
        {
            ClearContacts();
            return;
        }
        groundContacts.RemoveWhere(c => !IsSupport(c));
        isGround = groundContacts.Count > 0;
    }

    public bool IsTouching(Collider other)
    {
        RefreshContacts();
        return other != null && groundContacts.Contains(other);
    }

    private void OnTriggerEnter(Collider other) => RegisterContact(other);
    private void OnTriggerStay(Collider other) => RegisterContact(other);

    private void RegisterContact(Collider other)
    {
        if (IsSupport(other)) groundContacts.Add(other);
        else groundContacts.Remove(other);
        RefreshContacts();
    }

    private void OnTriggerExit(Collider other)
    {
        groundContacts.Remove(other);
        RefreshContacts();
    }

    public void ClearContacts()
    {
        groundContacts.Clear();
        isGround = false;
    }

    private void OnDisable() => ClearContacts();
}
