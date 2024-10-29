using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitControl : MonoBehaviour
{
    // optional target GameObject
    public Transform target;

    private float currentX = 0f;
    private float currentY = 0f;
    private float currentDistance;

    private InputAction lookAction;
    private InputAction moveAction;
    private InputAction pointerPressAction;
    
    private bool pointerIsPressed = false;
    private Camera cam;

    private void SetInitialState()
    {
        cam = Camera.main;
        if(cam == null){
            return;
        }
        Vector3 targetPosition = GetPointOfInterest();
        currentDistance = Vector3.Distance(targetPosition, cam.transform.position);
        Quaternion qr = Quaternion.LookRotation(targetPosition - cam.transform.position, Vector3.up);
        if (qr.eulerAngles.x > 90){
            currentY = qr.eulerAngles.x - 360;
        }  else {
            currentY = qr.eulerAngles.x;
        }
        currentX = qr.eulerAngles.y;
    }

    private void Awake()
    {
        SetInitialState(); // ensure consistency when switching to this controls

        lookAction = new InputAction("Look", binding: "<Pointer>/delta");
        pointerPressAction = new InputAction("PointerPress", binding: "<Pointer>/press");
        moveAction = new InputAction("Zoom", binding: "<Mouse>/scroll");

        lookAction.Enable();
        moveAction.Enable();
        pointerPressAction.Enable();

        pointerPressAction.started += _ => pointerIsPressed = true;
        pointerPressAction.canceled += _ => pointerIsPressed = false;

    }

    private void Update()
    {
        // Handle pointer position
        if (pointerIsPressed){
            Vector2 lookInput = lookAction.ReadValue<Vector2>();
            currentX += lookInput.x * Time.deltaTime;
            currentY -= lookInput.y * Time.deltaTime;
            currentY = Mathf.Clamp(currentY, -90f, 90f);
        }

        if (Input.touchSupported && (Input.touchCount > 1)){
            Touch tZero = Input.GetTouch(0);
            Touch tOne = Input.GetTouch(1);
            Vector2 tZeroPrevious = tZero.position - tZero.deltaPosition;
            Vector2 tOnePrevious = tOne.position - tOne.deltaPosition;
            float oldTouchDistance = Vector2.Distance(tZeroPrevious, tOnePrevious);
            float currentTouchDistance = Vector2.Distance(tZero.position, tOne.position);
            float deltaDistance = oldTouchDistance - currentTouchDistance;
            currentDistance  -= deltaDistance * Time.deltaTime;
        } else {
            float moveInput = moveAction.ReadValue<Vector2>().y;
            currentDistance -= moveInput * Time.deltaTime;

        }
    }

    private void LateUpdate()
    {
        Vector3 targetPosition = GetPointOfInterest();
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        cam.transform.position = targetPosition - (rotation * Vector3.forward * currentDistance);
        cam.transform.LookAt(targetPosition);
    }

    private void OnEnable(){
        SetInitialState();
    }


    private void OnDisable()
    {
        lookAction.Disable();
        moveAction.Disable();
        pointerPressAction.Disable();
    }


    private Vector3 GetPointOfInterest(){
        if (target != null){
            return target.position;
        };
        return Vector3.zero;
    }

}
