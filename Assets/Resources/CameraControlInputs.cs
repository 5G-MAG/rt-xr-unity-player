//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.7.0
//     from Assets/Resources/CameraControlInputs.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class @CameraControlInputs: IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @CameraControlInputs()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""CameraControlInputs"",
    ""maps"": [
        {
            ""name"": ""CameraControlMap"",
            ""id"": ""4aa2fdd7-f628-4613-bce4-e8e166955eee"",
            ""actions"": [
                {
                    ""name"": ""Forward"",
                    ""type"": ""Button"",
                    ""id"": ""1c41bbdc-47f8-40f8-beaa-798fbc333b99"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Backward"",
                    ""type"": ""Button"",
                    ""id"": ""58837771-9096-438d-9aca-048cf2dbb073"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Left"",
                    ""type"": ""Button"",
                    ""id"": ""85675ef1-3b89-42c9-a531-56664921ad7d"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Right"",
                    ""type"": ""Button"",
                    ""id"": ""71bb95ac-91c6-49ea-a38f-5d5a44a529bf"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Mod_fast"",
                    ""type"": ""Button"",
                    ""id"": ""9d70bc7c-775e-4cdf-a087-b3ab4f8b24dc"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Mod_slow"",
                    ""type"": ""Button"",
                    ""id"": ""f2f52ebd-5a50-4c02-a165-050b5e777ae5"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""LeftClic"",
                    ""type"": ""Button"",
                    ""id"": ""297abb44-728b-4056-9454-16b2cfcaa543"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""RightClic"",
                    ""type"": ""Button"",
                    ""id"": ""ee9e0694-46c7-4bb2-8fe0-09d64b382da5"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""MouseScrollDelta"",
                    ""type"": ""Value"",
                    ""id"": ""842d74c6-1d1c-4dcd-9104-3b115289ed52"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""fca40e99-2651-4b21-b3c6-3cab30923c22"",
                    ""path"": ""<Keyboard>/upArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Forward"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""7ce7f015-ef07-4464-8368-39627638599e"",
                    ""path"": ""<Keyboard>/downArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Backward"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""e8775c00-f9b8-41f1-8e87-300158943237"",
                    ""path"": ""<Keyboard>/leftArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Left"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""f4db7148-9758-4c2f-9d55-5c05ac43c2da"",
                    ""path"": ""<Keyboard>/rightArrow"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Right"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""77c41646-fe39-4e27-8536-3adcb51d517d"",
                    ""path"": ""<Keyboard>/leftShift"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Mod_fast"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""9ebfe697-c3d1-4215-b726-75c890918782"",
                    ""path"": ""<Keyboard>/leftCtrl"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Mod_slow"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""9387436c-1a38-4c38-8f63-294d2ce81903"",
                    ""path"": ""<Mouse>/leftButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""LeftClic"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""ec064937-d322-41a9-96ff-670339ed771f"",
                    ""path"": ""<Mouse>/rightButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""RightClic"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""df46ea82-0f83-4f45-a369-03eb1eec3fbb"",
                    ""path"": ""<Mouse>/scroll"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""MouseScrollDelta"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // CameraControlMap
        m_CameraControlMap = asset.FindActionMap("CameraControlMap", throwIfNotFound: true);
        m_CameraControlMap_Forward = m_CameraControlMap.FindAction("Forward", throwIfNotFound: true);
        m_CameraControlMap_Backward = m_CameraControlMap.FindAction("Backward", throwIfNotFound: true);
        m_CameraControlMap_Left = m_CameraControlMap.FindAction("Left", throwIfNotFound: true);
        m_CameraControlMap_Right = m_CameraControlMap.FindAction("Right", throwIfNotFound: true);
        m_CameraControlMap_Mod_fast = m_CameraControlMap.FindAction("Mod_fast", throwIfNotFound: true);
        m_CameraControlMap_Mod_slow = m_CameraControlMap.FindAction("Mod_slow", throwIfNotFound: true);
        m_CameraControlMap_LeftClic = m_CameraControlMap.FindAction("LeftClic", throwIfNotFound: true);
        m_CameraControlMap_RightClic = m_CameraControlMap.FindAction("RightClic", throwIfNotFound: true);
        m_CameraControlMap_MouseScrollDelta = m_CameraControlMap.FindAction("MouseScrollDelta", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    public IEnumerable<InputBinding> bindings => asset.bindings;

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }

    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // CameraControlMap
    private readonly InputActionMap m_CameraControlMap;
    private List<ICameraControlMapActions> m_CameraControlMapActionsCallbackInterfaces = new List<ICameraControlMapActions>();
    private readonly InputAction m_CameraControlMap_Forward;
    private readonly InputAction m_CameraControlMap_Backward;
    private readonly InputAction m_CameraControlMap_Left;
    private readonly InputAction m_CameraControlMap_Right;
    private readonly InputAction m_CameraControlMap_Mod_fast;
    private readonly InputAction m_CameraControlMap_Mod_slow;
    private readonly InputAction m_CameraControlMap_LeftClic;
    private readonly InputAction m_CameraControlMap_RightClic;
    private readonly InputAction m_CameraControlMap_MouseScrollDelta;
    public struct CameraControlMapActions
    {
        private @CameraControlInputs m_Wrapper;
        public CameraControlMapActions(@CameraControlInputs wrapper) { m_Wrapper = wrapper; }
        public InputAction @Forward => m_Wrapper.m_CameraControlMap_Forward;
        public InputAction @Backward => m_Wrapper.m_CameraControlMap_Backward;
        public InputAction @Left => m_Wrapper.m_CameraControlMap_Left;
        public InputAction @Right => m_Wrapper.m_CameraControlMap_Right;
        public InputAction @Mod_fast => m_Wrapper.m_CameraControlMap_Mod_fast;
        public InputAction @Mod_slow => m_Wrapper.m_CameraControlMap_Mod_slow;
        public InputAction @LeftClic => m_Wrapper.m_CameraControlMap_LeftClic;
        public InputAction @RightClic => m_Wrapper.m_CameraControlMap_RightClic;
        public InputAction @MouseScrollDelta => m_Wrapper.m_CameraControlMap_MouseScrollDelta;
        public InputActionMap Get() { return m_Wrapper.m_CameraControlMap; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(CameraControlMapActions set) { return set.Get(); }
        public void AddCallbacks(ICameraControlMapActions instance)
        {
            if (instance == null || m_Wrapper.m_CameraControlMapActionsCallbackInterfaces.Contains(instance)) return;
            m_Wrapper.m_CameraControlMapActionsCallbackInterfaces.Add(instance);
            @Forward.started += instance.OnForward;
            @Forward.performed += instance.OnForward;
            @Forward.canceled += instance.OnForward;
            @Backward.started += instance.OnBackward;
            @Backward.performed += instance.OnBackward;
            @Backward.canceled += instance.OnBackward;
            @Left.started += instance.OnLeft;
            @Left.performed += instance.OnLeft;
            @Left.canceled += instance.OnLeft;
            @Right.started += instance.OnRight;
            @Right.performed += instance.OnRight;
            @Right.canceled += instance.OnRight;
            @Mod_fast.started += instance.OnMod_fast;
            @Mod_fast.performed += instance.OnMod_fast;
            @Mod_fast.canceled += instance.OnMod_fast;
            @Mod_slow.started += instance.OnMod_slow;
            @Mod_slow.performed += instance.OnMod_slow;
            @Mod_slow.canceled += instance.OnMod_slow;
            @LeftClic.started += instance.OnLeftClic;
            @LeftClic.performed += instance.OnLeftClic;
            @LeftClic.canceled += instance.OnLeftClic;
            @RightClic.started += instance.OnRightClic;
            @RightClic.performed += instance.OnRightClic;
            @RightClic.canceled += instance.OnRightClic;
            @MouseScrollDelta.started += instance.OnMouseScrollDelta;
            @MouseScrollDelta.performed += instance.OnMouseScrollDelta;
            @MouseScrollDelta.canceled += instance.OnMouseScrollDelta;
        }

        private void UnregisterCallbacks(ICameraControlMapActions instance)
        {
            @Forward.started -= instance.OnForward;
            @Forward.performed -= instance.OnForward;
            @Forward.canceled -= instance.OnForward;
            @Backward.started -= instance.OnBackward;
            @Backward.performed -= instance.OnBackward;
            @Backward.canceled -= instance.OnBackward;
            @Left.started -= instance.OnLeft;
            @Left.performed -= instance.OnLeft;
            @Left.canceled -= instance.OnLeft;
            @Right.started -= instance.OnRight;
            @Right.performed -= instance.OnRight;
            @Right.canceled -= instance.OnRight;
            @Mod_fast.started -= instance.OnMod_fast;
            @Mod_fast.performed -= instance.OnMod_fast;
            @Mod_fast.canceled -= instance.OnMod_fast;
            @Mod_slow.started -= instance.OnMod_slow;
            @Mod_slow.performed -= instance.OnMod_slow;
            @Mod_slow.canceled -= instance.OnMod_slow;
            @LeftClic.started -= instance.OnLeftClic;
            @LeftClic.performed -= instance.OnLeftClic;
            @LeftClic.canceled -= instance.OnLeftClic;
            @RightClic.started -= instance.OnRightClic;
            @RightClic.performed -= instance.OnRightClic;
            @RightClic.canceled -= instance.OnRightClic;
            @MouseScrollDelta.started -= instance.OnMouseScrollDelta;
            @MouseScrollDelta.performed -= instance.OnMouseScrollDelta;
            @MouseScrollDelta.canceled -= instance.OnMouseScrollDelta;
        }

        public void RemoveCallbacks(ICameraControlMapActions instance)
        {
            if (m_Wrapper.m_CameraControlMapActionsCallbackInterfaces.Remove(instance))
                UnregisterCallbacks(instance);
        }

        public void SetCallbacks(ICameraControlMapActions instance)
        {
            foreach (var item in m_Wrapper.m_CameraControlMapActionsCallbackInterfaces)
                UnregisterCallbacks(item);
            m_Wrapper.m_CameraControlMapActionsCallbackInterfaces.Clear();
            AddCallbacks(instance);
        }
    }
    public CameraControlMapActions @CameraControlMap => new CameraControlMapActions(this);
    public interface ICameraControlMapActions
    {
        void OnForward(InputAction.CallbackContext context);
        void OnBackward(InputAction.CallbackContext context);
        void OnLeft(InputAction.CallbackContext context);
        void OnRight(InputAction.CallbackContext context);
        void OnMod_fast(InputAction.CallbackContext context);
        void OnMod_slow(InputAction.CallbackContext context);
        void OnLeftClic(InputAction.CallbackContext context);
        void OnRightClic(InputAction.CallbackContext context);
        void OnMouseScrollDelta(InputAction.CallbackContext context);
    }
}