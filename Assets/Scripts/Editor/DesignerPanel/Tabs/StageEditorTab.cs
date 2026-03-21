using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FlipLogic.Data;

namespace FlipLogic.Editor.DesignerPanel
{
    public class StageEditorTab : DesignerPanelTab
    {
        public override string TabName => "Stages";

        private List<StageConfig> _stageAssets = new List<StageConfig>();
        private int _selectedIndex = -1;
        private Vector2 _listScrollPos;
        private Vector2 _detailScrollPos;

        private SerializedObject _serializedStage;

        public override void InitializeOnGUI()
        {
            if (_stageAssets.Count == 0)
            {
                RefreshStageList();
            }
        }

        private void RefreshStageList()
        {
            _stageAssets.Clear();
            string[] guids = AssetDatabase.FindAssets("t:StageConfig");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                StageConfig asset = AssetDatabase.LoadAssetAtPath<StageConfig>(path);
                if (asset != null)
                {
                    _stageAssets.Add(asset);
                }
            }

            _stageAssets.Sort((a, b) => string.Compare(a.StageName, b.StageName));
            
            if (_selectedIndex >= _stageAssets.Count)
            {
                _selectedIndex = -1;
            }
        }

        public override void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            // Left Panel
            DrawListPanel();

            // Right Panel
            DrawDetailPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawListPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(250));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Stage Configs", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                RefreshStageList();
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Create New Stage", GUILayout.Height(30)))
            {
                CreateNewStageAsset();
            }

            GUILayout.Space(5);

            _listScrollPos = EditorGUILayout.BeginScrollView(_listScrollPos);

            for (int i = 0; i < _stageAssets.Count; i++)
            {
                var stage = _stageAssets[i];
                if (stage == null) continue;

                string label = string.IsNullOrEmpty(stage.StageName) ? stage.name : stage.StageName;
                
                GUIStyle btnStyle = (_selectedIndex == i) ? EditorStyles.selectionRect : EditorStyles.helpBox;
                if (GUILayout.Button(label, btnStyle))
                {
                    SelectStage(i);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void SelectStage(int index)
        {
            _selectedIndex = index;
            if (_selectedIndex >= 0 && _selectedIndex < _stageAssets.Count)
            {
                var asset = _stageAssets[_selectedIndex];
                if (asset != null)
                {
                    _serializedStage = new SerializedObject(asset);
                }
                else
                {
                    _serializedStage = null;
                }
            }
            else
            {
                _serializedStage = null;
            }
        }

        private void CreateNewStageAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create New Stage Config", "NewStage", "asset", "Save stage config");
            if (string.IsNullOrEmpty(path)) return;

            StageConfig newAsset = ScriptableObject.CreateInstance<StageConfig>();
            newAsset.StageName = "New Stage";

            AssetDatabase.CreateAsset(newAsset, path);
            AssetDatabase.SaveAssets();

            RefreshStageList();
            for (int i = 0; i < _stageAssets.Count; i++)
            {
                if (_stageAssets[i] == newAsset)
                {
                    SelectStage(i);
                    break;
                }
            }
        }

        private void DrawDetailPanel()
        {
            EditorGUILayout.BeginVertical("box");

            if (_serializedStage == null || _serializedStage.targetObject == null)
            {
                GUILayout.Label("Select a Stage Config from the list.", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            _detailScrollPos = EditorGUILayout.BeginScrollView(_detailScrollPos);

            _serializedStage.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Stage Information", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedStage.FindProperty("StageName"));
            EditorGUILayout.PropertyField(_serializedStage.FindProperty("Description"));

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Rule Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedStage.FindProperty("Rulebook"));

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Map Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedStage.FindProperty("RecommendedMapSize"));

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Meta Data", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedStage.FindProperty("Difficulty"));

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Editor Test", EditorStyles.boldLabel);
            var sceneAssetProp = _serializedStage.FindProperty("SceneAsset");
            if (sceneAssetProp != null)
            {
                EditorGUILayout.PropertyField(sceneAssetProp);
            }

            if (EditorGUI.EndChangeCheck())
            {
                _serializedStage.ApplyModifiedProperties();
            }

            GUILayout.Space(15);
            if (GUILayout.Button("▶ Play Stage", GUILayout.Height(30)))
            {
                PlayStage();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void PlayStage()
        {
            if (_serializedStage == null || _serializedStage.targetObject == null) return;
            var stage = (StageConfig)_serializedStage.targetObject;

            if (stage.SceneAsset == null)
            {
                EditorUtility.DisplayDialog("Error", "SceneAssetが設定されていません。", "OK");
                return;
            }

            string scenePath = AssetDatabase.GetAssetPath(stage.SceneAsset);
            if (string.IsNullOrEmpty(scenePath)) return;

            // シーンを保存するか確認して開く
            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                EditorApplication.EnterPlaymode();
            }
        }
    }
}
