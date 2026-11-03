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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace rt.xr.unity.app
{
    // Colours, fonts and small builders for the player's interface. The look follows the 5G-MAG Android apps: dark
    // streaming theme, 5G-MAG blue accent, Ubuntu type. All layout is built in code so no scene file changes.
    public static class Ui
    {
        private static Color C(byte r, byte g, byte b, byte a = 255) { return new Color32(r, g, b, a); }

        public static readonly Color Bg = C(0x12, 0x12, 0x12);
        public static readonly Color Card = C(0x1E, 0x1E, 0x1E);
        public static readonly Color Raised = C(0x2A, 0x2A, 0x2A);
        public static readonly Color Line = C(0x2C, 0x2C, 0x2C);
        public static readonly Color Dim = C(0xB3, 0xB3, 0xB3);
        public static readonly Color Accent = C(0x00, 0xA0, 0xD2);
        public static readonly Color AccentSoft = new Color(0f, 0.63f, 0.82f, 0.2f);
        public static readonly Color OnAccent = C(0x00, 0x23, 0x2D);
        public static readonly Color Warn = C(0xF2, 0xA3, 0x3A);
        public static readonly Color Good = C(0x4C, 0xC3, 0x8A);
        public static readonly Color Bad = C(0xFF, 0x6B, 0x6B);
        public static readonly Color Audio = C(0xB5, 0x7C, 0xF2);
        public static readonly Color LightC = C(0xFF, 0xD2, 0x4A);
        public static readonly Color MaterialC = C(0x6F, 0xD3, 0xFF);
        public static readonly Color CompressC = C(0xF5, 0x8B, 0xB3);
        public static readonly Color Scrim = new Color(0.03f, 0.03f, 0.03f, 0.72f);
        public static readonly Color Glass = new Color(0f, 0f, 0f, 0.45f);

        public static Font Regular, Medium, Bold;
        private static Sprite s_round, s_white, s_gradDown, s_gradUp, s_ring, s_outline;
        private static Material s_lineMat;
        private static readonly Dictionary<string, Sprite> s_icons = new Dictionary<string, Sprite>();
        private static bool s_ready;

        public static void Init()
        {
            if (s_ready) return;
            s_ready = true;
            Regular = Resources.Load<Font>("App/Fonts/Ubuntu-Regular");
            Medium = Resources.Load<Font>("App/Fonts/Ubuntu-Medium");
            Bold = Resources.Load<Font>("App/Fonts/Ubuntu-Bold");
            if (Regular == null) Regular = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (Medium == null) Medium = Regular;
            if (Bold == null) Bold = Regular;
            s_round = MakeRounded(64, 24);
            s_white = MakeWhite();
            s_gradDown = MakeGradient(true);
            s_gradUp = MakeGradient(false);
            s_ring = MakeRing(128);
            s_outline = MakeOutline(48, 12, 4);
        }

        public static Sprite Ring { get { return s_ring; } }
        public static Sprite Outline { get { return s_outline; } }

        public static Material LineMaterial()
        {
            if (s_lineMat != null) return s_lineMat;
            var sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("UI/Default");
            if (sh == null) sh = Shader.Find("Hidden/Internal-Colored");
            s_lineMat = new Material(sh);
            return s_lineMat;
        }
        public static Sprite GradDown { get { return s_gradDown; } }
        public static Sprite GradUp { get { return s_gradUp; } }

        public static Sprite Icon(string name)
        {
            Sprite s;
            if (s_icons.TryGetValue(name, out s)) return s;
            var tex = Resources.Load<Texture2D>("App/Icons/" + name);
            if (tex == null) { s_icons[name] = null; return null; }
            tex.filterMode = FilterMode.Bilinear;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            s_icons[name] = s;
            return s;
        }

        // ------------------------------------------------------------------ sprites

        private static Sprite MakeWhite()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color32[16];
            for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite MakeRounded(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            float c = (size - 1) / 2f, half = size / 2f - radius;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - c) - half + 0.5f, 0f);
                    float dy = Mathf.Max(Mathf.Abs(y - c) - half + 0.5f, 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(0.5f - d) * 255f));
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        // A rounded frame, drawn as a 9-slice: radius and line thickness in texture pixels
        private static Sprite MakeOutline(int size, int radius, int thickness)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            float c = (size - 1) / 2f, half = size / 2f - radius;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - c) - half + 0.5f, 0f);
                    float dy = Mathf.Max(Mathf.Abs(y - c) - half + 0.5f, 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - radius;      // negative inside
                    float a = Mathf.Clamp01(0.5f - d) * Mathf.Clamp01(d + thickness + 0.5f);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius + 2, radius + 2, radius + 2, radius + 2));
        }

        private static Sprite MakeGradient(bool darkAtTop)
        {
            var tex = new Texture2D(2, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < 64; y++)
            {
                float t = y / 63f;
                float a = darkAtTop ? t : 1f - t;
                a = a * a * 0.9f;
                for (int x = 0; x < 2; x++) tex.SetPixel(x, y, new Color(0, 0, 0, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, 64), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite MakeRing(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            float c = (size - 1) / 2f, rOut = size / 2f - 1f, rIn = rOut - size * 0.09f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - c, dy = y - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float edge = Mathf.Clamp01(rOut - d + 0.5f) * Mathf.Clamp01(d - rIn + 0.5f);
                    float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    float arc = Mathf.Clamp01((ang + 180f) / 270f);
                    float fade = (ang + 180f) < 270f ? Mathf.Lerp(0.15f, 1f, arc) : 0f;
                    tex.SetPixel(x, y, new Color(1, 1, 1, edge * fade));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // ------------------------------------------------------------------ builders

        public static RectTransform R(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        // fills the parent and runs past its edges, so a background also covers the area outside the safe area
        public static RectTransform Bleed(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-1200, -1200); rt.offsetMax = new Vector2(1200, 1200);
            return rt;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        // A filled box. radius 0 gives square corners.
        public static Image Box(Transform parent, Color color, float radius = 0f, string name = "Box")
        {
            var rt = R(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (radius > 0f)
            {
                img.sprite = s_round;
                img.type = Image.Type.Sliced;
                img.pixelsPerUnitMultiplier = 24f / radius;
            }
            else img.sprite = s_white;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string text, int size = 38, Font font = null, Color? color = null,
                                 TextAnchor anchor = TextAnchor.MiddleLeft, bool wrap = true, string name = "Label")
        {
            var rt = R(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font != null ? font : Regular;
            t.fontSize = size;
            t.color = color ?? Color.white;
            t.alignment = anchor;
            t.text = text;
            t.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        public static Image IconImage(Transform parent, string icon, Color color, float size, string name = "Icon")
        {
            var rt = R(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Icon(icon);
            img.color = color;
            img.preserveAspect = true;
            img.raycastTarget = false;
            Size(rt.gameObject, size, size);
            return img;
        }

        public static LayoutElement Size(GameObject go, float w = -1f, float h = -1f, float flexW = -1f, float flexH = -1f, float minW = -1f, float minH = -1f)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            if (w >= 0) le.preferredWidth = w;
            if (h >= 0) le.preferredHeight = h;
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (flexH >= 0) le.flexibleHeight = flexH;
            if (minW >= 0) le.minWidth = minW;
            if (minH >= 0) le.minHeight = minH;
            return le;
        }

        public static VerticalLayoutGroup VBox(Transform t, float spacing = 0f, int pad = 0, TextAnchor align = TextAnchor.UpperLeft, bool expandW = true)
        {
            var v = t.gameObject.GetComponent<VerticalLayoutGroup>();
            if (v == null) v = t.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(pad, pad, pad, pad);
            v.childAlignment = align;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = expandW; v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup HBox(Transform t, float spacing = 0f, int pad = 0, TextAnchor align = TextAnchor.MiddleLeft, bool expandH = false)
        {
            var h = t.gameObject.GetComponent<HorizontalLayoutGroup>();
            if (h == null) h = t.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = new RectOffset(pad, pad, pad, pad);
            h.childAlignment = align;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = expandH;
            return h;
        }

        public static ContentSizeFitter Fit(GameObject go, bool vertical = true, bool horizontal = false)
        {
            var f = go.GetComponent<ContentSizeFitter>();
            if (f == null) f = go.AddComponent<ContentSizeFitter>();
            f.verticalFit = vertical ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            f.horizontalFit = horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            return f;
        }

        public static RectTransform Spacer(Transform parent, float flex = 1f, float size = 0f)
        {
            var rt = R("Spacer", parent);
            Size(rt.gameObject, size, size, flex, flex);
            return rt;
        }

        public static Button Click(GameObject go, Graphic target, Action onClick)
        {
            if (target != null) target.raycastTarget = true;
            var b = go.GetComponent<Button>();
            if (b == null) b = go.AddComponent<Button>();
            b.targetGraphic = target;
            b.transition = Selectable.Transition.ColorTint;
            var cols = b.colors;
            cols.normalColor = Color.white;
            cols.highlightedColor = Color.white;
            cols.selectedColor = Color.white;
            cols.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            cols.fadeDuration = 0.06f;
            b.colors = cols;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            return b;
        }

        // Round or square icon button. filled: white disc with dark icon.
        // A horizontal slider from 0 to 1: a dark track, an accent fill and a round handle.
        public static UnityEngine.UI.Slider Slider(Transform parent, float value, Action<float> onChange, float height = 56f)
        {
            var root = R("Slider", parent);
            Size(root.gameObject, -1, height, 1);
            var bg = new GameObject("Track", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(root, false);
            var bgRt = (RectTransform)bg.transform;
            bgRt.anchorMin = new Vector2(0f, 0.5f); bgRt.anchorMax = new Vector2(1f, 0.5f); bgRt.sizeDelta = new Vector2(0f, 12f);
            bg.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.18f);
            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(root, false);
            var faRt = (RectTransform)fillArea.transform;
            faRt.anchorMin = new Vector2(0f, 0.5f); faRt.anchorMax = new Vector2(1f, 0.5f); faRt.sizeDelta = new Vector2(-28f, 12f);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            fill.GetComponent<Image>().color = Accent;
            var fillRt = (RectTransform)fill.transform;
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(0f, 1f); fillRt.sizeDelta = new Vector2(14f, 0f);
            var handleArea = new GameObject("HandleArea", typeof(RectTransform));
            handleArea.transform.SetParent(root, false);
            var haRt = (RectTransform)handleArea.transform;
            haRt.anchorMin = new Vector2(0f, 0.5f); haRt.anchorMax = new Vector2(1f, 0.5f); haRt.sizeDelta = new Vector2(-28f, 44f);
            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleImage = handle.GetComponent<Image>();
            handleImage.color = Color.white;
            ((RectTransform)handle.transform).sizeDelta = new Vector2(44f, 0f);
            var slider = root.gameObject.AddComponent<UnityEngine.UI.Slider>();
            slider.fillRect = fillRt; slider.handleRect = (RectTransform)handle.transform; slider.targetGraphic = handleImage;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight; slider.minValue = 0f; slider.maxValue = 1f;
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
            slider.onValueChanged.AddListener(v => { if (onChange != null) onChange(v); });
            return slider;
        }

        public static Button IconButton(Transform parent, string icon, float size, Action onClick, bool filled = false, bool glass = true, string name = "IconButton")
        {
            var bg = Box(parent, filled ? Color.white : (glass ? Glass : new Color(1, 1, 1, 0.14f)), size / 2f, name);
            Size(bg.gameObject, size, size);
            IconImage(bg.transform, icon, filled ? Bg : Color.white, size * 0.52f);
            var ic = bg.transform.GetChild(0) as RectTransform;
            ic.anchorMin = ic.anchorMax = new Vector2(0.5f, 0.5f);
            ic.sizeDelta = new Vector2(size * 0.52f, size * 0.52f);
            ic.anchoredPosition = Vector2.zero;
            ic.gameObject.GetComponent<LayoutElement>().ignoreLayout = true;
            return Click(bg.gameObject, bg, onClick);
        }

        public enum BtnStyle { Primary, Outline, Ghost, Danger }

        public static Button Pill(Transform parent, string text, Action onClick, BtnStyle style = BtnStyle.Primary, string icon = null, int fontSize = 36, float height = 104)
        {
            Color bgc = style == BtnStyle.Primary ? Accent : (style == BtnStyle.Ghost ? new Color(1, 1, 1, 0.12f) : (style == BtnStyle.Danger ? new Color(1f, 0.42f, 0.42f, 0.18f) : new Color(0, 0, 0, 0)));
            Color fg = style == BtnStyle.Primary ? OnAccent : (style == BtnStyle.Danger ? Bad : (style == BtnStyle.Outline ? Accent : Color.white));
            var bg = Box(parent, bgc, height / 2f, "Pill");
            Size(bg.gameObject, -1, height, 0);
            HBox(bg.transform, 14, 0, TextAnchor.MiddleCenter);
            bg.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(44, 44, 0, 0);
            if (style == BtnStyle.Outline)
            {
                var o = Box(bg.transform, Accent, height / 2f, "Outline");
                o.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                Stretch((RectTransform)o.transform);
                var inner = Box(o.transform, Bg, height / 2f - 3f, "Fill");
                Stretch((RectTransform)inner.transform);
                ((RectTransform)inner.transform).offsetMin = new Vector2(3, 3);
                ((RectTransform)inner.transform).offsetMax = new Vector2(-3, -3);
                inner.pixelsPerUnitMultiplier = 24f / Mathf.Max(4f, height / 2f - 3f);
                o.transform.SetAsFirstSibling();
            }
            if (icon != null) IconImage(bg.transform, icon, fg, fontSize * 1.1f);
            var t = Label(bg.transform, text, fontSize, Medium, fg, TextAnchor.MiddleCenter, false);
            return Click(bg.gameObject, bg, onClick);
        }

        public static Image Chip(Transform parent, string text, Color color, bool filled = false, int fontSize = 26)
        {
            var bg = Box(parent, filled ? color : new Color(color.r, color.g, color.b, 0.14f), 20f, "Chip");
            HBox(bg.transform, 8, 0, TextAnchor.MiddleCenter);
            bg.GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(20, 20, 8, 8);
            Label(bg.transform, text, fontSize, Medium, filled ? OnAccent : color, TextAnchor.MiddleCenter, false);
            Size(bg.gameObject, -1, -1, 0);
            return bg;
        }

        public static RectTransform ScrollV(Transform parent, out ScrollRect scroll, float spacing = 24f, int pad = 0, string name = "Scroll")
        {
            var root = R(name, parent);
            scroll = root.gameObject.AddComponent<ScrollRect>();
            var vp = R("Viewport", root);
            Stretch(vp);
            vp.gameObject.AddComponent<RectMask2D>();
            var content = R("Content", vp);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            VBox(content, spacing, pad);
            Fit(content.gameObject);
            scroll.viewport = vp; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
            scroll.decelerationRate = 0.12f;
            var img = root.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);       // gives the scroll area a surface to catch drags
            return content;
        }

        public static RectTransform ScrollH(Transform parent, out ScrollRect scroll, float spacing = 24f, int pad = 0, float height = 400f, string name = "Row")
        {
            var root = R(name, parent);
            Size(root.gameObject, -1, height, 1);
            scroll = root.gameObject.AddComponent<ScrollRect>();
            var vp = R("Viewport", root);
            Stretch(vp);
            vp.gameObject.AddComponent<RectMask2D>();
            var content = R("Content", vp);
            content.anchorMin = new Vector2(0, 0); content.anchorMax = new Vector2(0, 1); content.pivot = new Vector2(0, 0.5f);
            content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            var h = HBox(content, spacing, 0, TextAnchor.UpperLeft, true);
            h.padding = new RectOffset(pad, pad, 0, 0);
            Fit(content.gameObject, false, true);
            scroll.viewport = vp; scroll.content = content;
            scroll.horizontal = true; scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.decelerationRate = 0.12f;
            var img = root.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            return content;
        }

        // Shows a texture so that it covers the box, cropping the long side.
        public static RawImage Cover(RawImage img, Texture tex, float boxAspect)
        {
            img.texture = tex;
            img.color = Color.white;
            if (tex == null) return img;
            float ta = (float)tex.width / Mathf.Max(1, tex.height);
            if (ta > boxAspect) { float w = boxAspect / ta; img.uvRect = new Rect((1f - w) / 2f, 0, w, 1); }
            else { float h = ta / boxAspect; img.uvRect = new Rect(0, (1f - h) / 2f, 1, h); }
            return img;
        }

        public static void Fade(CanvasGroup g, float target, float speed = 10f)
        {
            if (g == null) return;
            g.alpha = Mathf.MoveTowards(g.alpha, target, Time.unscaledDeltaTime * speed);
            bool on = g.alpha > 0.5f && target > 0.5f;
            g.blocksRaycasts = on; g.interactable = on;
        }

        public static void Show(CanvasGroup g, bool on)
        {
            if (g == null) return;
            g.alpha = on ? 1f : 0f; g.blocksRaycasts = on; g.interactable = on;
        }

        public static CanvasGroup Group(RectTransform rt, bool visible = true)
        {
            var g = rt.gameObject.GetComponent<CanvasGroup>();
            if (g == null) g = rt.gameObject.AddComponent<CanvasGroup>();
            Show(g, visible);
            return g;
        }

        public static FlowLayout Flow(Transform parent, float spacingX = 10f, float spacingY = 10f)
        {
            var rt = R("Flow", parent);
            var f = rt.gameObject.AddComponent<FlowLayout>();
            f.spacingX = spacingX; f.spacingY = spacingY;
            return f;
        }

        public static string Plural(int n, string one, string many) { return n + " " + (n == 1 ? one : many); }

        public static void Copy(string text) { GUIUtility.systemCopyBuffer = text; }
    }

    // Children left to right, wrapping to a new row when the width is used up.
    public class FlowLayout : LayoutGroup
    {
        public float spacingX = 10f, spacingY = 10f;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            float min = 0f;
            for (int i = 0; i < rectChildren.Count; i++) min = Mathf.Max(min, LayoutUtility.GetPreferredWidth(rectChildren[i]));
            SetLayoutInputForAxis(min + padding.horizontal, min + padding.horizontal, 1f, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            float h = Arrange(false, 0);
            SetLayoutInputForAxis(h, h, -1f, 1);
        }

        public override void SetLayoutHorizontal() { Arrange(true, 0); }
        public override void SetLayoutVertical() { Arrange(true, 1); }

        private float Arrange(bool apply, int axis)
        {
            float width = rectTransform.rect.width - padding.horizontal;
            if (width <= 1f) width = 100000f;
            float x = 0f, y = 0f, rowH = 0f;
            for (int i = 0; i < rectChildren.Count; i++)
            {
                var c = rectChildren[i];
                float w = LayoutUtility.GetPreferredWidth(c), h = LayoutUtility.GetPreferredHeight(c);
                if (x > 0f && x + w > width) { x = 0f; y += rowH + spacingY; rowH = 0f; }
                if (apply)
                {
                    if (axis == 0) SetChildAlongAxis(c, 0, padding.left + x, w);
                    else SetChildAlongAxis(c, 1, padding.top + y, h);
                }
                x += w + spacingX;
                rowH = Mathf.Max(rowH, h);
            }
            return y + rowH + padding.vertical;
        }
    }
}
