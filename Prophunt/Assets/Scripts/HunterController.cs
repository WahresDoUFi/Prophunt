using Unity.Netcode;
using UnityEngine;

public class HunterController : NetworkBehaviour
{
    [SerializeField]
    private Character.CharacterController characterController;

    private void OnEnable()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public override void OnNetworkSpawn()
    {
        characterController.SetVisible(!IsOwner);
    }

    private void Update()
    {
        if (IsOwner)
        {
            characterController.SendInput(InputManager.GetPlayerMovement(),
                    InputManager.GetMouseDelta(),
                    InputManager.IsSneaking());
            if (InputManager.JumpTriggered())
                characterController.Jump();
        }
    }
}
