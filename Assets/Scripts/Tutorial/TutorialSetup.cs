using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using FlipLogic.Core;
using FlipLogic.Data;
using FlipLogic.Scenario;
using FlipLogic.Grid;

namespace FlipLogic.Tutorial
{
    /// <summary>
    /// チュートリアルのセットアップとシナリオ定義。
    /// ScenarioRunnerにステップを渡してイベント駆動で進行する。
    /// </summary>
    public class TutorialSetup : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _playerPrefab;
        [SerializeField] private EnemyData _iceSlimeData;

        [Header("Tutorial Settings")]
        [SerializeField] private Vector2Int _enemySpawnPos = new Vector2Int(5, 3);
        [SerializeField] private bool _showTutorial = true;
        [SerializeField] private Data.UITheme _uiTheme;

        private Data.UITheme Theme => _uiTheme != null ? _uiTheme : (_uiTheme = ScriptableObject.CreateInstance<Data.UITheme>());

        private Canvas _canvas;
        private GameObject _tutorialPanel;
        private Text _tutorialText;

        private void Start()
        {
#if UNITY_EDITOR
            if (_iceSlimeData == null)
            {
                _iceSlimeData = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/Enemies/IceSlime.asset");
            }
            if (_uiTheme == null)
            {
                _uiTheme = UnityEditor.AssetDatabase.LoadAssetAtPath<Data.UITheme>("Assets/Data/Visuals/DefaultUITheme.asset");
            }
            if (_playerPrefab == null)
            {
                _playerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            }
#endif

            // 古いアセット（EnemySymbolObj等）がシーンに残っていれば削除するセーフガード
            var oldEnemy = GameObject.Find("EnemySymbolObj");
            if (oldEnemy != null) Destroy(oldEnemy);

            SetupEntityVisuals();
            SetupTileOverlays();

            if (_showTutorial)
                StartCoroutine(BeginTutorial());
        }

        private void SetupEntityVisuals()
        {
            // プレイヤーを生成
            if (_playerPrefab != null)
            {
                var go = Instantiate(_playerPrefab, new Vector3(1f, 1f, 0f), Quaternion.identity);
                go.name = "PlayerObj";
                var entity = go.GetComponent<GameEntity>();
                
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = EntitySpriteFactory.CreateCircleWithLetter('P', new Color(0.2f, 0.55f, 1.0f), Color.white);
                    sr.sortingOrder = 5;
                    go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
                }

                // カメラにターゲットを設定
                if (CameraFollow.Instance != null)
                {
                    CameraFollow.Instance.Target = go.transform;
                }
            }

            // 敵 (IceSlime) を EntityFactory 経由で生成
            if (_iceSlimeData != null)
            {
                EntityFactory.CreateEnemy(_iceSlimeData, _enemySpawnPos);
            }

            // 火属性マスを設置（スライム巡回経路上）
            if (GridMap.Instance != null)
            {
                GridMap.Instance.AddCellTag(new Vector2Int(5, 5), new TagDefinition("Element", "Fire", -1, "Tutorial"));
            }

            // TagBehaviorRunnerがない場合は生成
            if (TagBehaviorRunner.Instance == null)
            {
                new GameObject("TagBehaviorRunner").AddComponent<TagBehaviorRunner>();
            }
        }

        private void SetupTileOverlays()
        {
            // TileOverlayRendererがない場合は生成
            if (TileOverlayRenderer.Instance == null)
            {
                new GameObject("TileOverlayRenderer").AddComponent<TileOverlayRenderer>();
            }

            // 少し遅延して全オーバーレイを構築（GridMap初期化待ち）
            StartCoroutine(DelayedOverlayBuild());
        }

        private IEnumerator DelayedOverlayBuild()
        {
            yield return null;
            yield return null;
            if (TileOverlayRenderer.Instance != null)
                TileOverlayRenderer.Instance.RebuildAll();
        }

        private IEnumerator BeginTutorial()
        {
            yield return null;
            yield return null; // GridMapやTileOverlay初期化待ち

            EnsureCanvas();
            BuildTutorialPanel();

            // ScenarioRunnerがなければ生成
            if (ScenarioRunner.Instance == null)
            {
                var go = new GameObject("ScenarioRunner");
                go.AddComponent<ScenarioRunner>();
            }

            // UI紐付け
            yield return null;
            var runner = ScenarioRunner.Instance;
            // リフレクションを避け、直接フィールドを設定する代わりにSerializeFieldを公開
            // → ScenarioRunnerのフィールドにUI要素を設定する必要がある
            // ここではworkaroundとしてSetUIメソッドを追加済みと想定
            SetScenarioRunnerUI(runner);

            // シナリオ定義
            var steps = BuildTutorialScenario();
            runner.StartScenario(steps);
        }

        private void SetScenarioRunnerUI(ScenarioRunner runner)
        {
            // ScenarioRunnerのSerializeFieldにはエディタからアクセスできないので、
            // RuntimeでUIを差し込むための公開メソッドを想定
            // → ScenarioRunnerにSetUI()を追加する必要がある
            // 一旦はリフレクションで対応
            var panelField = typeof(ScenarioRunner).GetField("_messagePanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var textField = typeof(ScenarioRunner).GetField("_messageText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (panelField != null) panelField.SetValue(runner, _tutorialPanel);
            if (textField != null) textField.SetValue(runner, _tutorialText);
        }

        private List<ScenarioStep> BuildTutorialScenario()
        {
            return new List<ScenarioStep>
            {
                // 1. ようこそ
                new ScenarioStep { StepId = "welcome", Trigger = ScenarioTrigger.Immediate, WaitClickAfterAction = true, Action = ScenarioAction.ShowMessage, ActionParam = "チュートリアルへようこそ" },
                // 2. スライム紹介
                new ScenarioStep { StepId = "intro_slime", Trigger = ScenarioTrigger.Immediate, WaitClickAfterAction = true, Action = ScenarioAction.ShowMessage, ActionParam = "近くに一匹の氷スライムがいます" },
                // 3. スライム強制移動
                new ScenarioStep { StepId = "move_slime", Trigger = ScenarioTrigger.Immediate, Action = ScenarioAction.ForceMoveEntity, ActionParam = "IceSlime;5,5" },
                // 4. 乗ったことの通知
                new ScenarioStep { StepId = "slime_on_fire", Trigger = ScenarioTrigger.Immediate, WaitClickAfterAction = true, Action = ScenarioAction.ShowMessage, ActionParam = "氷スライムが火のマスに乗ってしまいました" },
                // 5. 即死予告
                new ScenarioStep { StepId = "death_warning", Trigger = ScenarioTrigger.Immediate, WaitClickAfterAction = true, Action = ScenarioAction.ShowMessage, ActionParam = "氷スライムは火に弱いので\n即死してしまいます" },
                // 6. メッセージ非表示
                new ScenarioStep { StepId = "hide_warning", Trigger = ScenarioTrigger.Immediate, Action = ScenarioAction.HideMessage },
                // 7. 移動許可
                new ScenarioStep { StepId = "allow_move", Trigger = ScenarioTrigger.Immediate, Action = ScenarioAction.AllowMovement },
                // 8. スライム死亡を検知
                new ScenarioStep { StepId = "slime_died", Trigger = ScenarioTrigger.EntityDied, TriggerParam = "IceSlime", WaitClickAfterAction = true, Action = ScenarioAction.ShowMessage, ActionParam = "、、、" },
                // 9. 新スライム出現
                new ScenarioStep { StepId = "new_slime", Trigger = ScenarioTrigger.Immediate, Action = ScenarioAction.SpawnEnemy, ActionParam = "7,2", ActionParam2 = "IceSlime2" },
                // 10. 新スライムメッセージ
                new ScenarioStep { StepId = "new_slime_msg", Trigger = ScenarioTrigger.Immediate, WaitClickAfterAction = true, Action = ScenarioAction.ShowMessage, ActionParam = "また一匹新しい氷スライムが出てきました" },
                // 11. 戦闘促進
                new ScenarioStep { StepId = "go_fight", Trigger = ScenarioTrigger.Immediate, WaitClickAfterAction = true, Action = ScenarioAction.ShowMessage, ActionParam = "近づいて戦ってみましょう" },
                // 12. ルール操作の説明
                new ScenarioStep { StepId = "rule_power", Trigger = ScenarioTrigger.Immediate, WaitClickAfterAction = true, Action = ScenarioAction.ShowMessage, ActionParam = "おっと言い忘れてました。\nあなたにはルールを操るという\n特別な力を与えておきましたよ" },
                // 13. シナリオ終了
                new ScenarioStep { StepId = "end", Trigger = ScenarioTrigger.Immediate, Action = ScenarioAction.EndScenario },
            };
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
            img.color = Theme.MessagePanelColor;

            // テキスト
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(_tutorialPanel.transform, false);
            _tutorialText = textGo.AddComponent<Text>();
            _tutorialText.font = Theme.GetSafeFont();
            _tutorialText.fontSize = 18;
            _tutorialText.color = Theme.DefaultTextColor;
            _tutorialText.alignment = TextAnchor.MiddleCenter;
            _tutorialText.lineSpacing = 1.3f;
            var tRect = textGo.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.05f, 0.15f);
            tRect.anchorMax = new Vector2(0.95f, 0.95f);
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            // ▼マーク
            var arrowGo = new GameObject("Arrow");
            arrowGo.transform.SetParent(_tutorialPanel.transform, false);
            var arrowTxt = arrowGo.AddComponent<Text>();
            arrowTxt.font = Theme.GetSafeFont();
            arrowTxt.text = "▼";
            arrowTxt.fontSize = 14;
            arrowTxt.color = Theme.RuleHackStateLabelColor;
            arrowTxt.alignment = TextAnchor.LowerRight;
            var aRect = arrowGo.GetComponent<RectTransform>();
            aRect.anchorMin = new Vector2(0.85f, 0.02f);
            aRect.anchorMax = new Vector2(0.98f, 0.15f);
            aRect.offsetMin = Vector2.zero;
            aRect.offsetMax = Vector2.zero;

            _tutorialPanel.SetActive(false);
        }
    }
}
