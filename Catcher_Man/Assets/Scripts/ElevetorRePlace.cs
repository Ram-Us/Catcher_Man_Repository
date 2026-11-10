using NUnit.Framework;
using UnityEngine;

public class ElevetorRePlace : MonoBehaviour
{
    private bool isPlaced = false;
    public bool IsPlaced => isPlaced;
    void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.CompareTag("Player"))
        {
            isPlaced = true;
        }
    }
    void OnTriggerExit(Collider collider)
    {
        if (collider.gameObject.CompareTag("Player"))
        {
            isPlaced = false;
        }
    }
}
