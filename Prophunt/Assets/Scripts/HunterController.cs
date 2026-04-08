using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class HunterController : NetworkBehaviour
{
    public static HunterController Instance;

    [Header("References")]
    [SerializeField]
    private Character.CharacterController characterController;
    [SerializeField] private FirearmWeapon weaponController;
    [SerializeField] private CinemachineCamera firstPersonCamera;

    private void OnEnable()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public override void OnNetworkSpawn()
    {
        Instance = this;
        characterController.SetVisible(!IsOwner);
        if (IsOwner)
            firstPersonCamera.Priority = 4;
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

            if (InputManager.AttackTriggered())
                weaponController.Fire(characterController.HeadPosition, characterController.ForwardDirection);
            if (InputManager.ReloadTriggered())
                weaponController.Reload();
        }
    }

    public int GetAmmo(out int maxAmmo)
    {
        return weaponController.GetAmmo(out maxAmmo);
    }
}
