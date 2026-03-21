using UnityEngine;
using UnityEditor;
using FlipLogic.Data;

namespace FlipLogic.Editor.DesignerPanel
{
    public class TagRegistryTab : DesignerPanelTab
    {
        public override string TabName => "Tags";

        private SerializedObject _serializedObject;
        private SerializedProperty _entriesProperty;

        public override void InitializeOnGUI()
        {
            if (_serializedObject == null)
            {
                var registry = TagKeyRegistry.Instance;
                if (registry != null)
                {
                    _serializedObject = new SerializedObject(registry);
                    _entriesProperty = _serializedObject.FindProperty("_entries");
                }
            }
        }

        public override void OnGUI()
        {
            if (_serializedObject == null)
            {
                EditorGUILayout.HelpBox("Resources/TagKeyRegistry アセットが見つかりません。\n存在しない場合、作成して Resources フォルダに配置してください。", MessageType.Error);
                return;
            }

            _serializedObject.Update();

            GUILayout.Label("Tag Key Registry", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("登録されているタグキーと、その許容値を定義します。\n許容値（AllowedValues）が空の場合は、どんな値（Value）も許可されます。", MessageType.Info);

            GUILayout.Space(10);

            // リストの描画
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_entriesProperty, new GUIContent("Registered Tags"), true);
            if (EditorGUI.EndChangeCheck())
            {
                _serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_serializedObject.targetObject);
            }
        }

        public override void OnDisable()
        {
            if (_serializedObject != null)
            {
                _serializedObject.Dispose();
                _serializedObject = null;
            }
        }
    }
}
