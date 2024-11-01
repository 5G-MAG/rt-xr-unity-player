/*
 * Copyright (c) 2024 MotionSpell
 * Licensed under the License terms and conditions for use, reproduction,
 * and distribution of 5GMAG software (the “License”).
 * You may not use this file except in compliance with the License.
 * You may obtain a copy of the License at https://www.5g-mag.com/license .
 * Unless required by applicable law or agreed to in writing, software distributed under the License is
 * distributed on an “AS IS” BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and limitations under the License.
 */

using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCameraController : MonoBehaviour
{    
    // Input Actions for looking and movement
    private InputAction lookAction;
    private InputAction moveAction;
    private InputAction pointerPressAction;

    private bool pointerIsPressed = false;
    private Camera cam;
    
    private void Awake()
    {
        cam = Camera.main;

        lookAction = new InputAction("Look", binding: "<Pointer>/delta");
        pointerPressAction = new InputAction("PointerPress", binding: "<Pointer>/press");
        moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        lookAction.Enable();
        moveAction.Enable();
        pointerPressAction.Enable();

        pointerPressAction.started += _ => pointerIsPressed = true;
        pointerPressAction.canceled += _ => pointerIsPressed = false;
    }

    private void Update()
    {
        if (pointerIsPressed){
            HandleLook();
        }
        HandleMovement();
    }

    private void HandleLook()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        float deltaX = lookInput.x * Time.deltaTime;
        float deltaY = lookInput.y * Time.deltaTime;
        cam.transform.Rotate(Vector3.up, deltaX, Space.World);
        // TODO: clamp that to +/- 90
        cam.transform.Rotate(Vector3.right, deltaY);
    }

    private void HandleMovement()
    {
        if (Input.touchSupported && (Input.touchCount > 1)){
            Touch tZero = Input.GetTouch(0);
            Touch tOne = Input.GetTouch(1);
            Vector2 tZeroPrevious = tZero.position - tZero.deltaPosition;
            Vector2 tOnePrevious = tOne.position - tOne.deltaPosition;
            float oldTouchDistance = Vector2.Distance(tZeroPrevious, tOnePrevious);
            float currentTouchDistance = Vector2.Distance(tZero.position, tOne.position);
            float deltaDistance = oldTouchDistance - currentTouchDistance;
            cam.transform.Translate(cam.transform.forward * deltaDistance * Time.deltaTime, Space.World);            
        } else {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            Vector3 moveDirection = cam.transform.right * moveInput.x + cam.transform.forward * moveInput.y;
            cam.transform.Translate(moveDirection * Time.deltaTime, Space.World);
        }
    }

    private void OnDisable()
    {
        lookAction.Disable();
        moveAction.Disable();
        pointerPressAction.Disable();
    }

}
