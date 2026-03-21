using UnityEngine;
using UnityEditor;
using FlipLogic.Data;
using FlipLogic.Core;
using System.Linq;

namespace FlipLogic.Editor.DesignerPanel
{
    public class MapEditorTab : DesignerPanelTab
    {
        public override string TabName => "Map";

        private MapPlacementData _targetData;
        private SerializedObject _serializedObject;
        private SerializedProperty _enemiesProp;
        private SerializedProperty _cellTagsProp;

        private bool _isEditMode = false;
        private enum EditTool { None, AddEnemy, AddTag, Erase }
        private EditTool _currentTool = EditTool.None;
        
        private EnemyData _selectedEnemy;

        // タグ編集用
        private string _selectedTagKey = "";
        private string _selectedTagValue = "";

        // プレビュー表示用ギズモカラー
        private readonly Color _enemyColor = new Color(1f, 0.3f, 0.3f, 0.7f);
        private readonly Color _tagColor = new Color(1f, 0.6f, 0f, 0.5f);

        public override void InitializeOnGUI()
        {
            if (TagKeyRegistry.Instance != null && _selectedTagKey == "")
            {
                var keys = TagKeyRegistry.Instance.Entries.Select(e => e.Key).ToArray();
                if (keys.Length > 0) _selectedTagKey = keys[0];
            }
        }

        public override void OnEnable(DesignerPanelWindow window)
        {
            base.OnEnable(window);
            SceneView.duringSceneGui += OnSceneGUI;
        }

        public override void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            if (_serializedObject != null)
            {
                _serializedObject.Dispose();
                _serializedObject = null;
            }
            base.OnDisable();
        }

        public override void OnGUI()
        {
            GUILayout.Label("Map Editor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("シーンビュー上でクリックして敵やギミックを配置します。", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            _targetData = (MapPlacementData)EditorGUILayout.ObjectField("Target Data", _targetData, typeof(MapPlacementData), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (_targetData != null)
                {
                    _serializedObject = new SerializedObject(_targetData);
                    _enemiesProp = _serializedObject.FindProperty("Enemies");
                    _cellTagsProp = _serializedObject.FindProperty("CellTags");
                }
                else
                {
                    _serializedObject = null;
                }
            }

            if (_targetData == null || _serializedObject == null) return;
            
            _serializedObject.Update();

            EditorGUILayout.Space();
            GUILayout.Label("Tools", EditorStyles.boldLabel);
            
            _isEditMode = EditorGUILayout.Toggle("Enable Edit Mode", _isEditMode);
            
            if (_isEditMode)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Toggle(_currentTool == EditTool.AddEnemy, "Add Enemy", "Button")) _currentTool = EditTool.AddEnemy;
                if (GUILayout.Toggle(_currentTool == EditTool.AddTag, "Add Tag", "Button")) _currentTool = EditTool.AddTag;
                if (GUILayout.Toggle(_currentTool == EditTool.Erase, "Erase", "Button")) _currentTool = EditTool.Erase;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
                if (_currentTool == EditTool.AddEnemy)
                {
                    _selectedEnemy = (EnemyData)EditorGUILayout.ObjectField("Enemy", _selectedEnemy, typeof(EnemyData), false);
                }
                else if (_currentTool == EditTool.AddTag)
                {
                    DrawTagSelector();
                }
                EditorGUILayout.Space();
            }

            EditorGUILayout.PropertyField(_enemiesProp, true);
            EditorGUILayout.PropertyField(_cellTagsProp, true);

            if (_serializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(_targetData);
                SceneView.RepaintAll();
            }
        }

        private void DrawTagSelector()
        {
            var registry = TagKeyRegistry.Instance;
            if (registry == null)
            {
                _selectedTagKey = EditorGUILayout.TextField("Tag Key", _selectedTagKey);
                _selectedTagValue = EditorGUILayout.TextField("Tag Value", _selectedTagValue);
                return;
            }

            string[] keys = registry.Entries.Select(e => e.Key).ToArray();
            int keyIndex = System.Array.IndexOf(keys, _selectedTagKey);
            if (keyIndex < 0) keyIndex = 0;

            if (keys.Length > 0)
            {
                int newKeyIndex = EditorGUILayout.Popup("Tag Key", keyIndex, keys);
                _selectedTagKey = keys[newKeyIndex];

                var entry = registry.Entries[newKeyIndex];
                if (entry.AllowedValues != null && entry.AllowedValues.Count > 0)
                {
                    string[] vals = entry.AllowedValues.ToArray();
                    int valIndex = System.Array.IndexOf(vals, _selectedTagValue);
                    if (valIndex < 0) valIndex = 0;
                    int newValIndex = EditorGUILayout.Popup("Tag Value", valIndex, vals);
                    _selectedTagValue = vals[newValIndex];
                }
                else
                {
                    _selectedTagValue = EditorGUILayout.TextField("Tag Value", _selectedTagValue);
                }
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isEditMode || _targetData == null) return;
            if (_currentTool == EditTool.None) return;

            Event e = Event.current;
            
            // XZ平面（Z=0）でのレイキャスト
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Mathf.Approximately(ray.direction.z, 0)) return;

            float t = -ray.origin.z / ray.direction.z;
            Vector3 hitPos = ray.origin + ray.direction * t;
            Vector2Int gridPos = new Vector2Int(Mathf.FloorToInt(hitPos.x), Mathf.FloorToInt(hitPos.y));

            // マウス位置のプレビュー表示
            DrawPreviewHandle(gridPos);

            // シーンビューのコントロールを奪う
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(controlID);
            }

            // クリック判定
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                Undo.RecordObject(_targetData, "Map Edit");

                if (_currentTool == EditTool.AddEnemy && _selectedEnemy != null)
                {
                    // 既存の敵がいれば削除してから追加
                    _targetData.Enemies.RemoveAll(x => x.GridPosition == gridPos);
                    _targetData.Enemies.Add(new EnemyPlacement { EnemyData = _selectedEnemy, GridPosition = gridPos });
                }
                else if (_currentTool == EditTool.AddTag && !string.IsNullOrEmpty(_selectedTagKey))
                {
                    var cell = _targetData.CellTags.FirstOrDefault(x => x.GridPosition == gridPos);
                    if (cell == null)
                    {
                        cell = new CellTagPlacement { GridPosition = gridPos };
                        _targetData.CellTags.Add(cell);
                    }
                    // 同じキーがあれば上書き、なければ追加
                    cell.Tags.RemoveAll(x => x.Key == _selectedTagKey);
                    cell.Tags.Add(new TagDefinition(_selectedTagKey, _selectedTagValue, -1));
                }
                else if (_currentTool == EditTool.Erase)
                {
                    _targetData.Enemies.RemoveAll(x => x.GridPosition == gridPos);
                    _targetData.CellTags.RemoveAll(x => x.GridPosition == gridPos);
                }

                EditorUtility.SetDirty(_targetData);
                e.Use();
            }
        }

        private void DrawPreviewHandle(Vector2Int gridPos)
        {
            Vector3 center = new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, -0.1f);

            if (_currentTool == EditTool.AddEnemy && _selectedEnemy != null)
            {
                Handles.color = _enemyColor;
                Handles.DrawWireDisc(center, Vector3.forward, 0.4f);
                Handles.Label(center + Vector3.up * 0.6f, _selectedEnemy.EnemyName);
            }
            else if (_currentTool == EditTool.AddTag)
            {
                Handles.color = _tagColor;
                Handles.DrawWireCube(center, new Vector3(0.8f, 0.8f, 0f));
                Handles.Label(center + Vector3.up * 0.6f, $"{_selectedTagKey}:{_selectedTagValue}");
            }
            else if (_currentTool == EditTool.Erase)
            {
                Handles.color = Color.red;
                Handles.DrawLine(center + new Vector3(-0.3f, -0.3f, 0), center + new Vector3(0.3f, 0.3f, 0));
                Handles.DrawLine(center + new Vector3(-0.3f, 0.3f, 0), center + new Vector3(0.3f, -0.3f, 0));
            }
            
            // 強制再描画
            SceneView.RepaintAll();
        }
    }
}
