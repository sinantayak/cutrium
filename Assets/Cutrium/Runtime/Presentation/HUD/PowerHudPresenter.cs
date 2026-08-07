using Cutrium.Unity.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace Cutrium.Presentation.HUD
{
    [DisallowMultipleComponent]
    public sealed class PowerHudPresenter : MonoBehaviour
    {
        [SerializeField]
        private FirstPlayableController _controller;

        [SerializeField]
        private GameObject _freezePulseRoot;

        [SerializeField]
        private Button _freezePulseButton;

        [SerializeField]
        private Text _freezePulseChargesText;

        [SerializeField]
        private GameObject _instantBarrierRoot;

        [SerializeField]
        private Button _instantBarrierButton;

        [SerializeField]
        private Text _instantBarrierChargesText;

        private bool _freezeButtonSubscribed;
        private bool _instantButtonSubscribed;

        public FirstPlayableController Controller => _controller;

        public GameObject FreezePulseRoot => _freezePulseRoot;

        public Button FreezePulseButton => _freezePulseButton;

        public Text FreezePulseChargesText => _freezePulseChargesText;

        public GameObject InstantBarrierRoot => _instantBarrierRoot;

        public Button InstantBarrierButton => _instantBarrierButton;

        public Text InstantBarrierChargesText => _instantBarrierChargesText;

        public void Configure(
            FirstPlayableController controller,
            GameObject freezePulseRoot,
            Button freezePulseButton,
            Text freezePulseChargesText,
            GameObject instantBarrierRoot,
            Button instantBarrierButton,
            Text instantBarrierChargesText)
        {
            UnsubscribeButtons();
            _controller = controller;
            _freezePulseRoot = freezePulseRoot;
            _freezePulseButton = freezePulseButton;
            _freezePulseChargesText = freezePulseChargesText;
            _instantBarrierRoot = instantBarrierRoot;
            _instantBarrierButton = instantBarrierButton;
            _instantBarrierChargesText = instantBarrierChargesText;
            if (isActiveAndEnabled && Application.isPlaying)
            {
                SubscribeButtons();
            }

            RefreshNow();
        }

        public void RefreshNow()
        {
            if (_controller == null || _controller.Session == null)
            {
                return;
            }

            // Both power roots stay active for every level. A level that
            // does not configure a power simply has zero charges, which
            // already disables its button and shows a neutral label through
            // the same charge-gated path used once charges run out mid-level
            // -- one rule instead of a second visibility rule to keep in
            // sync, and a stable HUD that never appears or disappears.
            int freezeChargesRemaining = _controller.FreezePulseChargesRemaining;
            if (_freezePulseButton != null)
            {
                _freezePulseButton.interactable = freezeChargesRemaining > 0;
            }

            if (_freezePulseChargesText != null)
            {
                _freezePulseChargesText.text =
                    _controller.FreezePulseRemainingSeconds > 0f
                        ? "ACTIVE"
                        : $"FREEZE x{freezeChargesRemaining}";
            }

            int instantChargesRemaining =
                _controller.InstantBarrierChargesRemaining;
            if (_instantBarrierButton != null)
            {
                _instantBarrierButton.interactable = instantChargesRemaining > 0
                    && !_controller.InstantBarrierArmed;
            }

            if (_instantBarrierChargesText != null)
            {
                _instantBarrierChargesText.text =
                    _controller.InstantBarrierArmed
                        ? "ARMED"
                        : $"INSTANT x{instantChargesRemaining}";
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                SubscribeButtons();
            }
        }

        private void OnDisable()
        {
            UnsubscribeButtons();
        }

        private void LateUpdate()
        {
            RefreshNow();
        }

        private void SubscribeButtons()
        {
            if (!_freezeButtonSubscribed && _freezePulseButton != null)
            {
                _freezePulseButton.onClick.AddListener(
                    OnFreezePulseClicked);
                _freezeButtonSubscribed = true;
            }

            if (!_instantButtonSubscribed && _instantBarrierButton != null)
            {
                _instantBarrierButton.onClick.AddListener(
                    OnInstantBarrierClicked);
                _instantButtonSubscribed = true;
            }
        }

        private void UnsubscribeButtons()
        {
            if (_freezeButtonSubscribed && _freezePulseButton != null)
            {
                _freezePulseButton.onClick.RemoveListener(
                    OnFreezePulseClicked);
            }

            if (_instantButtonSubscribed && _instantBarrierButton != null)
            {
                _instantBarrierButton.onClick.RemoveListener(
                    OnInstantBarrierClicked);
            }

            _freezeButtonSubscribed = false;
            _instantButtonSubscribed = false;
        }

        private void OnFreezePulseClicked()
        {
            _controller.TryActivateFreezePulse();
            RefreshNow();
        }

        private void OnInstantBarrierClicked()
        {
            _controller.TryArmInstantBarrier();
            RefreshNow();
        }
    }
}
