using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FlipLogic.Data;

namespace FlipLogic.Editor.DesignerPanel
{
    public class EnemyEditorTab : DesignerPanelTab
    {
        public override string TabName => "Enemies";

        private List<EnemyData> _enemyAssets = new List<EnemyData>();
        private int _selectedIndex = -1;
        private Vector2 _listScrollPos;
        private Vector2 _detailScrollPos;

        private SerializedObject _serializedEnemy;

        public override void InitializeOnGUI()
        {
            if (_enemyAssets.Count == 0)
            {
                RefreshEnemyList();
            }
        }

        private void RefreshEnemyList()
        {
            _enemyAssets.Clear();
            string[] guids = AssetDatabase.FindAssets("t:EnemyData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EnemyData asset = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                if (asset != null)
                {
                    _enemyAssets.Add(asset);
                }
            }

            _enemyAssets.Sort((a, b) => string.Compare(a.EnemyName, b.EnemyName));
            
            if (_selectedIndex >= _enemyAssets.Count)
            {
                _selectedIndex = -1;
            }
        }

        public override void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            // Left Panel (List)
            DrawListPanel();

            // Right Panel (Detail)
            DrawDetailPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawListPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(250));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Enemy Assets", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                RefreshEnemyList();
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Create New Enemy", GUILayout.Height(30)))
            {
                CreateNewEnemyAsset();
            }

            GUILayout.Space(5);

            _listScrollPos = EditorGUILayout.BeginScrollView(_listScrollPos);

            for (int i = 0; i < _enemyAssets.Count; i++)
            {
                var enemy = _enemyAssets[i];
                if (enemy == null) continue;

                string label = string.IsNullOrEmpty(enemy.EnemyName) ? enemy.name : enemy.EnemyName;
                
                GUIStyle btnStyle = (_selectedIndex == i) ? EditorStyles.selectionRect : EditorStyles.helpBox;
                if (GUILayout.Button(label, btnStyle))
                {
                    SelectEnemy(i);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void SelectEnemy(int index)
        {
            _selectedIndex = index;
            if (_selectedIndex >= 0 && _selectedIndex < _enemyAssets.Count)
            {
                var asset = _enemyAssets[_selectedIndex];
                if (asset != null)
                {
                    _serializedEnemy = new SerializedObject(asset);
                }
                else
                {
                    _serializedEnemy = null;
                }
            }
            else
            {
                _serializedEnemy = null;
            }
        }

        private void CreateNewEnemyAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create New Enemy Asset", "NewEnemy", "asset", "Save enemy asset");
            if (string.IsNullOrEmpty(path)) return;

            EnemyData newAsset = ScriptableObject.CreateInstance<EnemyData>();
            newAsset.EnemyName = "New Enemy";

            AssetDatabase.CreateAsset(newAsset, path);
            AssetDatabase.SaveAssets();

            RefreshEnemyList();
            for (int i = 0; i < _enemyAssets.Count; i++)
            {
                if (_enemyAssets[i] == newAsset)
                {
                    SelectEnemy(i);
                    break;
                }
            }
        }

        private void DrawDetailPanel()
        {
            EditorGUILayout.BeginVertical("box");

            if (_serializedEnemy == null || _serializedEnemy.targetObject == null)
            {
                GUILayout.Label("Select an Enemy Asset from the list.", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            _detailScrollPos = EditorGUILayout.BeginScrollView(_detailScrollPos);

            _serializedEnemy.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedEnemy.FindProperty("EnemyName"));
            EditorGUILayout.PropertyField(_serializedEnemy.FindProperty("Description"));
            EditorGUILayout.PropertyField(_serializedEnemy.FindProperty("VisualDef"));

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedEnemy.FindProperty("MaxHp"));
            EditorGUILayout.PropertyField(_serializedEnemy.FindProperty("Attack"));
            EditorGUILayout.PropertyField(_serializedEnemy.FindProperty("Defense"));

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Initial Tags", EditorStyles.boldLabel);
            DrawTagListProperty(_serializedEnemy.FindProperty("InitialTags"));

            GUILayout.Space(10);
            EditorGUILayout.LabelField("AI & Rules", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedEnemy.FindProperty("AIType"));
            EditorGUILayout.PropertyField(_serializedEnemy.FindProperty("RelatedRuleId"));

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Messages", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedEnemy.FindProperty("EncounterMessage"));
            EditorGUILayout.PropertyField(_serializedEnemy.FindProperty("DefeatMessage"));

            if (EditorGUI.EndChangeCheck())
            {
                _serializedEnemy.ApplyModifiedProperties();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawTagListProperty(SerializedProperty listProp)
        {
            // Simple Drawing using default PropertyField for arrays, which gives ReorderableList in newer Unity
            // To make it use TagKeyRegistry dropdowns, we would need a custom PropertyDrawer for TagDefinition
            // For now, PropertyField is functional.
            EditorGUILayout.PropertyField(listProp, new GUIContent("Initial Tags"), true);
        }
    }
}
