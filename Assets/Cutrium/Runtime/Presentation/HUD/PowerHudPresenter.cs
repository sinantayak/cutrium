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

        [SerializeField]
        private GameObject _gravityWellRoot;

        [SerializeField]
        private Button _gravityWellButton;

        [SerializeField]
        private Text _gravityWellChargesText;

        private bool _freezeButtonSubscribed;
        private bool _instantButtonSubscribed;
        private bool _gravityButtonSubscribed;

        public FirstPlayableController Controller => _controller;

        public GameObject FreezePulseRoot => _freezePulseRoot;

        public Button FreezePulseButton => _freezePulseButton;

        public Text FreezePulseChargesText => _freezePulseChargesText;

        public GameObject InstantBarrierRoot => _instantBarrierRoot;

        public Button InstantBarrierButton => _instantBarrierButton;

        public Text InstantBarrierChargesText => _instantBarrierChargesText;

        public GameObject GravityWellRoot => _gravityWellRoot;

        public Button GravityWellButton => _gravityWellButton;

        public Text GravityWellChargesText => _gravityWellChargesText;

        public void Configure(
            FirstPlayableController controller,
            GameObject freezePulseRoot,
            Button freezePulseButton,
            Text freezePulseChargesText,
            GameObject instantBarrierRoot,
            Button instantBarrierButton,
            Text instantBarrierChargesText)
        {
            Configure(
                controller,
                freezePulseRoot,
                freezePulseButton,
                freezePulseChargesText,
                instantBarrierRoot,
                instantBarrierButton,
                instantBarrierChargesText,
                null,
                null,
                null);
        }

        public void Configure(
            FirstPlayableController controller,
            GameObject freezePulseRoot,
            Button freezePulseButton,
            Text freezePulseChargesText,
            GameObject instantBarrierRoot,
            Button instantBarrierButton,
            Text instantBarrierChargesText,
            GameObject gravityWellRoot,
            Button gravityWellButton,
            Text gravityWellChargesText)
        {
            UnsubscribeButtons();
            _controller = controller;
            _freezePulseRoot = freezePulseRoot;
            _freezePulseButton = freezePulseButton;
            _freezePulseChargesText = freezePulseChargesText;
            _instantBarrierRoot = instantBarrierRoot;
            _instantBarrierButton = instantBarrierButton;
            _instantBarrierChargesText = instantBarrierChargesText;
            _gravityWellRoot = gravityWellRoot;
            _gravityWellButton = gravityWellButton;
            _gravityWellChargesText = gravityWellChargesText;
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
            // already disables its button through the same charge-gated
            // path used once charges run out mid-level -- one rule instead
            // of a second visibility rule to keep in sync, and a stable HUD
            // that never appears or disappears.
            int freezeChargesRemaining = _controller.FreezePulseChargesRemaining;
            if (_freezePulseButton != null)
            {
                _freezePulseButton.interactable = freezeChargesRemaining > 0;
            }

            if (_freezePulseChargesText != null)
            {
                _freezePulseChargesText.text = freezeChargesRemaining > 0
                    ? freezeChargesRemaining.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    : string.Empty;
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
                _instantBarrierChargesText.text = instantChargesRemaining > 0
                    ? instantChargesRemaining.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    : string.Empty;
            }


            int gravityChargesRemaining =
                _controller.GravityWellChargesRemaining;
            if (_gravityWellButton != null)
            {
                _gravityWellButton.interactable =
                    _controller.GravityWellTargeting
                    || (gravityChargesRemaining > 0
                        && !_controller.GravityWellActive);
                if (_gravityWellButton.targetGraphic != null)
                {
                    _gravityWellButton.targetGraphic.color =
                        _controller.GravityWellTargeting
                            ? new Color(1f, 0.72f, 1f, 1f)
                            : Color.white;
                }
            }

            if (_gravityWellChargesText != null)
            {
                _gravityWellChargesText.text = gravityChargesRemaining > 0
                    ? gravityChargesRemaining.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    : string.Empty;
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

            if (!_gravityButtonSubscribed && _gravityWellButton != null)
            {
                _gravityWellButton.onClick.AddListener(
                    OnGravityWellClicked);
                _gravityButtonSubscribed = true;
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

            if (_gravityButtonSubscribed && _gravityWellButton != null)
            {
                _gravityWellButton.onClick.RemoveListener(
                    OnGravityWellClicked);
            }

            _freezeButtonSubscribed = false;
            _instantButtonSubscribed = false;
            _gravityButtonSubscribed = false;
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

        private void OnGravityWellClicked()
        {
            _controller.ToggleGravityWellTargeting();
            RefreshNow();
        }
    }
}
