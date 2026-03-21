using UnityEngine;
using FlipLogic.Rulebook;

namespace FlipLogic.Data
{
    /// <summary>
    /// ステージのメタ情報を保持する ScriptableObject。
    /// マップの構成データ（壁や敵の座標）は持たず、Sceneの配置コンポーネントに委ねる。
    /// </summary>
    [CreateAssetMenu(fileName = "NewStageConfig", menuName = "FlipLogic/Stage Config")]
    public class StageConfig : ScriptableObject
    {
        [Header("ステージ情報")]
        public string StageName;
        [TextArea(2, 4)]
        public string Description;

        [Header("ルール設定")]
        [Tooltip("このステージで使用するルールブック")]
        public RulebookAsset Rulebook;

        [Header("マップ設定")]
        [Tooltip("マップの推奨サイズ（ProceduralMapGenerator等が参照）")]
        public Vector2Int RecommendedMapSize = new Vector2Int(10, 8);

        [Header("マップ配置")]
        [Tooltip("マップエディタで配置した敵やギミック情報")]
        public MapPlacementData PlacementData;

        [Header("メタデータ")]
        [Tooltip("ステージの難易度（表示用）")]
        [Range(1, 10)]
        public int Difficulty = 1;

#if UNITY_EDITOR
        [Header("エディタ専用")]
        [Tooltip("プレイテスト用のシーンアセット。Designer Panelから即時ロードに使用します。")]
        public UnityEditor.SceneAsset SceneAsset;
#endif
    }
}
