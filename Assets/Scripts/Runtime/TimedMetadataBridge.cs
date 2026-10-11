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

using System;
using System.Collections.Generic;

using UnityEngine;

using Unity.Collections;

using GLTFast;


using maf;

namespace rt.xr.unity
{

    /// <summary>
    /// Moves frames of timed metadata accessors (MPEG_accessor_timed, ISO/IEC 23090-14:2024, 5.2.2)
    /// from the media buffers to GLTFast.MpegTimedAccessors, where the scene extensions
    /// (animation timing, viewport, mesh linking, lights) read them. Only accessors that a scene
    /// extension registered with MpegTimedAccessors.Want are read. When a media entry has no
    /// pipeline or no frames, nothing is published and the scene keeps its constant values.
    /// Frames are published when their time is reached on the media clock.
    /// </summary>
    public class TimedMetadataBridge : MonoBehaviour
    {
        SceneViewer viewer;
        readonly Dictionary<int, BufferInfoHeaderArray> headers = new Dictionary<int, BufferInfoHeaderArray>();
        readonly HashSet<string> warned = new HashSet<string>();

        public void Init(SceneViewer sceneViewer)
        {
            viewer = sceneViewer;
            headers.Clear();
            warned.Clear();
            pendingUpdates.Clear();
            pendingSamples.Clear();
        }

        void Warn(string key, string message)
        {
            if (warned.Add(key))
            {
                Debug.LogWarning(message);
            }
        }

        void Update()
        {
            if (viewer == null) return;
            var import = viewer.CurrentImport;
            var players = viewer.MediaPlayers;
            if (import == null || players == null) return;
            PumpSceneUpdates(players);
            var root = import.GetSourceRoot();
            if (root == null || root.accessors == null || root.bufferViews == null || root.buffers == null) return;

            // the media clock decides when scene updates (MPEG_scene_dynamic) become active
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null && players[i].HasPipeline && players[i].CurrentTime >= 0)
                {
                    MpegSceneUpdates.SetMediaTime(i, players[i].CurrentTime);
                }
            }

            foreach (int accessorIndex in MpegTimedAccessors.GetWanted())
            {
                if (warned.Add("seen/" + accessorIndex)) Debug.Log("XRTEST timed accessor wanted: " + accessorIndex);
                try
                {
                    Pump(accessorIndex, root, players);
                }
                catch (Exception e)
                {
                    Warn("pump/" + accessorIndex, "timed metadata accessor " + accessorIndex + ": " + e.Message);
                }
            }
        }

        /// <summary>
        /// Seconds before its presentation time at which a scene update sample is handed over to MpegSceneUpdates.
        /// Implementation default, not a value of the specification: the update is applied when its time is reached,
        /// the lead only lets the assets of new nodes be fetched ahead of time (7.2.1, NOTE).
        /// </summary>
        public static double SceneUpdateLookahead = 2.0;

        // Media without accessor (MPEG_scene_dynamic): the frames of the media player's scene update buffer are
        // [flags: bit 0 sync sample][update_mode, 0xFF when not available][sample bytes] (MAF.AttributeType.METADATA_SCENE_UPDATE).
        void PumpSceneUpdates(IReadOnlyList<MediaPlayer> players)
        {
            for (int i = 0; i < players.Count; i++)
            {
                var mp = players[i];
                if (mp == null || !mp.HasSceneUpdateBuffer) continue;
                try
                {
                    ReadSceneUpdates(i, mp);
                }
                catch (Exception e)
                {
                    Warn("sceneupdate/" + i, "scene update media " + i + ": " + e.Message);
                }
            }
        }

        void ReadSceneUpdates(int media, MediaPlayer mp)
        {
            var mb = mp.SceneUpdateBuffer;
            if (mb == null || mb.Handler == null) return;
            if (mp.CurrentTime >= 0)
            {
                MpegSceneUpdates.SetMediaTime(media, mp.CurrentTime);
            }
            // the timestamp of a frame is read from the frame itself, once it has been taken out of the buffer: the
            // timestamp of the next frame of the buffer is not that of the frame ReadFrame() then returns
            List<PendingUpdate> pending;
            if (!pendingUpdates.TryGetValue(media, out pending)) { pending = new List<PendingUpdate>(); pendingUpdates[media] = pending; }
            while (pending.Count < MaxPending)
            {
                Frame frame = mb.ReadFrame();
                if (frame == null) break;
                NativeArray<byte> data = mb.GetNativeArray(frame);
                if (data.Length < 2) continue;
                var p = new PendingUpdate();
                p.time = maf.maf.getTime_s(frame.timestamp);
                p.sync = (data[0] & 0x01) != 0;
                p.mode = data[1];
                p.payload = new byte[data.Length - 2];
                if (p.payload.Length > 0)
                {
                    NativeArray<byte>.Copy(data, 2, p.payload, 0, p.payload.Length);
                }
                pending.Add(p);
            }
            while (pending.Count > 0 && pending[0].time <= mp.CurrentTime + SceneUpdateLookahead)
            {
                var p = pending[0];
                pending.RemoveAt(0);
                int updateMode = p.mode;
                if (updateMode == 0xFF)
                {
                    Warn("updatemode/" + media, "scene update media " + media + ": update_mode of the GLTFPatchConfigBox cannot be read from the sample entry (ISO/IEC 14496-12 was not available), update_mode 0 is used for patch samples");
                    updateMode = 0;
                }
                Debug.Log("XRTEST scene update enqueued media=" + media + " t=" + p.time.ToString("0.00") + " sync=" + p.sync + " bytes=" + p.payload.Length + " clock=" + mp.CurrentTime.ToString("0.00"));
                MpegSceneUpdates.Enqueue(media, -1, p.time, p.sync, p.payload, updateMode);
            }
        }

        class PendingUpdate { public double time; public bool sync; public int mode; public byte[] payload; }
        class PendingSample { public double time; public byte[] bytes; public int count; }
        const int MaxPending = 64;
        readonly Dictionary<int, List<PendingUpdate>> pendingUpdates = new Dictionary<int, List<PendingUpdate>>();
        readonly Dictionary<int, List<PendingSample>> pendingSamples = new Dictionary<int, List<PendingSample>>();

        void Pump(int accessorIndex, GLTFast.Schema.Root root, IReadOnlyList<MediaPlayer> players)
        {
            if (accessorIndex < 0 || accessorIndex >= root.accessors.Length) return;
            var acc = root.accessors[accessorIndex];
            if (acc.bufferView < 0 || acc.bufferView >= root.bufferViews.Length) return;
            var bv = root.bufferViews[acc.bufferView];
            if (bv.buffer < 0 || bv.buffer >= root.buffers.Length) return;
            var circular = root.buffers[bv.buffer].extensions != null ? root.buffers[bv.buffer].extensions.MPEG_buffer_circular : null;
            if (circular == null || circular.media < 0)
            {
                Warn("circular/" + accessorIndex, "timed metadata accessor " + accessorIndex + " is not backed by an MPEG_buffer_circular buffer");
                return;
            }
            if (circular.media >= players.Count || players[circular.media] == null || !players[circular.media].HasPipeline)
            {
                Warn("media/" + accessorIndex, "timed metadata accessor " + accessorIndex + ": media " + circular.media + " has no running pipeline, the scene keeps its constant values");
                return;
            }
            var mp = players[circular.media];
            foreach (var mb in mp.Buffers)
            {
                if (mb.bufferId != bv.buffer || mb.Handler == null) continue;
                ReadFrames(accessorIndex, acc, bv, mp, mb);
            }
        }

        void ReadFrames(int accessorIndex, GLTFast.Schema.Accessor acc, GLTFast.Schema.BufferView bv, MediaPlayer mp, MediaBufferHandler mb)
        {
            bool hasHeader = acc.extensions != null && acc.extensions.MPEG_accessor_timed != null && acc.extensions.MPEG_accessor_timed.bufferView >= 0;
            BufferInfoHeaderArray hdr = null;
            int position = 0;
            if (hasHeader)
            {
                if (!headers.TryGetValue(mb.bufferId, out hdr))
                {
                    hdr = BufferInfoHeader.CreateAccessorsHeaders(mb.BufferInfo, true);
                    headers[mb.bufferId] = hdr;
                }
                // position of the accessor among the accessors of the buffer
                for (int j = 0; j < mb.BufferInfo.Count; j++)
                {
                    if (mb.BufferInfo[j].offset == acc.byteOffset + bv.byteOffset)
                    {
                        position = j;
                        break;
                    }
                }
            }
            int elementSize = GLTFast.Schema.Accessor.GetComponentTypeSize(acc.componentType) * GLTFast.Schema.Accessor.GetAccessorAttributeTypeLength(acc.typeEnum);

            List<PendingSample> queue;
            if (!pendingSamples.TryGetValue(accessorIndex, out queue)) { queue = new List<PendingSample>(); pendingSamples[accessorIndex] = queue; }
            while (queue.Count < MaxPending)
            {
                Frame frame = mb.ReadFrame();
                if (frame == null) break;
                double time = maf.maf.getTime_s(frame.timestamp);

                int count = acc.count;
                NativeArray<byte> data;
                if (hasHeader)
                {
                    BufferInfoHeader.ReadAccessorsHeaders(frame.data, hdr);
                    if (position < hdr.Count && hdr[position].count > 0) count = (int)hdr[position].count;
                    // Table 8: timestamp_delta is added to the timestamp of the buffer frame
                    if (position < hdr.Count) time += hdr[position].timestampDelta;
                    data = mb.GetNativeArray(frame, hdr);
                }
                else
                {
                    var whole = mb.GetNativeArray(frame);
                    int start = bv.byteOffset + acc.byteOffset;
                    if (start > whole.Length) continue;
                    data = whole.GetSubArray(start, whole.Length - start);
                }
                int length = Math.Min(data.Length, Math.Max(0, count) * elementSize);
                var bytes = new byte[length];
                if (length > 0)
                {
                    NativeArray<byte>.Copy(data, 0, bytes, 0, length);
                }
                queue.Add(new PendingSample { time = time, bytes = bytes, count = count });
            }
            // frames are published when their time is reached on the media clock
            while (queue.Count > 0 && queue[0].time <= mp.CurrentTime)
            {
                var sample = queue[0];
                queue.RemoveAt(0);
                Debug.Log("XRTEST timed publish accessor " + accessorIndex + " t=" + sample.time.ToString("0.00") + " bytes=" + sample.bytes.Length + " count=" + sample.count + " clock=" + mp.CurrentTime.ToString("0.00"));
                MpegTimedAccessors.Publish(accessorIndex, sample.time, sample.bytes, sample.count);
            }
        }
    }
}
