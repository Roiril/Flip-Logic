using UnityEngine;
using UnityEditor;

namespace FlipLogic.Editor.DesignerPanel
{
    public class DesignerPanelWindow : EditorWindow
    {
        private DesignerPanelTab[] _tabs;
        private string[] _tabNames;
        private int _currentTabIndex = 0;
        private Vector2 _scrollPosition;

        [MenuItem("Window/Flip Logic/Designer Panel")]
        public static void ShowWindow()
        {
            var window = GetWindow<DesignerPanelWindow>("Designer Panel");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnEnable()
        {
            // タブのインスタンス化
            _tabs = new DesignerPanelTab[]
            {
                new MapEditorTab(),
                new TagRegistryTab(),
                new RuleEditorTab(),
                new EnemyEditorTab(),
                new StageEditorTab(),
                new DebugTab()
            };

            _tabNames = new string[_tabs.Length];
            for (int i = 0; i < _tabs.Length; i++)
            {
                _tabs[i].OnEnable(this);
                _tabNames[i] = _tabs[i].TabName;
            }
        }

        private void OnDisable()
        {
            if (_tabs != null)
            {
                foreach (var tab in _tabs)
                {
                    tab?.OnDisable();
                }
            }
        }

        private void OnGUI()
        {
            if (_tabs == null || _tabs.Length == 0) return;

            // 各タブのGUI初期化
            foreach (var tab in _tabs)
            {
                tab.InitializeOnGUI();
            }

            // ヘッダー（タブ切り替え）
            GUILayout.Space(10);
            _currentTabIndex = GUILayout.Toolbar(_currentTabIndex, _tabNames, GUILayout.Height(30));
            GUILayout.Space(10);

            // 区切り線
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            GUILayout.Space(10);

            // コンテンツエリア
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            {
                _tabs[_currentTabIndex].OnGUI();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
