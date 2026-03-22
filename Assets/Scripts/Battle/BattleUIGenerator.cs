using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FlipLogic.Core;
using FlipLogic.Data;

namespace FlipLogic.Battle
{
    /// <summary>
    /// バトルUIの動的生成。Canvas上にコマンドメニュー、HP表示、メッセージウィンドウを作成する。
    /// </summary>
    public class BattleUIGenerator : MonoBehaviour
    {
        private Canvas _canvas;
        private BattleUIController _uiController;

        [SerializeField] private UITheme _theme;
        private UITheme Theme => _theme != null ? _theme : (_theme = ScriptableObject.CreateInstance<UITheme>());

        public BattleUIController UIController => _uiController;

        private void Start()
        {
#if UNITY_EDITOR
            if (_theme == null)
            {
                _theme = UnityEditor.AssetDatabase.LoadAssetAtPath<UITheme>("Assets/Data/Visuals/DefaultUITheme.asset");
            }
#endif
            EnsureCanvas();
            EnsureEventSystem();
            GenerateBattleUI();

            // BattleManagerに接続
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.SetUIController(_uiController);
            }
        }

        private void EnsureCanvas()
        {
            _canvas = FindAnyObjectByType<Canvas>();
            if (_canvas != null) return;

            var go = new GameObject("UICanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 540);
            go.AddComponent<GraphicRaycaster>();
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private void GenerateBattleUI()
        {
            var root = new GameObject("BattleUI");
            root.transform.SetParent(_canvas.transform, false);
            _uiController = root.AddComponent<BattleUIController>();
            root.SetActive(false);

            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // --- メッセージパネル ---
            var msgPanel = CreatePanel(root.transform, "MessagePanel",
                new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.28f),
                Theme.MessagePanelColor);
            var msgText = CreateText(msgPanel.transform, "MessageText",
                new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f),
                "", 16, Theme.DefaultTextColor);

            // --- コマンドパネル ---
            var cmdPanel = CreatePanel(root.transform, "CommandPanel",
                new Vector2(0.6f, 0.32f), new Vector2(0.95f, 0.7f),
                Theme.CommandPanelColor);
            cmdPanel.SetActive(false);

            var attackBtn = CreateButton(cmdPanel.transform, "AttackBtn", "たたかう",
                new Vector2(0.05f, 0.75f), new Vector2(0.95f, 0.95f),
                Theme.AttackButtonColor);
            var defendBtn = CreateButton(cmdPanel.transform, "DefendBtn", "ぼうぎょ",
                new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.60f),
                Theme.DefendButtonColor);
            var fleeBtn = CreateButton(cmdPanel.transform, "FleeBtn", "にげる",
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.25f),
                Theme.FleeButtonColor);

            // --- HP表示 ---
            var hpPanel = CreatePanel(root.transform, "HPPanel",
                new Vector2(0.05f, 0.72f), new Vector2(0.55f, 0.98f),
                Theme.HpPanelColor);

            var enemyNameText = CreateText(hpPanel.transform, "EnemyName",
                new Vector2(0.05f, 0.55f), new Vector2(0.95f, 0.95f),
                "敵", 18, Theme.EnemyNameTextColor);
            var enemyHpText = CreateText(hpPanel.transform, "EnemyHP",
                new Vector2(0.55f, 0.55f), new Vector2(0.95f, 0.95f),
                "HP: 0/0", 14, Theme.EnemyHpTextColor);
            enemyHpText.alignment = TextAnchor.MiddleRight;

            var playerHpText = CreateText(hpPanel.transform, "PlayerHP",
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.45f),
                "HP: 0/0", 16, Theme.PlayerHpTextColor);

            // UIControllerにSerializeFieldを設定（Reflectionで直接）
            SetPrivateField(_uiController, "_messageText", msgText);
            SetPrivateField(_uiController, "_messagePanel", msgPanel);
            SetPrivateField(_uiController, "_commandPanel", cmdPanel);
            SetPrivateField(_uiController, "_attackButton", attackBtn.GetComponent<Button>());
            SetPrivateField(_uiController, "_defendButton", defendBtn.GetComponent<Button>());
            SetPrivateField(_uiController, "_fleeButton", fleeBtn.GetComponent<Button>());
            SetPrivateField(_uiController, "_playerHpText", playerHpText);
            SetPrivateField(_uiController, "_enemyHpText", enemyHpText);
            SetPrivateField(_uiController, "_enemyNameText", enemyNameText);
        }

        private Text FindChildText(GameObject parent, string name)
        {
            var t = parent.transform.Find(name);
            return t != null ? t.GetComponent<Text>() : null;
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(obj, value);
        }

        // --- UI生成ヘルパー ---

        private GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string content, int fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = go.AddComponent<Text>();
            text.font = Theme.GetSafeFont();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            return text;
        }

        private GameObject CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(bgColor.r * 1.2f, bgColor.g * 1.2f, bgColor.b * 1.2f);
            colors.pressedColor = new Color(bgColor.r * 0.8f, bgColor.g * 0.8f, bgColor.b * 0.8f);
            btn.colors = colors;

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = Theme.GetSafeFont();
            text.text = label;
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 10;
            text.resizeTextMaxSize = 20;
            var tRect = textGo.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            return go;
        }
    }
}
