using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FlipLogic.Core;
using FlipLogic.Data;

namespace FlipLogic.Tutorial
{
    /// <summary>
    /// チュートリアルデモのセットアップと進行管理。
    /// ゲーム開始時にUIを動的生成し、チュートリアルメッセージを表示する。
    /// </summary>
    public class TutorialSetup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameEntity _playerEntity;
        [SerializeField] private GameEntity _enemyEntity;

        [Header("Tutorial Settings")]
        [SerializeField] private bool _showTutorial = true;

        private Canvas _canvas;
        private GameObject _tutorialPanel;
        private Text _tutorialText;
        private int _tutorialStep;

        private readonly List<string> _messages = new List<string>
        {
            "『世界論理改変RPG: Flip Logic』\nチュートリアルへようこそ。",
            "矢印キー（またはWASD）で移動できます。\n1マス歩くたびに「1ターン」が進みます。",
            "近くにいる氷スライムに近づいてみましょう。\n隣接するとバトルが始まります。",
            "バトルでは「たたかう」で攻撃できますが…\n氷スライムは防御力が高い敵です。",
            "「ルールブック」を開いてみましょう。\nこの世界のルールを「書き換える」ことで、\n戦況を有利に変化させられます！",
        };

        private void Start()
        {
            SetupEntityVisuals();

            if (_showTutorial)
                StartCoroutine(BeginTutorial());
        }

        private void SetupEntityVisuals()
        {
            SetupSprite(_playerEntity, new Color(0.2f, 0.6f, 1.0f), 0.8f);
            SetupSprite(_enemyEntity, new Color(0.5f, 0.9f, 1.0f), 0.7f);

            if (_enemyEntity != null)
                _enemyEntity.Tags.AddTag(new TagDefinition("Element", "Ice", -1, "Nature"));
        }

        private void SetupSprite(GameEntity entity, Color color, float scale)
        {
            if (entity == null) return;
            var sr = entity.GetComponent<SpriteRenderer>();
            if (sr == null) return;
            if (sr.sprite == null)
            {
                sr.sprite = MakeWhiteSprite();
                sr.color = color;
                sr.sortingOrder = 5;
                entity.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private IEnumerator BeginTutorial()
        {
            yield return null;
            _tutorialStep = 0;
            EnsureCanvas();
            BuildTutorialPanel();
            ShowStep(0);
        }

        private void ShowStep(int idx)
        {
            if (idx >= _messages.Count)
            {
                _tutorialPanel.SetActive(false);
                return;
            }
            _tutorialPanel.SetActive(true);
            _tutorialText.text = _messages[idx];
        }

        private void NextStep()
        {
            _tutorialStep++;
            ShowStep(_tutorialStep);
        }

        private void Update()
        {
            if (_tutorialPanel != null && _tutorialPanel.activeSelf)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    NextStep();
                }
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

            // EventSystemがなければ追加
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        private void BuildTutorialPanel()
        {
            _tutorialPanel = new GameObject("TutorialPanel");
            _tutorialPanel.transform.SetParent(_canvas.transform, false);

            var rect = _tutorialPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.02f);
            rect.anchorMax = new Vector2(0.9f, 0.3f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = _tutorialPanel.AddComponent<Image>();
            img.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);

            // テキスト
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(_tutorialPanel.transform, false);
            _tutorialText = textGo.AddComponent<Text>();
            _tutorialText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _tutorialText.fontSize = 18;
            _tutorialText.color = Color.white;
            _tutorialText.alignment = TextAnchor.MiddleCenter;
            _tutorialText.lineSpacing = 1.3f;
            var tRect = textGo.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.05f, 0.15f);
            tRect.anchorMax = new Vector2(0.95f, 0.95f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            // タップ領域（パネル全体）はUpdateで判定するため削除

            // ▼マーク
            var arrowGo = new GameObject("Arrow");
            arrowGo.transform.SetParent(_tutorialPanel.transform, false);
            var arrowTxt = arrowGo.AddComponent<Text>();
            arrowTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            arrowTxt.text = "▼";
            arrowTxt.fontSize = 14;
            arrowTxt.color = new Color(0.6f, 0.9f, 1f);
            arrowTxt.alignment = TextAnchor.LowerRight;
            var aRect = arrowGo.GetComponent<RectTransform>();
            aRect.anchorMin = new Vector2(0.85f, 0.02f);
            aRect.anchorMax = new Vector2(0.98f, 0.15f);
            aRect.offsetMin = Vector2.zero;
            aRect.offsetMax = Vector2.zero;
        }

        private static Sprite MakeWhiteSprite()
        {
            var tex = new Texture2D(4, 4);
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }
    }
}
