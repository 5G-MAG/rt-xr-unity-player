// Touches read through the Input System package, handed out as UnityEngine.Touch so that the gesture code reads them the same way it read
// UnityEngine.Input.touches. The project runs with the Input System only (Player Settings > Active Input Handling), where every call to
// UnityEngine.Input throws. fingerId is the index of the finger, which stays the same for as long as the finger is down.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using ETouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace rt.xr.unity
{
    public static class TouchInput
    {
        private static void Ensure()
        {
            if (!EnhancedTouchSupport.enabled) EnhancedTouchSupport.Enable();
        }

        // the fingers on the screen this frame, including those lifted this frame (phase Ended or Canceled)
        public static int Count
        {
            get { Ensure(); return ETouch.activeTouches.Count; }
        }

        public static UnityEngine.Touch Get(int index)
        {
            Ensure();
            return Convert(ETouch.activeTouches[index]);
        }

        public static UnityEngine.Touch[] All
        {
            get
            {
                Ensure();
                var active = ETouch.activeTouches;
                var touches = new UnityEngine.Touch[active.Count];
                for (int i = 0; i < touches.Length; i++) touches[i] = Convert(active[i]);
                return touches;
            }
        }

        // whether the point on the screen is over the UI (or anything else the EventSystem's raycasters hit)
        private static readonly List<RaycastResult> s_hits = new List<RaycastResult>();
        public static bool OverUi(Vector2 screenPosition)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            s_hits.Clear();
            es.RaycastAll(new PointerEventData(es) { position = screenPosition }, s_hits);
            return s_hits.Count > 0;
        }

        private static UnityEngine.Touch Convert(ETouch t)
        {
            return new UnityEngine.Touch
            {
                fingerId = t.finger.index,
                position = t.screenPosition,
                rawPosition = t.screenPosition,
                deltaPosition = t.delta,
                type = TouchType.Direct,
                phase = Convert(t.phase),
            };
        }

        private static UnityEngine.TouchPhase Convert(ETouchPhase phase)
        {
            switch (phase)
            {
                case ETouchPhase.Began: return UnityEngine.TouchPhase.Began;
                case ETouchPhase.Moved: return UnityEngine.TouchPhase.Moved;
                case ETouchPhase.Ended: return UnityEngine.TouchPhase.Ended;
                case ETouchPhase.Canceled: return UnityEngine.TouchPhase.Canceled;
                default: return UnityEngine.TouchPhase.Stationary;
            }
        }
    }
}
