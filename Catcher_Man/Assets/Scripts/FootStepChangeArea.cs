using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FootstepChangeArea : MonoBehaviour
{
    [Header("このエリアの足音")]
    [SerializeField] private AudioClip footstepLoop;

    [Tooltip("エリアが重なった場合、大きい値を優先")]
    [SerializeField] private int priority;

    private BoxCollider area;

    private static readonly List<FootstepChangeArea> activeAreas = new();

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearAreas()
    {
        activeAreas.Clear();
    }

    private void OnEnable()
    {
        area = GetComponent<BoxCollider>();

        if (GetComponentInParent<MoveController>() != null)
        {
            Debug.LogError(
                "FootstepChangeAreaはプレイヤーではなく、" +
                "独立した足音エリアに付けてください。",
                this);

            enabled = false;
            return;
        }

        if (!area.isTrigger)
        {
            Debug.LogError(
                "足音エリアのBoxColliderのIs TriggerをONにしてください。",
                this);
        }

        if (!activeAreas.Contains(this))
        {
            activeAreas.Add(this);
        }
    }

    private void OnDisable()
    {
        activeAreas.Remove(this);
    }

    public static AudioClip GetFootstepClip(BoxCollider body)
    {
        if (body == null ||
            !body.enabled ||
            body.isTrigger ||
            !body.gameObject.activeInHierarchy)
        {
            return null;
        }

        AudioClip selectedClip = null;
        int selectedPriority = int.MinValue;

        foreach (FootstepChangeArea candidate in activeAreas)
        {
            if (candidate == null ||
                !candidate.isActiveAndEnabled ||
                candidate.footstepLoop == null ||
                candidate.area == null ||
                !candidate.area.enabled ||
                !candidate.area.isTrigger)
            {
                continue;
            }

            bool overlaps = Physics.ComputePenetration(
                body,
                body.transform.position,
                body.transform.rotation,
                candidate.area,
                candidate.area.transform.position,
                candidate.area.transform.rotation,
                out _,
                out _
            );

            if (overlaps && candidate.priority >= selectedPriority)
            {
                selectedClip = candidate.footstepLoop;
                selectedPriority = candidate.priority;
            }
        }

        return selectedClip;
    }
}