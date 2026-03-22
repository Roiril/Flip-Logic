using System;
using System.Collections.Generic;
using UnityEngine;
using FlipLogic.Core;
using FlipLogic.Grid;
using FlipLogic.Explore;
using Cysharp.Threading.Tasks;

namespace FlipLogic.Scenario
{
    /// <summary>
    /// シナリオ実行エンジン。ScenarioStepのリストを順に実行し、
    /// トリガー条件を監視して自動進行する。
    /// </summary>
    public class ScenarioRunner : MonoBehaviour
    {
        public static ScenarioRunner Instance { get; private set; }

        [Header("UI")]
        [SerializeField] private GameObject _messagePanel;
        [SerializeField] private UnityEngine.UI.Text _messageText;

        private List<ScenarioStep> _steps;
        private int _currentIndex;
        private bool _isRunning;
        private bool _waitingForClick;
        private bool _movementBlocked;

        private readonly Dictionary<string, bool> _flags = new Dictionary<string, bool>();

        public bool IsMovementBlocked => _movementBlocked;
        public bool IsRunning => _isRunning;

        public event Action<GameEntity> OnEnemySpawned;

        private int _turnAtStepStart;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnTurnEnd -= OnTurnEnd;
        }

        public void StartScenario(List<ScenarioStep> steps)
        {
            _steps = steps;
            _currentIndex = 0;
            _isRunning = true;
            _waitingForClick = false;
            _movementBlocked = false;
            _flags.Clear();

            if (TurnManager.Instance != null)
                TurnManager.Instance.OnTurnEnd += OnTurnEnd;

            ProcessCurrent();
        }

        private void Update()
        {
            if (!_isRunning || _currentIndex >= _steps.Count) return;

            if (_waitingForClick)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    _waitingForClick = false;
                    Advance();
                }
                return;
            }

            var step = _steps[_currentIndex];
            if (step.Trigger == ScenarioTrigger.EntityOnTile)
                CheckEntityOnTile(step);
            else if (step.Trigger == ScenarioTrigger.EntityDied)
                CheckEntityDied(step);
            else if (step.Trigger == ScenarioTrigger.FlagSet && HasFlag(step.TriggerParam))
                RunAction(step);
        }

        private void OnTurnEnd(int turn)
        {
            if (!_isRunning || _currentIndex >= _steps.Count) return;
            var step = _steps[_currentIndex];

            if (step.Trigger == ScenarioTrigger.TurnEnd)
            {
                RunAction(step);
            }
            else if (step.Trigger == ScenarioTrigger.TurnCount)
            {
                int req = 1;
                int.TryParse(step.TriggerParam, out req);
                if (turn - _turnAtStepStart >= req) RunAction(step);
            }
        }

        private void ProcessCurrent()
        {
            if (_currentIndex >= _steps.Count) { Finish(); return; }

            var step = _steps[_currentIndex];
            _turnAtStepStart = TurnManager.Instance != null ? TurnManager.Instance.CurrentTurn : 0;

            if (step.Trigger == ScenarioTrigger.Immediate || step.Trigger == ScenarioTrigger.Click)
            {
                RunAction(step);
            }
        }

        private void RunAction(ScenarioStep step)
        {
            switch (step.Action)
            {
                case ScenarioAction.ShowMessage:    ShowMsg(step.ActionParam); break;
                case ScenarioAction.HideMessage:    HideMsg(); break;
                case ScenarioAction.SpawnEnemy:      DoSpawnEnemy(step.ActionParam, step.ActionParam2); break;
                case ScenarioAction.DespawnEntity:   DoDespawn(step.ActionParam); break;
                case ScenarioAction.SetFlag:         _flags[step.ActionParam] = true; break;
                case ScenarioAction.ClearFlag:       _flags[step.ActionParam] = false; break;
                case ScenarioAction.AllowMovement:   _movementBlocked = false; break;
                case ScenarioAction.BlockMovement:   _movementBlocked = true; break;
                case ScenarioAction.AddTileTag:      DoAddTileTag(step.ActionParam, step.ActionParam2); break;
                case ScenarioAction.ForceMoveEntity: DoForceMove(step.ActionParam); break;
                case ScenarioAction.ResolveTurn:     DoResolveTurn().Forget(); break;
                case ScenarioAction.SpawnRuleBoard:  DoSpawnRuleBoard(step.ActionParam); break;
                case ScenarioAction.EndScenario:     Finish(); return;
            }

            if (step.WaitClickAfterAction || step.Trigger == ScenarioTrigger.Click)
            {
                _waitingForClick = true;
            }
            else
            {
                Advance();
            }
        }

        private void Advance()
        {
            _currentIndex++;
            ProcessCurrent();
        }

        private void Finish()
        {
            _isRunning = false;
            _movementBlocked = false;
            HideMsg();
            if (TurnManager.Instance != null)
                TurnManager.Instance.OnTurnEnd -= OnTurnEnd;
        }

        private void ShowMsg(string text)
        {
            if (_messagePanel != null) _messagePanel.SetActive(true);
            if (_messageText != null) _messageText.text = text;
        }

        private void HideMsg()
        {
            if (_messagePanel != null) _messagePanel.SetActive(false);
        }

        private void DoSpawnEnemy(string posStr, string name)
        {
            var pos = ParseV2I(posStr);
            if (!pos.HasValue) return;

            var go = new GameObject(name ?? "SpawnedEnemy");
            var entity = go.AddComponent<GameEntity>();
            entity.Initialize(name ?? "SpawnedEnemy", EntityType.Enemy, pos.Value, 10, 2, 0);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = EntitySpriteFactory.CreateCircleWithLetter('E', new Color(0.5f, 0.9f, 1.0f), Color.white);
            sr.sortingOrder = 5;
            go.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            var sym = go.AddComponent<EnemySymbol>();
            sym.AIType = EnemyAIType.Stationary;

            if (GridMap.Instance != null)
            {
                go.transform.position = GridMap.Instance.GridToWorld(pos.Value);
                GridMap.Instance.RegisterEntity(entity, pos.Value);
            }

            // チュートリアル向けの仮実装（後でデータ化）
            entity.Tags.AddTag(new TagDefinition("Element", "Ice", -1, "Nature"));
            OnEnemySpawned?.Invoke(entity);
        }

        private void DoDespawn(string entityName)
        {
            var entities = EntityRegistry.Instance.GetAllEntities();
            foreach (var e in entities)
            {
                if (e.EntityName == entityName)
                {
                    if (GridMap.Instance != null) GridMap.Instance.UnregisterEntity(e);
                    Destroy(e.gameObject);
                    break;
                }
            }
        }

        private void DoAddTileTag(string posStr, string tagStr)
        {
            var pos = ParseV2I(posStr);
            if (!pos.HasValue || GridMap.Instance == null) return;
            var parts = tagStr.Split(':');
            if (parts.Length < 2) return;
            GridMap.Instance.AddCellTag(pos.Value, new TagDefinition(parts[0], parts[1], -1, "Scenario"));
            if (TileOverlayRenderer.Instance != null)
                TileOverlayRenderer.Instance.UpdateOverlay(pos.Value);
        }

        private void DoForceMove(string param)
        {
            var parts = param.Split(';');
            if (parts.Length < 2) return;
            string eName = parts[0];
            var newPos = ParseV2I(parts[1]);
            if (!newPos.HasValue) return;

            var entities = EntityRegistry.Instance.GetAllEntities();
            foreach (var e in entities)
            {
                if (e.EntityName == eName)
                {
                    var symbol = e.GetComponent<EnemySymbol>();
                    if (symbol != null) symbol.ForceMoveTo(newPos.Value);
                    else
                    {
                        e.GridPosition = newPos.Value;
                        if (GridMap.Instance != null)
                        {
                            GridMap.Instance.RegisterEntity(e, newPos.Value);
                            e.transform.position = GridMap.Instance.GridToWorld(newPos.Value);
                        }
                    }
                    break;
                }
            }
        }

        private async UniTaskVoid DoResolveTurn()
        {
            await TurnResolutionProcessor.ExecuteAsync();
        }

        private void DoSpawnRuleBoard(string posStr)
        {
            // TutorialSetupのSpawnRuleBoardを呼び出すか、ここ自体にロジックを実装する
            // チュートリアル固有の処理なので、TutorialSetupから実行するのが望ましい
            var tutorial = FindAnyObjectByType<Tutorial.TutorialSetup>();
            if (tutorial != null)
            {
                var pos = ParseV2I(posStr);
                if (pos.HasValue)
                {
                    // TutorialSetup側にpublicなSpawnRuleBoardがある前提
                    var method = tutorial.GetType().GetMethod("SpawnRuleBoard", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (method != null) method.Invoke(tutorial, new object[] { pos.Value });
                }
            }
        }

        private void CheckEntityOnTile(ScenarioStep step)
        {
            var parts = step.TriggerParam.Split(';');
            if (parts.Length < 2) return;
            string eName = parts[0];
            var tPos = ParseV2I(parts[1]);
            if (!tPos.HasValue) return;

            var entities = EntityRegistry.Instance.GetAllEntities();
            foreach (var e in entities)
            {
                if (e.EntityName == eName && e.GridPosition == tPos.Value)
                { RunAction(step); return; }
            }
        }

        private void CheckEntityDied(ScenarioStep step)
        {
            string eName = step.TriggerParam;
            var entities = EntityRegistry.Instance.GetAllEntities();
            foreach (var e in entities)
            {
                if (e.EntityName == eName && e.IsAlive) return;
            }
            RunAction(step);
        }

        public bool HasFlag(string f) => _flags.ContainsKey(f) && _flags[f];

        private static Vector2Int? ParseV2I(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            var p = s.Split(',');
            if (p.Length < 2) return null;
            if (int.TryParse(p[0].Trim(), out int x) && int.TryParse(p[1].Trim(), out int y))
                return new Vector2Int(x, y);
            return null;
        }
    }
}
