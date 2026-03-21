using UnityEngine;
using UnityEditor;

namespace FlipLogic.Editor.DesignerPanel
{
    public abstract class DesignerPanelTab
    {
        protected DesignerPanelWindow Window { get; private set; }

        public abstract string TabName { get; }

        public virtual void OnEnable(DesignerPanelWindow window)
        {
            Window = window;
        }

        public virtual void OnDisable() { }

        public abstract void OnGUI();
        
        /// <summary>GUI内のスタイル初期化等、GUIループ内で必要な初期化</summary>
        public virtual void InitializeOnGUI() { }
    }
}
