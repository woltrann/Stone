using System;
using TMPro;
using UnityEngine;
using StoneGame.Economy;
using StoneGame.Shop;
namespace StoneGame.Computers.UI
{
    public sealed class StoneProductCard : MonoBehaviour
    {
        [SerializeField] TMP_Text nameLabel,priceLabel,placeholderLabel;
        [SerializeField] UnityEngine.UI.Image productImage;
        [SerializeField] UnityEngine.UI.Button selectButton;
        StoneProduct product;
        Action<StoneProduct> select;
        public StoneProduct Product=>product;
        public UnityEngine.UI.Button SelectButton=>selectButton;
        public UnityEngine.UI.Image ProductImage=>productImage;
        void Awake(){selectButton.onClick.AddListener(Selected);}
        public void Bind(StoneProduct value,Action<StoneProduct> callback,bool valid)
        {
            product=value;select=callback;nameLabel.text=value.displayName;
            priceLabel.text=MarketAmounts.Money(value.unitPrice);productImage.sprite=value.image;productImage.preserveAspect=true;
            productImage.enabled=value.image!=null;placeholderLabel.gameObject.SetActive(value.image==null);selectButton.interactable=valid;
        }
        void Selected(){select?.Invoke(product);}
        void OnDestroy(){if(selectButton!=null)selectButton.onClick.RemoveListener(Selected);}
    }
}
