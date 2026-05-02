using System;
using GamePaymentSDK.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GamePaymentSDK.Samples
{
    public sealed class PaymentProductButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _priceText;

        private string _productKey;
        private Action<string> _onClicked;

        public void Initialize(PaymentProduct product, Action<string> onClicked)
        {
            if (product == null)
            {
                gameObject.SetActive(false);
                return;
            }

            _productKey = product.ProductKey;
            _onClicked = onClicked;

            if (_titleText != null)
                _titleText.text = product.Name;

            if (_priceText != null)
                _priceText.text = product.GetLocalizedPriceString();

            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClicked);
                _button.onClick.AddListener(HandleClicked);
                _button.interactable = true;
            }

            gameObject.SetActive(true);
        }

        public void SetInteractable(bool interactable)
        {
            if (_button != null)
                _button.interactable = interactable;
        }

        private void HandleClicked()
        {
            if (string.IsNullOrWhiteSpace(_productKey))
                return;

            _onClicked?.Invoke(_productKey);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClicked);
        }
    }
}