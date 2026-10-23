using UnityEngine;

public class GearRotation : MonoBehaviour
{
    public enum RotationDirection
    {
        Positive = 1,
        Negative = -1
    }

    [SerializeField, Min(0f)] private float rotationSpeed = 90f;
    [SerializeField] private RotationDirection direction =
        RotationDirection.Positive;

    [SerializeField] private GameObject ashiba1, ashiba2;

    private Quaternion ashibaRotation1, ashibaRotation2;

    private void Awake()
    {
        // 開始時のワールド回転を保存する
        if (ashiba1 != null)
        {
            ashibaRotation1 = ashiba1.transform.rotation;
        }

        if (ashiba2 != null)
        {
            ashibaRotation2 = ashiba2.transform.rotation;
        }
    }

    private void Update()
    {
        if (this.gameObject.transform.GetChild(2).GetComponent<GearSet>().IsTouchedGear)
        {
            transform.Rotate(Vector3.up * ((int)direction * rotationSpeed * Time.deltaTime),Space.Self);
        }
        
    }

    private void LateUpdate()
    {
        if (this.gameObject.transform.GetChild(2).GetComponent<GearSet>().IsTouchedGear)
        {
            if (ashiba1 != null)
            {
                ashiba1.transform.rotation = ashibaRotation1;
            }

            if (ashiba2 != null)
            {
                ashiba2.transform.rotation = ashibaRotation2;
            }
        }
        // 歯車が回った後、足場の向きを開始時の向きへ戻す
        
    }

    private void OnValidate()
    {
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
    }

    
}