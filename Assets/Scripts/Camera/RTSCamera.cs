using UnityEngine;

public class RTSCamera : MonoBehaviour
{
    //Pan
    public float moveSpeed = 20f;
    public float shiftMultiplier = 2f;
    public float posLerpTime = 10f;

    //Rotation
    public float rotSpeed = 2f;
    public float rotLerpTime = 10f;
    public Vector2 pitchLimits = new Vector2(20f, 80f);

    //Zoom
    public float scrollSensitivity = 10f;
    public float zoomLerpTime = 5f;
    public Vector2 heightLimits = new Vector2(5f, 100f);

    //Bounds
    public bool useBounds = true;
    public Vector2 minmapBounds = new Vector2(100f, 100f); 
    public Vector2 maxmapBounds = new Vector2(100f, 100f);
    public LayerMask groundLayer;
    public float groundOffset = 2f;

    private Vector3 targetPos;
    private Quaternion targetRot;
    private float currentYaw;
    private float currentPitch;

    private void Start()
    {
        targetPos = transform.position;
        targetRot = transform.rotation;

        currentYaw = transform.eulerAngles.y;
        currentPitch = transform.eulerAngles.x;
    }

    private void Update()
    {
        HandleInput();

        ApplyConstraints();

        //Smooth the movement
        float dt = Time.deltaTime;
        transform.position = Vector3.Lerp(transform.position, targetPos, dt * posLerpTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, dt * rotLerpTime);
    }

    void HandleInput()
    {
        float speed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * shiftMultiplier : moveSpeed;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        if (Mathf.Abs(x) > 0.01f || Mathf.Abs(z) > 0.01f)
        {
            //Move relative to camera facing
            Vector3 fwd = transform.forward;
            fwd.y = 0;
            Vector3 right = transform.right;
            right.y = 0;

            Vector3 dir = (fwd.normalized * z + right.normalized * x).normalized;
            targetPos += dir * speed * Time.deltaTime;
        }

        //rotation (Middle Mouse)
        if (Input.GetMouseButton(2))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotSpeed;

            currentYaw += mouseX;
            currentPitch -= mouseY;
            currentPitch = Mathf.Clamp(currentPitch, pitchLimits.x, pitchLimits.y);

            targetRot = Quaternion.Euler(currentPitch, currentYaw, 0f);
        }

        //zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            Vector3 zoomDir = transform.forward * scroll * scrollSensitivity;
            targetPos += zoomDir;
        }
    }

    void ApplyConstraints()
    {
        //Height check
        targetPos.y = Mathf.Clamp(targetPos.y, heightLimits.x, heightLimits.y);

        //map Bounds
        if (useBounds)
        {
            targetPos.x = Mathf.Clamp(targetPos.x, minmapBounds.x, maxmapBounds.x);
            targetPos.z = Mathf.Clamp(targetPos.z, minmapBounds.y, maxmapBounds.y);
        }

        //Terrain collision
        Ray ray = new Ray(new Vector3(targetPos.x, 5000f, targetPos.z), Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 10000f, groundLayer))
        {
            float minH = hit.point.y + groundOffset;
            if (targetPos.y < minH) targetPos.y = minH;
        }
    }


}