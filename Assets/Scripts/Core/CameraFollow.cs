using UnityEngine;

namespace FlipLogic.Core
{
    /// <summary>
    /// Orthographicカメラのプレイヤー追従。
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public static CameraFollow Instance { get; private set; }

        [SerializeField] private Transform _target;
        [SerializeField] private float _smoothSpeed = 5f;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0f, -10f);

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (_target == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) _target = player.transform;
            }
        }

        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        private void LateUpdate()
        {
            if (_target == null) return;
            var desired = _target.position + _offset;
            transform.position = Vector3.Lerp(transform.position, desired, _smoothSpeed * Time.deltaTime);
        }
    }
}
