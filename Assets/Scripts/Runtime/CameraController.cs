/*
 * Copyright (c) 2023 MotionSpell
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

public class CameraController : MonoBehaviour {

    public KeyCode FORWARD = KeyCode.UpArrow;
    public KeyCode BACKWARD = KeyCode.DownArrow;
    public KeyCode LEFT = KeyCode.LeftArrow;
    public KeyCode RIGHT = KeyCode.RightArrow;

    private KeyCode MOD_FAST = KeyCode.LeftShift;
    private KeyCode MOD_SLOW = KeyCode.LeftControl;

    public float lookAroundSpeedFactor = 1f;
    public float scrollSpeedFactor = 1f;
    public float moveSpeedFactor = 1f;

    private float baseSpeedFactor = 0.5f;


    void Update() {

        Vector3 pos = new Vector3(0,0,0);
        Quaternion rot = transform.rotation;

        if (
            Input.GetKeyUp(MOD_FAST) || 
            Input.GetKeyUp(MOD_SLOW) ||
            Input.GetKeyUp(FORWARD) ||
            Input.GetKeyUp(BACKWARD) ||
            Input.GetKeyUp(LEFT) ||
            Input.GetKeyUp(RIGHT)
            )
        {
            baseSpeedFactor = 0.5f;
        }
        if (Input.GetKey(MOD_FAST))
        {
            if (baseSpeedFactor < 8)
                baseSpeedFactor *= 1.01f;
        }
        if (Input.GetKey(MOD_SLOW))
        {
            if (baseSpeedFactor > 0.01f)
                baseSpeedFactor *= 0.99f;
        }

        // Right btn => look around
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            float mouseY = Input.GetAxis("Mouse Y") * baseSpeedFactor * lookAroundSpeedFactor;
            float mouseX = Input.GetAxis("Mouse X") * baseSpeedFactor * lookAroundSpeedFactor;
            rot = Quaternion.Euler(rot.eulerAngles.x - mouseY, rot.eulerAngles.y + mouseX, rot.eulerAngles.z);
        }
        
        if (Input.GetKey(FORWARD))
        {
            pos.z = baseSpeedFactor * moveSpeedFactor;
        }
        else if (Input.GetKey(BACKWARD))
        {
            pos.z = -baseSpeedFactor * moveSpeedFactor;
        }

        if (Input.GetKey(LEFT))
        {
            pos.x = -baseSpeedFactor * moveSpeedFactor;
        }
        else if (Input.GetKey(RIGHT))
        {
            pos.x = baseSpeedFactor * moveSpeedFactor;
        }

        pos.y = -Input.mouseScrollDelta.y * baseSpeedFactor * scrollSpeedFactor;

        pos = BasisRotate(pos, rot);

        transform.position += pos; 
        transform.rotation = rot;
        
    }

    private Vector3 BasisRotate(Vector3 posIn, Quaternion rot)
    {
        Vector3 posOut = new Vector3(0, 0, 0);

        posOut.x = posIn.z * Mathf.Sin(rot.eulerAngles.y * Mathf.PI / 180)
                 + posIn.x * Mathf.Cos(rot.eulerAngles.y * Mathf.PI / 180);

        posOut.y = posIn.y;

        posOut.z = posIn.z * Mathf.Cos(rot.eulerAngles.y * Mathf.PI / 180)
                 - posIn.x * Mathf.Sin(rot.eulerAngles.y * Mathf.PI / 180);
  
        return posOut;
    }

}