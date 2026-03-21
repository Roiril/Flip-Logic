using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FlipLogic.Data;
using FlipLogic.Rulebook;

namespace FlipLogic.Editor.DesignerPanel
{
    public class RuleEditorTab : DesignerPanelTab
    {
        public override string TabName => "Rules";

        private List<RuleAsset> _ruleAssets = new List<RuleAsset>();
        private int _selectedIndex = -1;
        private Vector2 _listScrollPos;
        private Vector2 _detailScrollPos;

        private SerializedObject _serializedRule;

        public override void InitializeOnGUI()
        {
            if (_ruleAssets.Count == 0)
            {
                RefreshRuleList();
            }
        }

        private void RefreshRuleList()
        {
            _ruleAssets.Clear();
            string[] guids = AssetDatabase.FindAssets("t:RuleAsset");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RuleAsset asset = AssetDatabase.LoadAssetAtPath<RuleAsset>(path);
                if (asset != null)
                {
                    _ruleAssets.Add(asset);
                }
            }

            // Sort by Chapter then RuleId
            _ruleAssets.Sort((a, b) => 
            {
                int c = a.Chapter.CompareTo(b.Chapter);
                if (c == 0) return string.Compare(a.RuleId, b.RuleId);
                return c;
            });
            
            if (_selectedIndex >= _ruleAssets.Count)
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
            GUILayout.Label("Rule Assets", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                RefreshRuleList();
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Create New Rule", GUILayout.Height(30)))
            {
                CreateNewRuleAsset();
            }

            GUILayout.Space(5);

            _listScrollPos = EditorGUILayout.BeginScrollView(_listScrollPos);

            for (int i = 0; i < _ruleAssets.Count; i++)
            {
                var rule = _ruleAssets[i];
                if (rule == null) continue;

                string label = $"[Ch.{rule.Chapter}] {rule.RuleId}\n{rule.RuleName}";
                
                GUIStyle btnStyle = (_selectedIndex == i) ? EditorStyles.selectionRect : EditorStyles.helpBox;
                if (GUILayout.Button(label, btnStyle))
                {
                    SelectRule(i);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void SelectRule(int index)
        {
            _selectedIndex = index;
            if (_selectedIndex >= 0 && _selectedIndex < _ruleAssets.Count)
            {
                var asset = _ruleAssets[_selectedIndex];
                if (asset != null)
                {
                    _serializedRule = new SerializedObject(asset);
                }
                else
                {
                    _serializedRule = null;
                }
            }
            else
            {
                _serializedRule = null;
            }
        }

        private void CreateNewRuleAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create New Rule Asset", "NewRule", "asset", "Save rule asset");
            if (string.IsNullOrEmpty(path)) return;

            RuleAsset newAsset = ScriptableObject.CreateInstance<RuleAsset>();
            newAsset.RuleId = "new_rule";
            newAsset.RuleName = "New Rule";
            newAsset.Condition = new PropositionData("...", "NOT ...");
            newAsset.Result = new PropositionData("...", "NOT ...");
            newAsset.TagConditionP = new TagCondition("Element", "");
            newAsset.TagResultQ = new TagEffect("Element", "", TagOperation.Add);

            AssetDatabase.CreateAsset(newAsset, path);
            AssetDatabase.SaveAssets();

            RefreshRuleList();
            for (int i = 0; i < _ruleAssets.Count; i++)
            {
                if (_ruleAssets[i] == newAsset)
                {
                    SelectRule(i);
                    break;
                }
            }
        }

        private void DrawDetailPanel()
        {
            EditorGUILayout.BeginVertical("box");

            if (_serializedRule == null || _serializedRule.targetObject == null)
            {
                GUILayout.Label("Select a Rule Asset from the list.", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndVertical();
                return;
            }

            _detailScrollPos = EditorGUILayout.BeginScrollView(_detailScrollPos);

            _serializedRule.Update();

            EditorGUI.BeginChangeCheck();

            // Basic Info
            EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedRule.FindProperty("RuleId"));
            EditorGUILayout.PropertyField(_serializedRule.FindProperty("RuleName"));
            EditorGUILayout.PropertyField(_serializedRule.FindProperty("Description"));
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_serializedRule.FindProperty("Chapter"));
            EditorGUILayout.PropertyField(_serializedRule.FindProperty("IsActive"));
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
            drawSplitter();

            // Propositions
            EditorGUILayout.LabelField("Propositions (Texts)", EditorStyles.boldLabel);
            var conditionProp = _serializedRule.FindProperty("Condition");
            var resultProp = _serializedRule.FindProperty("Result");

            EditorGUILayout.PropertyField(conditionProp, new GUIContent("Condition (P)"), true);
            EditorGUILayout.PropertyField(resultProp, new GUIContent("Result (Q)"), true);

            GUILayout.Space(10);
            drawSplitter();

            // Tag Logic
            EditorGUILayout.LabelField("Tag Logic Settings", EditorStyles.boldLabel);

            var subjectProp = _serializedRule.FindProperty("SubjectFilterP");
            DrawStructPropertyWithTagPopup(subjectProp, "Subject Filter (Precondition)");

            var tagCondProp = _serializedRule.FindProperty("TagConditionP");
            DrawStructPropertyWithTagPopup(tagCondProp, "Tag Condition (P)");

            var tagResProp = _serializedRule.FindProperty("TagResultQ");
            DrawStructPropertyWithTagPopup(tagResProp, "Tag Result (Q)");

            if (EditorGUI.EndChangeCheck())
            {
                _serializedRule.ApplyModifiedProperties();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void drawSplitter()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            GUILayout.Space(10);
        }

        private void DrawStructPropertyWithTagPopup(SerializedProperty property, string label)
        {
            if (property == null) return;
            
            EditorGUILayout.BeginVertical("helpBox");
            property.isExpanded = EditorGUILayout.Foldout(property.isExpanded, label, true, EditorStyles.foldoutHeader);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                // Collect children
                var it = property.Copy();
                var endProp = it.GetEndProperty();
                bool enterChildren = true;
                
                while (it.NextVisible(enterChildren) && !SerializedProperty.EqualContents(it, endProp))
                {
                    enterChildren = false; // Only top level children of the struct
                    
                    if (it.name == "Key")
                    {
                        DrawTagKeyDropdown(it);
                    }
                    else if (it.name == "Value")
                    {
                        DrawTagValueDropdown(property.FindPropertyRelative("Key").stringValue, it);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(it, true);
                    }
                }
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawTagKeyDropdown(SerializedProperty keyProp)
        {
            var registry = TagKeyRegistry.Instance;
            if (registry == null || registry.Entries.Count == 0)
            {
                EditorGUILayout.PropertyField(keyProp);
                return;
            }

            string[] keys = new string[registry.Entries.Count];
            int currentIndex = -1;
            for (int i = 0; i < registry.Entries.Count; i++)
            {
                keys[i] = registry.Entries[i].Key;
                if (keys[i] == keyProp.stringValue) currentIndex = i;
            }

            EditorGUILayout.BeginHorizontal();
            int newIndex = EditorGUILayout.Popup(keyProp.displayName, currentIndex, keys);
            if (newIndex >= 0 && newIndex != currentIndex)
            {
                keyProp.stringValue = keys[newIndex];
            }
            // Allow manual text entry just in case
            keyProp.stringValue = EditorGUILayout.TextField(keyProp.stringValue, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTagValueDropdown(string currentKey, SerializedProperty valueProp)
        {
            var registry = TagKeyRegistry.Instance;
            if (registry == null)
            {
                EditorGUILayout.PropertyField(valueProp);
                return;
            }

            var entry = System.Linq.Enumerable.FirstOrDefault(registry.Entries, e => e.Key == currentKey);
            if (entry == null || entry.AllowedValues == null || entry.AllowedValues.Count == 0)
            {
                EditorGUILayout.PropertyField(valueProp);
                return;
            }

            string[] vals = entry.AllowedValues.ToArray();
            int currentIndex = System.Array.IndexOf(vals, valueProp.stringValue);

            EditorGUILayout.BeginHorizontal();
            int newIndex = EditorGUILayout.Popup(valueProp.displayName, currentIndex, vals);
            if (newIndex >= 0 && newIndex != currentIndex)
            {
                valueProp.stringValue = vals[newIndex];
            }
            valueProp.stringValue = EditorGUILayout.TextField(valueProp.stringValue, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        }
    }
}
