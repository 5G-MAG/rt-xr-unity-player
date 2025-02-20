using System;
using System.Text;
using UnityEngine;
using Unity.Profiling;

public class MemoryRecorder : MonoBehaviour
{

    string statsText = "";
    ProfilerRecorder totalReservedMemoryRecorder;
    ProfilerRecorder gcReservedMemoryRecorder;
    ProfilerRecorder systemUsedMemoryRecorder;
    public bool memoryUsageRecorder; // should not be editable at runtime
    bool memoryUsageRecording;
    int avgFpsTotal = 0;
    int avgFpsCount = 0;
    int minFps = int.MaxValue;
    int maxFps = int.MinValue;


    void OnDisable()
    {
            disposeMemoryRecorder();
    }

    void OnGUI()
    {
        if (memoryUsageRecorder)
            GUI.TextArea(new Rect(10, 30, 250, 50), statsText);
    }

    // Update is called once per frame
    void Update()
    {
            if (memoryUsageRecorder)
            {
                if (!memoryUsageRecording)
                    enableMemoryRecorder();
                var sb = new StringBuilder(800);

                // int currentFps = (int)(1f / Time.unscaledDeltaTime);
                int currentFps = (int)(1f / Time.deltaTime);
                avgFpsTotal += currentFps;
                avgFpsCount++;
                if (currentFps != 0)
                {
                    minFps = Math.Min(minFps, currentFps);
                }
                maxFps = Math.Max(maxFps, currentFps);

                sb.AppendLine($"FPS: {avgFpsTotal/avgFpsCount} | {minFps} ~ {maxFps} ");
                if (totalReservedMemoryRecorder.Valid)
                    sb.AppendLine($"Total Reserved Memory: {totalReservedMemoryRecorder.LastValue}");
                if (gcReservedMemoryRecorder.Valid)
                    sb.AppendLine($"GC Reserved Memory: {gcReservedMemoryRecorder.LastValue}");
                if (systemUsedMemoryRecorder.Valid)
                    sb.AppendLine($"System Used Memory: {systemUsedMemoryRecorder.LastValue}");
                statsText = sb.ToString();
            } else {
                if (memoryUsageRecording)
                    disposeMemoryRecorder();
            }

    }


        private void enableMemoryRecorder()
        {
            if (memoryUsageRecording)
            {
                return;
            }
            totalReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Reserved Memory");
            gcReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
            systemUsedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            memoryUsageRecording = true;
        }

        private void disposeMemoryRecorder()
        {
            if (!memoryUsageRecording)
            {
                return;
            }
            totalReservedMemoryRecorder.Dispose();
            gcReservedMemoryRecorder.Dispose();
            systemUsedMemoryRecorder.Dispose();
            memoryUsageRecording = false;
        }
}
