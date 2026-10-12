using System.Collections.Generic;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimTwitch.Portraits
{
    // Viewer picker for a placed portrait, built with Jotunn's GUIManager
    // (the same widgets XPortal uses): woodpanel, Valheim fonts and colours,
    // scroll view, buttons. Opened with E on the piece (Portrait.Interact),
    // clicking a viewer assigns it to that piece via its ZDO. While open,
    // GUIManager.BlockInput stops player input and the mouse look.
    internal class PortraitPicker : MonoBehaviour
    {
        private const float CloseDistance = 12f;
        private const float PanelWidth = 500f;
        private const float PanelHeight = 520f;
        private const float RowHeight = 44f;

        private static PortraitPicker _instance;

        private Portrait _target;
        private GameObject _panel;
        private ScrollRect _scroll;
        private readonly List<GameObject> _rows = new List<GameObject>();

        internal static bool IsOpen => _instance != null && _instance._target != null;

        internal static PortraitPicker Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("VTPortraitPicker");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<PortraitPicker>();
                }
                return _instance;
            }
        }

        internal void Open(Portrait target)
        {
            if (GUIManager.IsHeadless())
            {
                return;
            }
            _target = target;
            BuildIfNeeded();
            RebuildRows();
            _panel.SetActive(true);
            GUIManager.BlockInput(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void Close()
        {
            _target = null;
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
            GUIManager.BlockInput(false);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            if (_target == null)
            {
                return;
            }
            if (_target == null || Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
                return;
            }
            Player player = Player.m_localPlayer;
            if (player == null ||
                Vector3.Distance(player.transform.position, _target.transform.position) > CloseDistance)
            {
                Close();
            }
        }

        // ---- construction with Jotunn's widgets ----

        private void BuildIfNeeded()
        {
            if (_panel != null)
            {
                return;
            }

            GUIManager gui = GUIManager.Instance;
            Transform parent = GUIManager.CustomGUIFront.transform;

            _panel = gui.CreateWoodpanel(
                parent: parent,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: Vector2.zero,
                width: PanelWidth,
                height: PanelHeight,
                draggable: false);
            _panel.name = "VTPortraitPickerPanel";
            _panel.AddComponent<CanvasGroup>();
            _panel.AddComponent<UIGroupHandler>().m_groupPriority = 10000;

            var header = gui.CreateText(
                text: "VT Portrait",
                parent: _panel.transform,
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(0f, 0f),
                position: Vector2.zero,
                font: gui.AveriaSerifBold,
                fontSize: 30,
                color: gui.ValheimOrange,
                outline: true,
                outlineColor: Color.black,
                width: PanelWidth - 40f,
                height: 40f,
                addContentSizeFitter: false);
            header.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            Place(header, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(20f, -70f), new Vector2(-20f, -30f));

            var subtitle = gui.CreateText(
                text: "Choose the viewer shown on this portrait",
                parent: _panel.transform,
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(0f, 0f),
                position: Vector2.zero,
                font: gui.AveriaSerif,
                fontSize: 16,
                color: Color.white,
                outline: false,
                outlineColor: Color.black,
                width: PanelWidth - 40f,
                height: 26f,
                addContentSizeFitter: false);
            subtitle.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            Place(subtitle, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(20f, -100f), new Vector2(-20f, -74f));
            GameObject scrollGo = gui.CreateScrollView(
                parent: _panel.transform,
                showHorizontalScrollbar: false,
                showVerticalScrollbar: true,
                handleSize: 10f,
                handleDistanceToBorder: 6f,
                handleColors: ColorBlock.defaultColorBlock,
                slidingAreaBackgroundColor: new Color(0f, 0f, 0f, 0.25f),
                width: PanelWidth - 60f,
                height: PanelHeight - 200f);
            Place(scrollGo, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(30f, 92f), new Vector2(-30f, -112f));
            _scroll = scrollGo.GetComponentInChildren<ScrollRect>();

            GameObject blank = gui.CreateButton(
                text: "Blank",
                parent: _panel.transform,
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(0f, 0f),
                position: Vector2.zero,
                width: 120f,
                height: 46f);
            Place(blank, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-270f, 30f), new Vector2(-150f, 76f));
            blank.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (_target != null)
                {
                    _target.AssignViewer("", "");
                }
                Close();
            });

            GameObject close = gui.CreateButton(
                text: "Close",
                parent: _panel.transform,
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(0f, 0f),
                position: Vector2.zero,
                width: 120f,
                height: 46f);
            Place(close, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-140f, 30f), new Vector2(-20f, 76f));
            close.GetComponent<Button>().onClick.AddListener(Close);

            var hint = gui.CreateText(
                text: "[Esc] Close",
                parent: _panel.transform,
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(0f, 0f),
                position: Vector2.zero,
                font: gui.AveriaSerif,
                fontSize: 14,
                color: Color.white,
                outline: false,
                outlineColor: Color.black,
                width: 200f,
                height: 24f,
                addContentSizeFitter: false);
            hint.GetComponent<Text>().alignment = TextAnchor.MiddleLeft;
            Place(hint, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(24f, 24f), new Vector2(224f, 48f));
        }

        // Positions a UI element by explicit anchors, pivot and offsets. Jotunn's
        // helpers place elements inconsistently, so every element is set here.
        private static void Place(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        private void RebuildRows()
        {
            foreach (GameObject row in _rows)
            {
                Destroy(row);
            }
            _rows.Clear();

            GUIManager gui = GUIManager.Instance;
            RectTransform content = _scroll.content;
            List<PortraitManager.PortraitData> roster = PortraitManager.Instance?.GetRoster() ?? new List<PortraitManager.PortraitData>();

            // Standard vertical layout: rows are stacked by the layout group,
            // so their positions no longer depend on hand-computed offsets.
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>() ?? content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 6f;
            layout.padding = new RectOffset(8, 8, 6, 6);
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>() ?? content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            foreach (PortraitManager.PortraitData viewer in roster)
            {
                string name = viewer.Name;
                string url = viewer.Url;
                string label = _target != null && name == _target.ViewerName ? name + "  (shown)" : name;

                GameObject row = gui.CreateButton(
                    text: label,
                    parent: content,
                    anchorMin: new Vector2(0f, 1f),
                    anchorMax: new Vector2(1f, 1f),
                    position: Vector2.zero,
                    width: PanelWidth - 90f,
                    height: RowHeight - 6f);
                row.name = "VTPortraitRow_" + name;
                row.SetActive(true);
                var element = row.GetComponent<LayoutElement>() ?? row.AddComponent<LayoutElement>();
                element.minHeight = RowHeight - 6f;
                element.preferredHeight = RowHeight - 6f;
                row.GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (_target != null)
                    {
                        _target.AssignViewer(name, url);
                    }
                    Close();
                });
                _rows.Add(row);
            }

            if (roster.Count == 0)
            {
                GameObject empty = gui.CreateText(
                    text: "No viewers registered yet.\nSend a viewer_portrait command from Streamer.bot.",
                    parent: content,
                    anchorMin: new Vector2(0f, 1f),
                    anchorMax: new Vector2(1f, 1f),
                    position: Vector2.zero,
                    font: gui.AveriaSerif,
                    fontSize: 16,
                    color: Color.white,
                    outline: false,
                    outlineColor: Color.black,
                    width: PanelWidth - 120f,
                    height: 60f,
                    addContentSizeFitter: false).gameObject;
                _rows.Add(empty);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            _scroll.verticalNormalizedPosition = 1f;
            ValheimTwitchPlugin.Log.LogInfo(
                $"[Portraits] Picker list: {roster.Count} viewer row(s), content height {content.rect.height:F0}.");
        }
    }
}

