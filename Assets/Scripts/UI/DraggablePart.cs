using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace FlipLogic.UI
{
    public class DraggablePart : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private string _content;
        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private Image _image;
        
        private Vector3 _originalPosition;
        private Vector3 _originalLocalPosition;
        private Transform _originalParent;
        private Transform _canvasTransform;
        private Canvas _canvas;
        private bool _isDragging;

        public string Content => _content;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
            _canvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            var text = GetComponentInChildren<Text>();
            if (text != null) _content = text.text;
            
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas != null) _canvasTransform = _canvas.transform;
            else _canvasTransform = transform.root;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isDragging) return;
            SetHighlight(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isDragging) return;
            SetHighlight(false);
        }

        private void SetHighlight(bool highlight)
        {
            if (_image != null) 
                _image.color = highlight ? new Color(0.95f, 0.95f, 0.95f) : Color.white;
            
            transform.localScale = highlight ? Vector3.one * 1.05f : Vector3.one;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            StopAllCoroutines(); // スナップバック中なら停止

            // 所属スロットの参照クリア
            var slot = GetComponentInParent<DropSlot>();
            if (slot != null && slot.CurrentPart == this) slot.Clear();

            _originalPosition = _rectTransform.position;
            _originalLocalPosition = _rectTransform.localPosition;
            _originalParent = transform.parent;
            
            _canvasGroup.alpha = 0.7f;
            _canvasGroup.blocksRaycasts = false;
            
            transform.SetParent(_canvasTransform);
            transform.SetAsLastSibling();
            
            SetHighlight(true);
            transform.localScale = Vector3.one * 1.1f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                _rectTransform.position = eventData.position;
            }
            else
            {
                Vector3 worldPoint;
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_rectTransform, eventData.position, _canvas.worldCamera, out worldPoint))
                {
                    _rectTransform.position = worldPoint;
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;

            // どこにもドロップされなかった（または親がCanvasのまま）場合は元の場所へ戻す
            if (transform.parent == _canvasTransform)
            {
                ReturnToOriginalPosition();
            }
            else
            {
                // ドロップ成功時の見た目リセット
                SetHighlight(false);
            }
        }

        public void ReturnToOriginalPosition()
        {
            StartCoroutine(AnimateReturn(_originalParent, _originalLocalPosition));
        }

        private System.Collections.IEnumerator AnimateReturn(Transform targetParent, Vector3 targetLocalPos)
        {
            transform.SetParent(targetParent);
            Vector3 startPos = _rectTransform.localPosition;
            float elapsed = 0;
            float duration = 0.15f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Ease Out Quad
                t = t * (2 - t);
                _rectTransform.localPosition = Vector3.Lerp(startPos, targetLocalPos, t);
                yield return null;
            }

            _rectTransform.localPosition = targetLocalPos;
            SetHighlight(false);
        }
    }
}
