using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class SavePoint : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;

    // 復帰位置が未指定なら、このオブジェクトの位置を使う。
    public Vector3 RespawnPosition =>
        respawnPoint != null
            ? respawnPoint.position
            : transform.position;

    private void Reset()
    {


        GetComponent<BoxCollider>().isTrigger = true;
    }
}