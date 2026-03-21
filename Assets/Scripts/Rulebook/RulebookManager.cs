using System.Collections.Generic;
using FlipLogic.Data;

namespace FlipLogic.Rulebook
{
    /// <summary>
    /// ルールブック管理。ページ（章）概念による段階的ルール解放と、
    /// ルール改変能力の管理を行う。
    /// </summary>
    public class RulebookManager
    {
        private readonly List<RulePage> _pages = new List<RulePage>();
        private readonly List<RuleData> _allRules = new List<RuleData>();
        private int _unlockedChapter;

        /// <summary>全ルール（取得済み）。</summary>
        public IReadOnlyList<RuleData> AllRules => _allRules;

        /// <summary>全ページ。</summary>
        public IReadOnlyList<RulePage> Pages => _pages;

        /// <summary>現在解放済みの最大章番号。</summary>
        public int UnlockedChapter => _unlockedChapter;

        /// <summary>ルール数。</summary>
        public int Count => _allRules.Count;

        /// <summary>RulebookAsset からデータを読み込み、初期化する。</summary>
        public void LoadFromAsset(RulebookAsset asset)
        {
            _pages.Clear();
            _allRules.Clear();
            _unlockedChapter = 1; // ひとまず1章は解放済みとする

            if (asset == null) return;

            foreach (var pageAsset in asset.Pages)
            {
                if (pageAsset != null)
                {
                    AddPage(pageAsset.CreateRulePage());
                }
            }
        }

        /// <summary>ページを追加する。</summary>
        public void AddPage(RulePage page)
        {
            _pages.Add(page);
            foreach (var rule in page.Rules)
            {
                if (!_allRules.Exists(r => r.RuleId == rule.RuleId))
                {
                    rule.ValidateTags(); // 追加時にバリデーションを実行
                    _allRules.Add(rule);
                }
            }
        }

        /// <summary>章を解放する。その章以下のルールが全て利用可能になる。</summary>
        public void UnlockChapter(int chapter)
        {
            if (chapter > _unlockedChapter)
                _unlockedChapter = chapter;

            foreach (var rule in _allRules)
            {
                if (rule.Chapter <= _unlockedChapter)
                    rule.IsActive = true;
            }
        }

        /// <summary>ルールを1つ追加する（ページ外の個別追加）。</summary>
        public void AddRule(RuleData rule)
        {
            if (!_allRules.Exists(r => r.RuleId == rule.RuleId))
                _allRules.Add(rule);
        }

        /// <summary>IDでルールを取得する。</summary>
        public RuleData GetRule(string ruleId)
        {
            return _allRules.Find(r => r.RuleId == ruleId);
        }

        /// <summary>現在アクティブなルール一覧を取得する。</summary>
        public List<RuleData> GetActiveRules()
        {
            return _allRules.FindAll(r => r.IsActive && r.Chapter <= _unlockedChapter);
        }

        /// <summary>全ルールの改変状態をリセットする（セーフティネット機能）。</summary>
        public void ResetAllRules()
        {
            foreach (var rule in _allRules)
            {
                rule.ResetAll();
            }
        }
    }

    /// <summary>
    /// ルールブックのページ（章）。テーマごとにルールをグループ化する。
    /// </summary>
    public class RulePage
    {
        /// <summary>章番号。</summary>
        public int Chapter;

        /// <summary>章タイトル（例: "状態異常の法則"）。</summary>
        public string Title;

        /// <summary>章の説明。</summary>
        public string Description;

        /// <summary>この章に含まれるルール。</summary>
        public List<RuleData> Rules = new List<RuleData>();
    }
}
