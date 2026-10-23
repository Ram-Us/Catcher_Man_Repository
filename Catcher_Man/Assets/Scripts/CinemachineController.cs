using System.Security;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEditor.Search;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;

public class CinemachineController : MonoBehaviour
{
    [SerializeField] private  CinemachinePositionComposer cinemachine;
    [SerializeField] private float lerpSpeed = 1.0f;
    [SerializeField]private bool isInside = false;
    [SerializeField] private Vector3 normalPositon = new Vector3(0f,0f,0f),ZoomOutPositon = new Vector3(0f,0f,0f);
    void Awake()
    {
        cinemachine.TargetOffset = normalPositon;
    }

    void FixedUpdate()
    {
        if (isInside)
        {
            cinemachine.TargetOffset = Vector3.Lerp(cinemachine.TargetOffset,ZoomOutPositon,lerpSpeed);
            
        }
        else if(!isInside)
        {
            cinemachine.TargetOffset = Vector3.Lerp(cinemachine.TargetOffset,normalPositon,lerpSpeed);
            
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("接触したCollider：" + other.gameObject.name);
        if(other.gameObject.name == "TestPlayer")
        {
            isInside = true;
            Debug.Log(other.gameObject.name);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if(other.gameObject.name == "TestPlayer")
        {
            
            isInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {if(other.gameObject.name == "TestPlayer")
        {
            isInside = false;
        }
    }

}
