using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ConveyorBelt : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player", ItemTag = "Item";
    [SerializeField, Min(0f)] private float beltSpeed = 2f;
    [SerializeField, Range(0f, 1f)] private float inputSpeedMultiplier = 0.5f;

    private readonly Dictionary<Rigidbody, RigidbodyConstraints> itemConstraints = new();
    private readonly HashSet<Rigidbody> currentItems = new();
    private readonly List<Rigidbody> itemsToRelease = new();
    private readonly HashSet<Rigidbody> movedItems = new();
    private Collider beltCollider;

    public float BeltSpeed => beltSpeed;
    public float InputSpeedMultiplier => inputSpeedMultiplier;

    private void Awake()
    {
        beltCollider = GetComponent<Collider>();
    }

    private void FixedUpdate()
    {
        Bounds bounds = beltCollider.bounds;
        Vector3 queryCenter = new(bounds.center.x, bounds.max.y + 0.2f, bounds.center.z);
        Vector3 queryExtents = new(bounds.extents.x, 0.3f, bounds.extents.z);
        Collider[] overlaps = Physics.OverlapBox(
            queryCenter,
            queryExtents,
            Quaternion.identity,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);

        currentItems.Clear();
        movedItems.Clear();

        foreach (Collider itemCollider in overlaps)
        {
            if (itemCollider == beltCollider || !IsItem(itemCollider) ||
                itemCollider.bounds.min.y < bounds.max.y - 0.1f ||
                itemCollider.bounds.min.y > bounds.max.y + 0.2f)
            {
                continue;
            }

            Rigidbody itemBody = itemCollider.attachedRigidbody;
            if (itemBody == null || !currentItems.Add(itemBody) || !movedItems.Add(itemBody))
            {
                continue;
            }

            if (!itemConstraints.TryGetValue(itemBody, out RigidbodyConstraints originalConstraints))
            {
                originalConstraints = itemBody.constraints;
                itemConstraints.Add(itemBody, originalConstraints);
            }

            itemBody.constraints = originalConstraints & ~RigidbodyConstraints.FreezePositionZ;
            if (itemBody.isKinematic)
            {
                itemBody.MovePosition(itemBody.position + Vector3.back * beltSpeed * Time.fixedDeltaTime);
            }
            else
            {
                Vector3 velocity = itemBody.linearVelocity;
                velocity.z = -beltSpeed;
                itemBody.linearVelocity = velocity;
            }
        }

        itemsToRelease.Clear();
        foreach (KeyValuePair<Rigidbody, RigidbodyConstraints> item in itemConstraints)
        {
            if (item.Key == null || !currentItems.Contains(item.Key))
            {
                itemsToRelease.Add(item.Key);
            }
        }
        foreach (Rigidbody itemBody in itemsToRelease)
        {
            if (itemBody != null && itemConstraints.TryGetValue(itemBody, out RigidbodyConstraints constraints))
            {
                itemBody.constraints = constraints;
            }
            itemConstraints.Remove(itemBody);
        }
    }

    private void OnDisable()
    {
        foreach (KeyValuePair<Rigidbody, RigidbodyConstraints> item in itemConstraints)
        {
            if (item.Key != null)
            {
                item.Key.constraints = item.Value;
            }
        }
        itemConstraints.Clear();
        currentItems.Clear();
    }

    private bool IsItem(Collider candidate)
    {
        ItemGimmick item = candidate.GetComponentInParent<ItemGimmick>();
        if (item != null)
        {
            return !item.IsAttached;
        }

        return candidate.CompareTag(ItemTag) || candidate.transform.root.CompareTag(ItemTag);
    }

    public bool IsPlayer(GameObject candidate)
    {
        return candidate != null && (candidate.tag == playerTag || candidate.tag == ItemTag);
    }

    private void OnValidate()
    {
        beltSpeed = Mathf.Max(0f, beltSpeed);
        inputSpeedMultiplier = Mathf.Clamp01(inputSpeedMultiplier);
    }
}
