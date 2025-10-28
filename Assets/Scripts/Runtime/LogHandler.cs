using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LogHandler : MonoBehaviour
{
        private uint maxLogMessages = 15;
        private Queue logQueue = new Queue();
        public bool showLog = false;

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            logQueue.Enqueue("[" + type + "] : " + logString);
            if (type == LogType.Exception)
                logQueue.Enqueue(stackTrace);
            while (logQueue.Count > maxLogMessages)
                logQueue.Dequeue();
        }

        void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        void Update(){
            if (Input.GetKeyDown(KeyCode.L))
            {
                if(showLog){
                    showLog = false;
                } else {
                    showLog = true;
                }
            }
        }

        void OnGUI()
        {
            if (showLog)
            {
                int offset = 100;
                GUILayout.BeginArea(new Rect(0, offset, Screen.width, Screen.height-2*offset));
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                GUILayout.Label("\n" + string.Join("\n", logQueue.ToArray()));
                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }

}
