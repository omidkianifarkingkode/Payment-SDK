using System;
using System.Collections.Generic;
using GamePaymentSDK.Core;
using UnityEngine;

namespace GamePaymentSDK.Samples
{
    public sealed class PaymentProductListUI : MonoBehaviour
    {
        [SerializeField] private Transform _contentRoot;
        [SerializeField] private PaymentProductButton _buttonPrefab;

        private readonly List<PaymentProductButton> _buttons = new();

        public void BindProducts(
            IReadOnlyCollection<PaymentProduct> products,
            Action<string> onProductClicked
        )
        {
            Clear();

            if (products == null)
                return;

            foreach (PaymentProduct product in products)
            {
                PaymentProductButton button = Instantiate(_buttonPrefab, _contentRoot);
                button.Initialize(product, onProductClicked);
                _buttons.Add(button);
            }
        }

        public void SetInteractable(bool interactable)
        {
            foreach (PaymentProductButton button in _buttons)
            {
                if (button != null)
                    button.SetInteractable(interactable);
            }
        }

        public void Clear()
        {
            foreach (PaymentProductButton button in _buttons)
            {
                if (button != null)
                    Destroy(button.gameObject);
            }

            _buttons.Clear();
        }
    }
}