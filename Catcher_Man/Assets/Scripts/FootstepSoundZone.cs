using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FootstepSoundZone : MonoBehaviour
{
    [SerializeField] private AudioClip[] footsteps;
    private readonly HashSet<PlayerMovementAudio> playersInside = new();

    private void Reset()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnEnable()
    {
        playersInside.Clear();
    }

    private void OnTriggerEnter(Collider other) => TrySwitch(other);

    // 内側で有効化・開始した場合も対応する。
    private void OnTriggerStay(Collider other) => TrySwitch(other);

    private void TrySwitch(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body == null) return;
        PlayerMovementAudio player = body.GetComponent<PlayerMovementAudio>();
        // 装備品の別Rigidbodyでは切り替えない。
        if (player != null && player.isActiveAndEnabled && playersInside.Add(player))
            player.SetFootsteps(footsteps);
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body == null) return;
        PlayerMovementAudio player = body.GetComponent<PlayerMovementAudio>();
        if (player == null) return;
        // 本体Colliderが複数ある場合、一つ抜けただけで再切り替えない。
        foreach (Collider contact in body.GetComponentsInChildren<Collider>())
        {
            if (contact.attachedRigidbody == body && contact.enabled &&
                contact.gameObject.activeInHierarchy &&
                GetComponent<BoxCollider>().bounds.Intersects(contact.bounds)) return;
        }
        playersInside.Remove(player);
    }
}
