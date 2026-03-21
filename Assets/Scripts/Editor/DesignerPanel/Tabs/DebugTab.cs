using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FlipLogic.Core;
using FlipLogic.Rulebook;
using FlipLogic.Logic;

namespace FlipLogic.Editor.DesignerPanel
{
    public class DebugTab : DesignerPanelTab, IRuleEventLogger
    {
        public override string TabName => "Debug";

        private Vector2 _scrollPos;
        private Vector2 _logScrollPos;
        private EntityRegistry _registry;

        // ログ記録用リングバッファ
        private const int MAX_LOG_COUNT = 500;
        private Queue<RuleEvalEvent> _evalLogs = new Queue<RuleEvalEvent>();
        private bool _showOnlyApplied = true;

        public override void InitializeOnGUI()
        {
            // Debug panel mainly works when playing
        }

        public override void OnEnable(DesignerPanelWindow window)
        {
            base.OnEnable(window);
            if (Application.isPlaying)
            {
                RuleEvaluator.GlobalLogger = this;
            }
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public override void OnDisable()
        {
            if (RuleEvaluator.GlobalLogger == this)
            {
                RuleEvaluator.GlobalLogger = null;
            }
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            base.OnDisable();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                RuleEvaluator.GlobalLogger = this;
                _evalLogs.Clear();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                if (RuleEvaluator.GlobalLogger == this)
                    RuleEvaluator.GlobalLogger = null;
            }
        }

        public void LogEvent(RuleEvalEvent evalEvent)
        {
            if (_evalLogs.Count >= MAX_LOG_COUNT)
            {
                _evalLogs.Dequeue();
            }
            _evalLogs.Enqueue(evalEvent);
            // プレイ中の再描画要求
            Window.Repaint();
        }

        public override void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Debug Panel is only available in Play Mode.", MessageType.Info);
                return;
            }

            _registry = EntityRegistry.Instance;
            if (_registry == null)
            {
                EditorGUILayout.HelpBox("EntityRegistry is not initialized.", MessageType.Warning);
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.BeginHorizontal();
            
            // 左側のカラム（ルールとエンティティ）
            EditorGUILayout.BeginVertical(GUILayout.Width(300));
            DrawActiveRulesSection();
            GUILayout.Space(15);
            drawSplitter();
            DrawEntityListSection();
            EditorGUILayout.EndVertical();

            // 右側のカラム（ルール評価ログ）
            EditorGUILayout.BeginVertical("box");
            DrawRuleLogSection();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        private void DrawRuleLogSection()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Rule Evaluation Log", EditorStyles.boldLabel);
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                _evalLogs.Clear();
            }
            EditorGUILayout.EndHorizontal();

            _showOnlyApplied = EditorGUILayout.Toggle("Show Only Applied", _showOnlyApplied);

            EditorGUILayout.Space(5);

            _logScrollPos = EditorGUILayout.BeginScrollView(_logScrollPos);

            foreach (var log in _evalLogs)
            {
                if (_showOnlyApplied && !log.ConditionMet) continue;

                GUI.color = log.ConditionMet ? new Color(0.8f, 1f, 0.8f) : new Color(1f, 0.8f, 0.8f);
                EditorGUILayout.BeginVertical("helpBox");
                GUI.color = Color.white;

                string timeStr = log.Timestamp.ToString("HH:mm:ss.fff");
                EditorGUILayout.LabelField($"[{timeStr}] Turn:{log.TurnNumber} Phase:{log.PhaseName}", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField($"{log.RuleName} ({log.TargetEntityInfo})", EditorStyles.boldLabel);
                
                string cond = log.ConditionData.Key != null ? $"{log.ConditionData.Key}:{log.ConditionData.Value}" : "None";
                EditorGUILayout.LabelField($"Condition: {(log.ConditionMet ? "MET" : "FAILED")} (Require: {cond})");

                if (log.ConditionMet && log.AppliedEffectData.Key != null)
                {
                    string op = log.AppliedEffectData.Operation == Data.TagOperation.Add ? "+" : "-";
                    EditorGUILayout.LabelField($"Action: {log.AppliedEffectData.Target} {op}[{log.AppliedEffectData.Key}:{log.AppliedEffectData.Value}]");
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawActiveRulesSection()
        {
            EditorGUILayout.LabelField("Active Rules", EditorStyles.boldLabel);

            if (GameManager.Instance == null || GameManager.Instance.RulebookData == null)
            {
                EditorGUILayout.LabelField("RulebookManager not found.");
                return;
            }

            var rules = GameManager.Instance.RulebookData.GetActiveRules();
            if (rules == null || rules.Count == 0)
            {
                EditorGUILayout.LabelField("No rules active.", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var rule in rules)
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField(rule.RuleId, EditorStyles.boldLabel);
                    
                    EditorGUILayout.LabelField($"Active: {rule.IsActive}");

                    if (rule.TagConditionP != null)
                    {
                        var cond = rule.TagConditionP;
                        EditorGUILayout.LabelField($"P: {cond.Target} must have [{cond.Key} = {cond.Value}]", EditorStyles.wordWrappedMiniLabel);
                    }

                    if (rule.TagResultQ != null)
                    {
                        var res = rule.TagResultQ;
                        string op = res.Operation == Data.TagOperation.Add ? "+" : "-";
                        EditorGUILayout.LabelField($"Q: {res.Target} {op}[{res.Key} = {res.Value}]", EditorStyles.wordWrappedMiniLabel);
                    }

                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void DrawEntityListSection()
        {
            EditorGUILayout.LabelField("Entities", EditorStyles.boldLabel);

            var entities = _registry.GetAllEntities();

            if (entities == null)
            {
                EditorGUILayout.LabelField("No entities registered.");
                return;
            }

            foreach (var entity in entities)
            {
                if (entity == null) continue;

                string id = entity.gameObject.GetInstanceID().ToString();

                EditorGUILayout.BeginVertical("helpBox");
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{entity.Type} : {id}", EditorStyles.boldLabel);
                if (GUILayout.Button("Ping", GUILayout.Width(60)))
                {
                    EditorGUIUtility.PingObject(entity.gameObject);
                }
                EditorGUILayout.EndHorizontal();

                DrawEntityTags(entity);

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawEntityTags(GameEntity entity)
        {
            var tags = entity.Tags.Tags;
            if (tags == null || tags.Count == 0)
            {
                EditorGUILayout.LabelField("  No tags", EditorStyles.miniLabel);
                return;
            }

            foreach (var tag in tags)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                EditorGUILayout.LabelField($"{tag.Key} = {tag.Value}");
                EditorGUILayout.EndHorizontal();
            }
        }

        private void drawSplitter()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            GUILayout.Space(10);
        }
    }
}
