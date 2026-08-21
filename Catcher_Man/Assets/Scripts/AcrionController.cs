using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;


public class AcrionController : MonoBehaviour
{
    private InputAction catchAction;
    private GameObject gb;
    private bool isTouched = false;
    [SerializeField] private SlotController sc;
    private void Awake()
    {
        catchAction = InputSystem.actions.FindAction("Catch");
        
    }

    private void OnEnable() {
        catchAction.started += OnCatch;
        
    }
    private void OnDisable()
    {
        catchAction.started -= OnCatch;
    }

    private void FixedUpdate()
    {
        
    }
    private void OnCatch(InputAction.CallbackContext context)
    {
        if (isTouched)
        {   Debug.Log("タッチ");
            if (sc.TryAdd())
            {
                Debug.Log("獲得！");
                sc.RefreshUI(gb);
            }
            else
            {
                Debug.Log("失敗");
            }
        }
    }
    private void OnCollisionStay(Collision other) {
        if (other.gameObject.CompareTag("Item"))
        {
            isTouched = true;
            gb = other.gameObject;
            //Debug.Log("触れてるよ");

        }
        else
        {
            isTouched = false;

        }
    }
}
