using UnityEngine;

public class TPSCamera : MonoBehaviour
{
    public Transform player;      // Player object
    public Transform pivot;       // Empty pivot behind the player
    public float sensitivity = 1.5f;
    public float distance = 3f;

    private float verticalRotation = 0f;
    public Camera cam;

    void Start()
    {
        cam = Camera.main;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        // --- Read input ---
        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");

        // --- Horizontal rotation controls player (yaw) ---
        player.Rotate(Vector3.up * mouseX * sensitivity);

        // --- Vertical rotation controls camera pivot (pitch) ---
        verticalRotation -= mouseY * sensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, -40f, 60f);
        pivot.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        // --- Position camera ---
        cam.transform.position = pivot.position - pivot.forward * distance;
        cam.transform.LookAt(pivot);
    }
}


