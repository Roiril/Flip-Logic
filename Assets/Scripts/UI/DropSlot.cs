using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace FlipLogic.UI
{
    public class DropSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private Image _image;
        private Outline _outline;
        private Color _originalOutlineColor;

        public DraggablePart CurrentPart { get; private set; }

        private void Awake()
        {
            _image = GetComponent<Image>();
            _outline = GetComponent<Outline>();
            if (_outline != null) _originalOutlineColor = _outline.effectColor;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerDrag != null)
            {
                var part = eventData.pointerDrag.GetComponent<DraggablePart>();
                if (part != null) SetHighlight(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlight(false);
        }

        private void SetHighlight(bool highlight)
        {
            if (_outline != null)
                _outline.effectColor = highlight ? Color.cyan : _originalOutlineColor;
            
            if (_image != null)
                _image.color = highlight ? new Color(0.2f, 0.25f, 0.25f) : new Color(0.15f, 0.15f, 0.15f);
        }

        public void OnDrop(PointerEventData eventData)
        {
            SetHighlight(false);

            if (eventData.pointerDrag != null)
            {
                DraggablePart newPart = eventData.pointerDrag.GetComponent<DraggablePart>();
                if (newPart != null)
                {
                    // 既にパーツがある場合は入れ替える
                    if (CurrentPart != null && CurrentPart != newPart)
                    {
                        // 既存パーツをドロップされたパーツの元の位置に戻す
                        CurrentPart.ReturnToOriginalPosition();
                    }

                    // スロットのど真ん中に確実に吸着させる
                    newPart.transform.SetParent(transform);
                    newPart.transform.localPosition = Vector3.zero;
                    CurrentPart = newPart;
                }
            }
        }

        public void Clear()
        {
            CurrentPart = null;
        }
    }
}
