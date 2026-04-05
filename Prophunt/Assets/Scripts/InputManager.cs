using UnityEngine;

public class InputManager : MonoBehaviour
{
    private static InputManager instance;

    private PlayerControls _playerControls;

    private void Awake()
    {
        instance = this;
        _playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        _playerControls.Enable();
    }

    private void OnDisable()
    {
        _playerControls.Disable();
    }

    public static Vector2 GetPlayerMovement()
    {
        return instance._playerControls.Player.Movement.ReadValue<Vector2>();
    }

    public static Vector2 GetMouseDelta()
    {
        return instance._playerControls.Player.Look.ReadValue<Vector2>();
    }

    public static bool IsSneaking()
    {
        return instance._playerControls.Player.Sneak.ReadValue<float>() > 0;
    }

    public static bool JumpTriggered()
    {
        return instance._playerControls.Player.Jump.triggered;
    }
}
