using UnityEngine;
using UnityEngine.UI;
using FlipLogic.Core;
using FlipLogic.Data;
using FlipLogic.UI;
using System.Reflection;

namespace FlipLogic.Explore
{
    /// <summary>
    /// ルールボードUIを動的に生成するジェネレーター。
    /// Canvas上にパネル、スクロールビュー、閉じるボタン等を構築し、RuleBoardUIControllerをセットアップする。
    /// </summary>
    public class RuleBoardUIGenerator : MonoBehaviour
    {
        private Canvas _canvas;
        private RuleBoardUIController _uiController;

        [SerializeField] private UITheme _theme;
        private UITheme Theme => _theme != null ? _theme : (_theme = ScriptableObject.CreateInstance<UITheme>());

        private void Start()
        {
#if UNITY_EDITOR
            if (_theme == null)
            {
                _theme = UnityEditor.AssetDatabase.LoadAssetAtPath<UITheme>("Assets/Data/Visuals/DefaultUITheme.asset");
            }
#endif
            EnsureCanvas();
            GenerateRuleBoardUI();
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
            if (go.GetComponent<GraphicRaycaster>() == null)
            {
                go.AddComponent<GraphicRaycaster>();
            }

            EnsureEventSystem();
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        private void GenerateRuleBoardUI()
        {
            var root = new GameObject("RuleBoardUI");
            root.transform.SetParent(_canvas.transform, false);
            _uiController = root.AddComponent<RuleBoardUIController>();

            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // --- 背景パネル ---
            var boardPanel = CreatePanel(root.transform, "BoardPanel",
                new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f),
                new Color(0.1f, 0.1f, 0.15f, 0.95f));
            
            // タイトル
            var title = CreateText(boardPanel.transform, "Title",
                new Vector2(0f, 0.9f), new Vector2(1f, 1f),
                "現在のルール / World Rules", 20, Color.white);
            title.fontStyle = FontStyle.Bold;

            // 閉じるボタン
            var closeBtn = CreateButton(boardPanel.transform, "CloseBtn", "とじる [X]",
                new Vector2(0.85f, 0.92f), new Vector2(0.98f, 0.98f),
                new Color(0.5f, 0.2f, 0.2f));
            closeBtn.GetComponent<Button>().onClick.AddListener(() => _uiController.CloseBoard());

            // --- スクロールエリア（簡易） ---
            var scrollArea = CreatePanel(boardPanel.transform, "ScrollArea",
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.85f),
                new Color(0, 0, 0, 0.3f));
            
            // Content Root (Vertical Layout)
            var contentRoot = new GameObject("Content");
            contentRoot.transform.SetParent(scrollArea.transform, false);
            var contentRect = contentRoot.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = new Vector2(10, -500); // 暫定
            contentRect.offsetMax = new Vector2(-10, 0);
            
            var vlg = contentRoot.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 10;
            vlg.padding = new RectOffset(10, 10, 10, 10);

            var csf = contentRoot.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // --- RuleRowUI プレハブの動的構築 ---
            var rowPrefab = CreateRuleRowUIPrototype();

            // Controllerに参照を注入
            SetPrivateField(_uiController, "_boardPanel", boardPanel);
            SetPrivateField(_uiController, "_contentRoot", contentRoot.transform);
            SetPrivateField(_uiController, "_rowPrefab", rowPrefab);

            root.SetActive(false); // 初期状態は非表示
        }

        private RuleRowUI CreateRuleRowUIPrototype()
        {
            var go = new GameObject("RuleRow_Prototype");
            go.transform.SetParent(this.transform, false); // 非表示で保持
            go.SetActive(false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 60);

            var rowImg = go.AddComponent<Image>();
            rowImg.color = new Color(1, 1, 1, 0.1f);

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;
            hlg.spacing = 20;

            var rowUI = go.AddComponent<RuleRowUI>();

            // Condition Block
            var condBlock = CreatePropositionBlock(go.transform, "ConditionBlock");
            // Connector Text
            var connector = CreateText(go.transform, "Connector", Vector2.zero, Vector2.zero, "ならば", 14, Color.gray);
            connector.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 40);
            // Result Block
            var resBlock = CreatePropositionBlock(go.transform, "ResultBlock");
            // Swap Button
            var swapBtn = CreateButton(go.transform, "SwapBtn", "入替", Vector2.zero, Vector2.zero, new Color(0.3f, 0.3f, 0.6f));
            swapBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 40);

            // Refで登録
            SetPrivateField(rowUI, "_conditionBlock", condBlock);
            SetPrivateField(rowUI, "_resultBlock", resBlock);
            SetPrivateField(rowUI, "_connectorText", connector);
            SetPrivateField(rowUI, "_swapButton", swapBtn.GetComponent<Button>());

            return rowUI;
        }

        private PropositionBlock CreatePropositionBlock(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(250, 50);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.25f);

            var block = go.AddComponent<PropositionBlock>();

            // Content Text
            var text = CreateText(go.transform, "Content", new Vector2(0.05f, 0f), new Vector2(0.75f, 1f), "---", 14, Color.white);
            text.alignment = TextAnchor.MiddleLeft;

            // Negate Button
            var negBtnGO = CreateButton(go.transform, "NegateBtn", "否定", new Vector2(0.75f, 0.1f), new Vector2(0.95f, 0.9f), new Color(0.4f, 0.4f, 0.4f));
            var negBtnText = negBtnGO.transform.Find("Label").GetComponent<Text>();

            SetPrivateField(block, "_contentText", text);
            SetPrivateField(block, "_negateButton", negBtnGO.GetComponent<Button>());
            SetPrivateField(block, "_negateButtonText", negBtnText);
            SetPrivateField(block, "_blockBackground", img);

            return block;
        }

        // --- UI生成ヘルパー (BattleUIGeneratorと類似) ---

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
            img.raycastTarget = false; // パネル自体はクリックを遮らない（子が処理する）
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
            text.raycastTarget = false; // テキストはクリックを遮らない
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
            
            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = Theme.GetSafeFont();
            text.text = label;
            text.fontSize = 12;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            var tRect = textGo.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            return go;
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(obj, value);
        }
    }
}
