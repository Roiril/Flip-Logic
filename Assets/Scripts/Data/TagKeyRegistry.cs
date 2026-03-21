using System.Collections.Generic;
using UnityEngine;
using System;

namespace FlipLogic.Data
{
    [Serializable]
    public class TagKeyEntry
    {
        public string Key;
        public List<string> AllowedValues = new List<string>();
    }

    /// <summary>
    /// タグのKeyと許容されるValueの一覧を管理するマスタデータ。
    /// マジック文字列によるタイプミスを防ぎ、デザイナーがタグを定義できるようにする。
    /// </summary>
    [CreateAssetMenu(fileName = "TagKeyRegistry", menuName = "FlipLogic/TagKeyRegistry")]
    public class TagKeyRegistry : ScriptableObject
    {
        [SerializeField]
        private List<TagKeyEntry> _entries = new List<TagKeyEntry>();

        public IReadOnlyList<TagKeyEntry> Entries => _entries;

        private static TagKeyRegistry _instance;
        public static TagKeyRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<TagKeyRegistry>("TagKeyRegistry");
                    if (_instance == null)
                    {
                        Debug.LogWarning("[TagKeyRegistry] Resources/TagKeyRegistry が見つかりませんでした。");
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 指定されたKeyとValueがマスタに存在するか検証する。
        /// </summary>
        public bool IsValid(string key, string value)
        {
            var entry = _entries.Find(e => e.Key == key);
            if (entry == null) return false;
            if (entry.AllowedValues.Count == 0) return true; // 空の場合は任意の値（EncounterMsgなど）を許容
            return entry.AllowedValues.Contains(value);
        }
    }
}
