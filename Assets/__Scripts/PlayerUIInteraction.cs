using UnityEngine;
using UnityEngine.InputSystem;

namespace __Scripts
{
    public class PlayerUIInteraction : MonoBehaviour
    {
        private PlayerInput playerInput;
        private bool isUIInteractionEnabled;
        private InputAction toggleUIAction;
        private InputAction uiClickAction;
        private InputAction toggleCursorLockAction;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            playerInput = GetComponent<PlayerInput>();

            // Find the ToggleUI action in the ToggleActions Action Map
            InputActionMap toggleActionMap = playerInput.actions.FindActionMap("ToggleActions");
            toggleUIAction = toggleActionMap.FindAction("ToggleUI");
            toggleUIAction.performed += _ => ToggleUIInteraction();
            toggleCursorLockAction = toggleActionMap.FindAction("ToggleCursorLock");
            toggleCursorLockAction.performed += _ => ToggleCursorLockState();

            // Find the Click action in the UI Interactions Action Map
            InputActionMap uiInteractionMap = playerInput.actions.FindActionMap("UI Interactions");
            uiClickAction = uiInteractionMap.FindAction("Click");
            uiClickAction.performed += _ => Debug.Log("UI Click Action Performed");
        }

        private void OnEnable()
        {
            toggleUIAction.Enable();
            uiClickAction.Enable();
            toggleCursorLockAction.Enable();
        }

        private void OnDisable()
        {
            toggleUIAction.Disable();
            uiClickAction.Disable();
            toggleCursorLockAction.Disable();
        }

        private void ToggleUIInteraction() 
        {
            isUIInteractionEnabled = !isUIInteractionEnabled;
            Cursor.lockState = isUIInteractionEnabled ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = true;

            if (isUIInteractionEnabled) {
                playerInput.SwitchCurrentActionMap("UI Interactions");
                Debug.Log("Switched to 'UI Interactions' Action Map.");
            } else {
                playerInput.SwitchCurrentActionMap("Player");
                Debug.Log("Switched back to 'Player' Action Map.");
            }

            Debug.Log("'Player' Action Map: " + (playerInput.currentActionMap.name == "Player" ? "Enabled" : "Disabled"));
            Debug.Log("'UI Interactions' Action Map: " + (playerInput.currentActionMap.name == "UI Interactions" ? "Enabled" : "Disabled"));

            // Add additional debug information as needed
            Debug.Log("UI Interaction Mode: " + (isUIInteractionEnabled ? "Enabled" : "Disabled"));
            Debug.Log("Cursor Lock State: " + Cursor.lockState);
            Debug.Log("Cursor Visible: " + Cursor.visible);
        }
        
        private void ToggleCursorLockState()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                // Unlock the cursor if it was locked
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                // Lock the cursor if it was unlocked
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
