using System;
using System.Collections.Generic;
using System.Linq;
using JALib.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace JALib.Tools;

public class JANotification(JAMod mod, string title, string message, JANotification.IconType icon) {
    private static Canvas _canvas;
    private static List<JANotification> _notifications = [];
    private static int _index;

    internal static void Initialize() {
        GameObject gameObject = new("JANotification");
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 12346;
        CanvasScaler canvasScaler = gameObject.AddComponent<CanvasScaler>();
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        _canvas = canvas;
        gameObject.AddComponent<GraphicRaycaster>();
        Object.DontDestroyOnLoad(gameObject);
    }

    internal static void UnloadNotify(JAMod mod) {
        for(int i = 0; i < _notifications.Count; i++) {
            if(_notifications[i].Mod != mod) continue;
            _notifications[i].Hide();
            i--;
        }
    }

    private static void UpdateLocation() {

    }

    public readonly JAMod Mod = mod;
    private string _title = title;
    private string _message = message;
    private IconType _icon = icon;
    private Selection[] _selections;
    private RectTransform _transform;
    private TextMeshPro _titleText;
    private TextMeshPro _messageText;
    private Image _iconImage;

    public string Message {
        get => _message;
        set {
            _message = value;
            UpdateDesign();
        }
    }

    public string Title {
        get => _title;
        set {
            _title = value;
            UpdateDesign();
        }
    }

    public IconType Icon {
        get => _icon;
        set {
            _icon = value;
            UpdateDesign();
        }
    }

    public Selection[] Selections {
        get => _selections;
        set {
            lock(this) {
                foreach(Selection selection in _selections) selection._notification = null;
                _selections = value;
                foreach(Selection selection in value) selection._notification = this;
            }
            UpdateDesign();
        }
    }

    public bool Active {
        get => _notifications.Contains(this);
        set {
            if(value) Show();
            else Hide();
        }
    }

    public JANotification(JAMod mod, string title, string message) : this(mod, title, message, IconType.None) {
    }

    public JANotification(JAMod mod, string message) : this(mod, null, message) {
    }

    public void Show() {
        if(Active) return;
        _notifications.Add(this);
        GameObject gameObject = new("Notification-" + _index++);
        RectTransform transform = _transform = gameObject.AddComponent<RectTransform>();
        transform.SetParent(_canvas.transform);
        transform.anchorMin = transform.anchorMax = transform.pivot = Vector2.one;
        transform.anchoredPosition = Vector2.zero;
        transform.SizeDeltaX(300);
        GameObject obj = new("Background");
        RectTransform trans = obj.AddComponent<RectTransform>();
        trans.SetParent(transform);
        trans.sizeDelta = new Vector2(300, 1080);
        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.235f, 0.247f, 0.255f, 0.8f);
        obj = new GameObject("Icon");
        trans = obj.AddComponent<RectTransform>();
        trans.SetParent(transform);
        trans.sizeDelta = new Vector2(50, 50);
        _iconImage = obj.AddComponent<Image>();
        obj = new GameObject("Title");
        trans = obj.AddComponent<RectTransform>();
        trans.SetParent(transform);
        _titleText = obj.AddComponent<TextMeshPro>();
        obj = new GameObject("Message");
        trans = obj.AddComponent<RectTransform>();
        trans.SetParent(transform);
        _messageText = obj.AddComponent<TextMeshPro>();
        UpdateDesign();
    }

    public void UpdateDesign() {
        if(!Active) return;
        _titleText.text = _title;
        _messageText.text = _message;
        LayoutRebuilder.ForceRebuildLayoutImmediate(_transform);
        UpdateLocation();
    }

    public void Hide() {
        if(!Active) return;
        _notifications.Remove(this);
        UpdateDesign();
    }

    public void AddSelection(Selection selection) {
        lock(this) {
            Selection[] selections = new Selection[_selections.Length + 1];
            Array.Copy(_selections, selections, _selections.Length);
            selections[^1] = selection;
            Selections = selections;
            selection._notification = this;
        }
    }

    public void AddSelection(params Selection[] selections) {
        lock(this) {
            _selections = _selections.Concat(selections).ToArray();
        }
    }

    public enum IconType {
        None,
        Info,
        Warning,
        Error
    }

    public class Selection(string text, Action callback) {
        internal JANotification _notification;
        public readonly string Text = text;
        public readonly Action Callback = callback;
        private bool _clickable = true;

        public bool Clickable {
            get => _clickable;
            set {
                _clickable = value;
                _notification?.UpdateDesign();
            }
        }
    }
}