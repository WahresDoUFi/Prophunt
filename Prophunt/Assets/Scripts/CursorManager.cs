using UnityEngine;

public class CursorManager : MonoBehaviour
{
    private static int NeedCursorObjects;

    public static void RequestCursor()
    {
        NeedCursorObjects++;
    }

    public static void ReturnCursor()
    {
        NeedCursorObjects--;
    }

    private void Update()
    {
        Cursor.visible = NeedCursorObjects > 0;
        Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
