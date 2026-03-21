using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlipLogic.Core
{
    /// <summary>
    /// タグの集合を管理するコンテナ。
    /// GameEntityに付随し、タグのCRUDとターン経過処理を提供する。
    /// </summary>
    [Serializable]
    public class TagContainer
    {
        [SerializeField] private List<TagDefinition> _tags = new List<TagDefinition>();

        /// <summary>現在のタグ一覧（読み取り専用）。</summary>
        public IReadOnlyList<TagDefinition> Tags => _tags;

        /// <summary>タグ数。</summary>
        public int Count => _tags.Count;

        /// <summary>タグ変更時に発火するイベント。</summary>
        public event Action OnTagsChanged;

        /// <summary>タグを追加する。同一Key-Valueが既にある場合はDurationを上書き。</summary>
        public void AddTag(TagDefinition tag)
        {
            for (int i = 0; i < _tags.Count; i++)
            {
                if (_tags[i].Key == tag.Key && _tags[i].Value == tag.Value)
                {
                    // 既存タグのDurationを更新（より長い方を採用）
                    if (tag.Duration == -1 || (_tags[i].Duration != -1 && tag.Duration > _tags[i].Duration))
                    {
                        _tags[i] = tag;
                        OnTagsChanged?.Invoke();
                    }
                    return;
                }
            }
            _tags.Add(tag);
            OnTagsChanged?.Invoke();
        }

        /// <summary>Key-Valueが一致するタグを除去する。</summary>
        public bool RemoveTag(string key, string value)
        {
            for (int i = _tags.Count - 1; i >= 0; i--)
            {
                if (_tags[i].Key == key && _tags[i].Value == value)
                {
                    _tags.RemoveAt(i);
                    OnTagsChanged?.Invoke();
                    return true;
                }
            }
            return false;
        }

        /// <summary>指定Keyのタグを全て除去する。</summary>
        public int RemoveAllByKey(string key)
        {
            int removed = _tags.RemoveAll(t => t.Key == key);
            if (removed > 0) OnTagsChanged?.Invoke();
            return removed;
        }

        /// <summary>指定Key-Valueのタグを保持しているか。</summary>
        public bool HasTag(string key, string value)
        {
            for (int i = 0; i < _tags.Count; i++)
            {
                if (_tags[i].Key == key && _tags[i].Value == value)
                    return true;
            }
            return false;
        }

        /// <summary>指定Keyのタグを1つでも保持しているか。</summary>
        public bool HasKey(string key)
        {
            for (int i = 0; i < _tags.Count; i++)
            {
                if (_tags[i].Key == key)
                    return true;
            }
            return false;
        }

        /// <summary>指定Keyのタグの値を取得する。なければnull。</summary>
        public string GetValue(string key)
        {
            for (int i = 0; i < _tags.Count; i++)
            {
                if (_tags[i].Key == key)
                    return _tags[i].Value;
            }
            return null;
        }

        /// <summary>指定Keyの全タグを取得する。</summary>
        public List<TagDefinition> GetAllByKey(string key)
        {
            var result = new List<TagDefinition>();
            for (int i = 0; i < _tags.Count; i++)
            {
                if (_tags[i].Key == key)
                    result.Add(_tags[i]);
            }
            return result;
        }

        /// <summary>
        /// ターン経過処理。非永続タグのDurationを1減算し、0になったタグを除去する。
        /// </summary>
        /// <returns>除去されたタグのリスト。</returns>
        public List<TagDefinition> TickDurations()
        {
            var expired = new List<TagDefinition>();
            for (int i = _tags.Count - 1; i >= 0; i--)
            {
                var tag = _tags[i];
                if (tag.IsPermanent) continue;

                tag.Duration--;
                if (tag.Duration <= 0)
                {
                    expired.Add(tag);
                    _tags.RemoveAt(i);
                }
                else
                {
                    _tags[i] = tag;
                }
            }
            if (expired.Count > 0) OnTagsChanged?.Invoke();
            return expired;
        }

        /// <summary>全タグを除去する。</summary>
        public void Clear()
        {
            if (_tags.Count > 0)
            {
                _tags.Clear();
                OnTagsChanged?.Invoke();
            }
        }

        /// <summary>デバッグ用文字列表現。</summary>
        public override string ToString()
        {
            if (_tags.Count == 0) return "(no tags)";
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _tags.Count; i++)
            {
                if (i > 0) sb.Append(" ");
                sb.Append(_tags[i].ToString());
            }
            return sb.ToString();
        }
    }
}
