using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 2.2f, -6f);
    public float mouseSensitivity = 2.4f;
    public float minPitch = -20f;
    public float maxPitch = 55f;

    float yaw;
    float pitch = 12f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (target != null)
            yaw = target.eulerAngles.y;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = target.position + rot * offset;
        transform.LookAt(target.position + Vector3.up * 1.4f);
    }
}
