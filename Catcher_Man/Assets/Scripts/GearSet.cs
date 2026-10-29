using UnityEngine;
using System.Collections.Generic;

public class GearSet : MonoBehaviour
{
    private bool isTouchedGear;
    public bool IsTouchedGear => isTouchedGear;

    private readonly HashSet<Collider> gearContacts = new();

    private void OnTriggerEnter(Collider other)
    {
        ItemGimmick item = other.GetComponentInParent<ItemGimmick>();

        if (item == null || item.Id != 3 || item.IsAttached)
        {
            return;
        }

        gearContacts.Add(other);
        isTouchedGear = gearContacts.Count > 0;

        // Colliderが子にあっても、アイテム本体を移動させる。
        Vector3 position = transform.parent != null
            ? transform.parent.position
            : transform.position;

        position.x = 0.3f;

        item.transform.position = position;
        item.transform.localScale = new Vector3(5f, 5f, 5f);

        Rigidbody rb = item.GetComponent<Rigidbody>();

        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 位置・回転を両方固定する。
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.useGravity = false;
        }

        item.SetTrap(true);
    }

    private void OnTriggerExit(Collider other)
    {
        gearContacts.Remove(other);
        isTouchedGear = gearContacts.Count > 0;
    }

    private void FixedUpdate()
    {
        // 取得・破壊・無効化でExitが呼ばれない場合にも対応する。
        gearContacts.RemoveWhere(other =>
        {
            if (other == null ||
                !other.enabled ||
                !other.gameObject.activeInHierarchy)
            {
                return true;
            }

            ItemGimmick item =
                other.GetComponentInParent<ItemGimmick>();

            return item == null || item.Id != 3 || item.IsAttached;
        });

        isTouchedGear = gearContacts.Count > 0;
    }

    private void OnDisable()
    {
        gearContacts.Clear();
        isTouchedGear = false;
    }
}