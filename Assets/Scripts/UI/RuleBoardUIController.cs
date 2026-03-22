using System;
using System.Collections.Generic;
using UnityEngine;
using FlipLogic.Core;
using FlipLogic.Data;

namespace FlipLogic.UI
{
    /// <summary>
    /// フィールド常設のルールボードUI制御。
    /// 全アクティブルールの表示と改変を行う。
    /// </summary>
    public class RuleBoardUIController : MonoBehaviour
    {
        public static RuleBoardUIController Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject _boardPanel;
        [SerializeField] private Transform _contentRoot;
        [SerializeField] private RuleRowUI _rowPrefab;

        private readonly List<RuleRowUI> _activeRows = new List<RuleRowUI>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (_boardPanel != null) _boardPanel.SetActive(false);
        }

        /// <summary>ルールボードを開く。</summary>
        public void OpenBoard()
        {
            gameObject.SetActive(true);
            if (_boardPanel != null) _boardPanel.SetActive(true);
            transform.SetAsLastSibling(); // 最前面に持ってくる
            RefreshRules();
        }

        /// <summary>ルールボードを閉じる。</summary>
        public void CloseBoard()
        {
            if (_boardPanel != null) _boardPanel.SetActive(false);
            gameObject.SetActive(false);
        }

        /// <summary>現在のルール一覧を表示する。</summary>
        public void RefreshRules()
        {
            // 既存の行をクリア
            foreach (var row in _activeRows)
            {
                Destroy(row.gameObject);
            }
            _activeRows.Clear();

            // GameManagerからアクティブなルールを取得
            if (GameManager.Instance == null || GameManager.Instance.RulebookData == null) return;

            var activeRules = GameManager.Instance.RulebookData.GetActiveRules();
            foreach (var rule in activeRules)
            {
                if (_rowPrefab == null) break;
                
                var row = Instantiate(_rowPrefab, _contentRoot);
                row.Setup(rule);
                row.gameObject.SetActive(true);
                _activeRows.Add(row);
            }
        }
    }
}
