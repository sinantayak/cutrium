using Cutrium.Unity.Input;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.HUD
{
    [DisallowMultipleComponent]
    public sealed class DebugPointerStatusView : MonoBehaviour
    {
        [SerializeField]
        private PointerInputAdapter _pointerInput;

        [SerializeField]
        private Text _pointerStatus;

        [SerializeField]
        private Text _mappingStatus;

        public PointerInputAdapter PointerInput => _pointerInput;

        public Text PointerStatus => _pointerStatus;

        public Text MappingStatus => _mappingStatus;

        public void Configure(
            PointerInputAdapter pointerInput,
            Text pointerStatus,
            Text mappingStatus)
        {
            _pointerInput = pointerInput;
            _pointerStatus = pointerStatus;
            _mappingStatus = mappingStatus;
        }

        private void OnEnable()
        {
            if (_pointerInput != null)
            {
                _pointerInput.Sampled += OnPointerSampled;
            }

            SetInitialText();
        }

        private void OnDisable()
        {
            if (_pointerInput != null)
            {
                _pointerInput.Sampled -= OnPointerSampled;
            }
        }

        private void OnPointerSampled(PointerSample sample)
        {
            if (_pointerStatus != null)
            {
                _pointerStatus.text =
                    $"Pointer: {sample.Phase}  id={sample.PointerId}  " +
                    $"start blocked={sample.StartedOverUi}";
            }

            if (_mappingStatus != null)
            {
                _mappingStatus.text = sample.IsInsideBoard
                    ? $"Board: inside  logical=({sample.LogicalPoint.X:0.00}, " +
                      $"{sample.LogicalPoint.Y:0.00})"
                    : "Board: outside — presentation margin";
            }
        }

        private void SetInitialText()
        {
            if (_pointerStatus != null)
            {
                _pointerStatus.text = "Pointer: waiting";
            }

            if (_mappingStatus != null)
            {
                _mappingStatus.text =
                    "Board: move or press to inspect 10 × 16 mapping";
            }
        }
    }
}
