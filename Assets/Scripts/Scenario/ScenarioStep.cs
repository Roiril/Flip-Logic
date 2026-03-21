using System;

namespace FlipLogic.Scenario
{
    /// <summary>
    /// シナリオステップの定義。トリガー条件とアクションのペア。
    /// 人間が「条件→何をするか」をリスト形式で定義する最小単位。
    /// </summary>
    [Serializable]
    public class ScenarioStep
    {
        /// <summary>ステップの識別名（デバッグ用）。</summary>
        public string StepId;

        /// <summary>この手順が実行される条件。</summary>
        public ScenarioTrigger Trigger;

        /// <summary>トリガーの補助パラメータ（エンティティ名、座標"x,y"等）。</summary>
        public string TriggerParam;

        /// <summary>アクション実行後にクリックを待機するかどうか。</summary>
        public bool WaitClickAfterAction;

        /// <summary>条件成立時に実行するアクション。</summary>
        public ScenarioAction Action;

        /// <summary>アクションの補助パラメータ（メッセージ本文、座標等）。</summary>
        public string ActionParam;

        /// <summary>追加アクションの補助パラメータ（エンティティ名等）。</summary>
        public string ActionParam2;
    }

    /// <summary>シナリオステップのトリガー条件。</summary>
    public enum ScenarioTrigger
    {
        Immediate,       // 即座に実行
        Click,           // プレイヤーのクリック/キー待ち
        TurnEnd,         // ターン終了時
        EntityDied,      // 指定エンティティが死亡
        EntityOnTile,    // 指定エンティティが指定マスに到達
        BattleStart,     // バトル開始
        BattleEnd,       // バトル終了
        FlagSet,         // 指定フラグがセットされた
        TurnCount,       // 指定ターン数経過
    }

    /// <summary>シナリオステップで実行するアクション。</summary>
    public enum ScenarioAction
    {
        ShowMessage,     // メッセージを表示（ActionParam = テキスト）
        HideMessage,     // メッセージパネルを非表示
        SpawnEnemy,      // 敵をスポーン（ActionParam = "x,y", ActionParam2 = エンティティ名）
        DespawnEntity,   // エンティティを除去（ActionParam = エンティティ名）
        SetFlag,         // フラグをセット（ActionParam = フラグ名）
        ClearFlag,       // フラグをクリア（ActionParam = フラグ名）
        AllowMovement,   // プレイヤー移動を許可
        BlockMovement,   // プレイヤー移動を禁止
        AddTileTag,      // マスにタグを付与（ActionParam = "x,y", ActionParam2 = "Key:Value"）
        WaitTurns,       // 指定ターン数待機（ActionParam = ターン数）
        EndScenario,     // シナリオ終了
        ForceMoveEntity  // エンティティを強制移動（ActionParam = "エンティティ名;x,y"）
    }
}
