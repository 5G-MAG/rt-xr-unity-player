/*
 * License: 5G-MAG Public License (v1.0)
 * Author: Jordi J. Gimenez (5G-MAG)
 * Copyright: (C) 2026 5G-MAG Association
 *
 * For full license terms please see the LICENSE file distributed with this
 * program. If this file is missing then the license can be retrieved from
 * https://www.5g-mag.com/license.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace rt.xr.unity.app
{
    // Opens every scene in the library in turn, as a flat preview, and reports what was observed: opened or not, how long it
    // took, extensions the loader reported as unsupported, media that could not be found, and warnings. It reports
    // observations only; it does not judge conformance.
    public partial class App
    {
        private bool m_batch;
        private string m_lastFailure = "";
        private class CheckRow { public SceneEntry Entry; public bool Opened, Skipped; public float Seconds; public string Failure; public List<string> Unsupported = new List<string>(); public List<string> Missing = new List<string>(); public int Warnings; }
        private readonly List<CheckRow> m_checkRows = new List<CheckRow>();

        public void RunAllScenes()
        {
            if (m_busy || m_batch || Catalog.Entries.Count == 0) { Toast("Nothing to check yet."); return; }
            StartCoroutine(RunAllRoutine());
        }

        private IEnumerator RunAllRoutine()
        {
            m_batch = true;
            m_checkRows.Clear();
            var all = Catalog.Entries.ToList();
            for (int i = 0; i < all.Count; i++)
            {
                var e = all[i];
                var row = new CheckRow { Entry = e };
                // marker, geometry and geolocation anchors come from a live AR session; the loader's anchor code for them
                // reported errors every frame in a flat preview and the app crashed natively in the ARCore plug-in once
                if (e.TrackableTypes.Any(t => t >= 3 && t <= 6))
                {
                    row.Skipped = true; row.Failure = "not opened: needs a marker, geometry or geolocation anchor from a live AR session";
                    m_checkRows.Add(row);
                    continue;
                }
                float t0 = Time.realtimeSinceStartup;
                m_lastFailure = "";
                OpenScene(e, true);
                float guard = 0f;
                while (m_busy && guard < 240f) { guard += Time.unscaledDeltaTime; yield return null; }
                row.Opened = m_screen == Screen_.Player && m_current == e;
                row.Seconds = Time.realtimeSinceStartup - t0;
                if (row.Opened)
                {
                    yield return new WaitForSecondsRealtime(1.2f);
                    row.Unsupported = UnsupportedByLoader().OrderBy(x => x).ToList();
                    row.Missing = e.Media.Where(m => m.Status == "missing").Select(m => m.Uri.Substring(m.Uri.LastIndexOf('/') + 1)).ToList();
                    row.Warnings = m_logs.Count(l => (l.Type == LogType.Warning || l.Type == LogType.Error || l.Type == LogType.Exception) && !l.Text.Contains("spatializer"));
                    LeavePlayer();
                    yield return new WaitForSecondsRealtime(0.4f);
                }
                else row.Failure = string.IsNullOrEmpty(m_lastFailure) ? "did not open" : m_lastFailure;
                m_checkRows.Add(row);
            }
            m_batch = false;
            ShowCheckReport();
        }

        private string CheckReportText()
        {
            var sb = new StringBuilder();
            sb.AppendLine(ProductName + " " + Application.version + " scene check on " + SystemInfo.deviceModel);
            sb.AppendLine(m_checkRows.Count(r => r.Opened) + " of " + m_checkRows.Count(r => !r.Skipped) + " scenes opened as a flat preview, " + m_checkRows.Count(r => r.Skipped) + " skipped.");
            foreach (var r in m_checkRows)
            {
                sb.Append((r.Opened ? "OK   " : (r.Skipped ? "SKIP " : "FAIL ")) + r.Entry.Uri);
                if (r.Opened) sb.Append(" (" + r.Seconds.ToString("0.0") + " s)");
                if (r.Unsupported.Count > 0) sb.Append(" unsupported: " + string.Join(", ", r.Unsupported));
                if (r.Missing.Count > 0) sb.Append(" media missing: " + string.Join(", ", r.Missing));
                if (!r.Opened) sb.Append(" " + r.Failure);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private void ShowCheckReport()
        {
            try { System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "scene-check.txt"), CheckReportText()); } catch { }
            m_detEntry = null;
            m_detailsOpen = true;
            Ui.Show(m_detGroup, true);
            m_detTitle.text = "Scene check";
            for (int i = m_detContent.childCount - 1; i >= 0; i--) Destroy(m_detContent.GetChild(i).gameObject);
            Ui.Size(Ui.Spacer(m_detContent, 0, 6).gameObject, -1, 6);
            int ok = m_checkRows.Count(r => r.Opened);
            var s = Section(ok + " of " + m_checkRows.Count(r => !r.Skipped) + " opened", "Each scene was opened as a flat preview. This reports what was observed, not conformance.");
            var copy = Ui.Pill(s, "Copy the report", () => { Ui.Copy(CheckReportText()); Toast("Copied to the clipboard"); }, Ui.BtnStyle.Primary, "copy");
            Ui.Size(copy.gameObject, -1, 104, 1);
            foreach (var r in m_checkRows.OrderBy(x => x.Opened).ThenBy(x => x.Entry.Uri))
            {
                var wrap = Pad(m_detContent, 0, 0);
                var card = Ui.Box(wrap, Ui.Card, 28f, "Row");
                Ui.VBox(card.transform, 8, 30);
                var head = Ui.R("Head", card.transform);
                Ui.HBox(head, 14, 0, TextAnchor.MiddleLeft);
                var t = Ui.Label(head, r.Entry.Title + (string.IsNullOrEmpty(r.Entry.Variant) ? "" : " · " + r.Entry.Variant), 32, Ui.Medium, Color.white, TextAnchor.MiddleLeft, true);
                Ui.Size(t.gameObject, -1, -1, 1);
                Ui.Chip(head, r.Opened ? "Opened" : (r.Skipped ? "Skipped" : "Did not open"), r.Opened ? Ui.Good : (r.Skipped ? Ui.Warn : Ui.Bad), false, 24);
                Ui.Label(card.transform, r.Entry.FileName + (r.Opened ? " · " + r.Seconds.ToString("0.0") + " s" : ""), 26, Ui.Regular, Ui.Dim, TextAnchor.MiddleLeft, true);
                if (r.Unsupported.Count > 0) Ui.Label(card.transform, "Reported unsupported: " + string.Join(", ", r.Unsupported), 26, Ui.Regular, Ui.Warn, TextAnchor.UpperLeft, true);
                if (r.Missing.Count > 0) Ui.Label(card.transform, "Media not found: " + string.Join(", ", r.Missing), 26, Ui.Regular, Ui.Warn, TextAnchor.UpperLeft, true);
                if (!r.Opened) Ui.Label(card.transform, r.Failure, 26, Ui.Regular, r.Skipped ? Ui.Warn : Ui.Bad, TextAnchor.UpperLeft, true);
            }
            Ui.Size(Ui.Spacer(m_detContent, 0, 80).gameObject, -1, 80);
            m_detScroll.verticalNormalizedPosition = 1f;
        }
    }
}
