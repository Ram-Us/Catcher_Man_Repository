using System.Security;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEditor.Search;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;

public class CinemachineController : MonoBehaviour
{
    [SerializeField] private  CinemachinePositionComposer cinemachine;
    [SerializeField] private GameObject wall;
    [SerializeField] private float lerpSpeed = 1.0f;
    [SerializeField]private bool isInside = false;
    [SerializeField] private Vector3 normalPositon = new Vector3(0f,0f,0f),ZoomOutPositon = new Vector3(0f,0f,0f);
    void Awake()
    {
        cinemachine.TargetOffset = normalPositon;
    }

    void FixedUpdate()
    {
        cinemachineMove(isInside);
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject == wall)
        {
            isInside = true;
            Debug.Log("パスします");
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if(other.gameObject == wall)
        {
            
            isInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        isInside = false;
    }

    private void cinemachineMove(bool inside)
    {
        if (inside)
        {
            cinemachine.TargetOffset = Vector3.Lerp(cinemachine.TargetOffset,ZoomOutPositon,lerpSpeed);
            
        }
        else if(!inside)
        {
            cinemachine.TargetOffset = Vector3.Lerp(cinemachine.TargetOffset,normalPositon,lerpSpeed);
            
        }
        
        
    }
}
