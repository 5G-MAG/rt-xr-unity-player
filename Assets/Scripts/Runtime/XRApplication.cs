using rt.xr.unity;
using UnityEngine;
using GLTFast;
using System;
using UnityEngine.XR.ARFoundation;
using System.Collections.Generic;

namespace rt.xr.unity
{
    public class XRApplication : MonoBehaviour
    {
        private ARRaycastManager m_RaycastManager;
        private List<ARRaycastHit> m_RaycastHitList;
        private Camera m_Camera;

        private bool m_IsAnchored;
        private bool m_IsPressed;
        private GameObject m_AnchoredGameObject;
        private Vector2 m_LastMousePos;

        public void StartApplication()
        {
            UserInputModule.GetInstance().Register(OnUserInput, 0);
            ActionModule.GetInstance().Register(OnActionManipulate, 0);
            TrackableModule.GetInstance().Register(OnTrackableEvent, 0);
            Debug.LogError("XR Application behavior started");
        }

        private void OnActionManipulate(MPEG_ActionEvent _event)
        {
            MPEG_ActionManipulateEvent _manipulate = (MPEG_ActionManipulateEvent)_event;
            m_LastMousePos = _manipulate.inputAction.ReadValue<Vector2>();
        }

        private void OnTrackableEvent(MPEG_TrackableEvent _event)
        {
            if (_event.trackableType == GLTFast.Schema.TrackableType.TRACKABLE_FLOOR)
            {
                if (_event.trackableEventType == TrackableEventType.ADDED)
                {
                    m_IsAnchored = true;
                    m_AnchoredGameObject = VirtualSceneGraph.GetGameObjectFromIndex(3);
                    m_AnchoredGameObject.transform.localScale *= 0.95f;

                    m_RaycastManager = FindObjectOfType<ARRaycastManager>();
                    if (m_RaycastManager == null)
                    {
                        m_RaycastManager = gameObject.AddComponent<ARRaycastManager>();
                    }

                    m_RaycastHitList = new List<ARRaycastHit>();
                    m_Camera = Camera.main;
                }
            }
        }

        private void Update()
        {
            if (!m_IsAnchored)
                return;

            if (!m_IsPressed)
                return;

            Ray _ray = m_Camera.ScreenPointToRay(m_LastMousePos);

            if (m_RaycastManager.Raycast(_ray, m_RaycastHitList, UnityEngine.XR.ARSubsystems.TrackableType.Planes))
            {
                Pose _hitPose = m_RaycastHitList[0].pose;
                Vector3 _offset = new Vector3(0, -0.2f, 0);
                m_AnchoredGameObject.transform.position = _hitPose.position + _offset;
            }
        }

        private void OnUserInput(MPEG_UserInputEvent _event)
        {
            m_IsPressed = _event.isPerformed;
        }
    }
}
