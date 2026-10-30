using System;
using System.Collections.Generic;
using StoneGame.Economy;
using StoneGame.Orders;
using StoneGame.Shop;
using TidyUpFramework.Core;
using TMPro;
using UnityEngine;
namespace StoneGame.Computers.UI
{
    [DisallowMultipleComponent]
    public sealed class StoneMarketView : MonoBehaviour
    {
        [SerializeField] StoneMarketCatalog catalog;
        [SerializeField] StoneProductCard cardPrefab;
        [SerializeField] GameObject shopPage,ordersPage;
        [SerializeField] RectTransform productsContent,ordersContent;
        [SerializeField] UnityEngine.UI.ScrollRect productsScroll,ordersScroll;
        [SerializeField] TMP_Text balanceLabel,selectedName,descriptionLabel,unitPriceLabel,totalLabel,quantityHint,emptyOrdersLabel;
        [SerializeField] TMP_InputField quantityInput;
        [SerializeField] UnityEngine.UI.Button createOrderButton;
        [SerializeField] GameObject notificationPanel;
        [SerializeField] CanvasGroup notificationGroup;
        [SerializeField] UnityEngine.UI.Image notificationBackground;
        [SerializeField] TMP_Text notificationText;
        [SerializeField] TMP_Text orderRowPrefab;
        [SerializeField] UnityEngine.UI.Button shopTab,ordersTab;
        InteractiveComputer computer;
        EconomyService economy;
        StoneProduct selected;
        string requestId;
        long quotedPrice;
        bool submitted,subscribed;
        float notificationUntil;
        readonly List<StoneProductCard> cards=new List<StoneProductCard>();
        public StoneMarketCatalog Catalog=>catalog;
        public IReadOnlyList<StoneProductCard> Cards=>cards;
        public TMP_InputField QuantityInput=>quantityInput;
        public UnityEngine.UI.Button CreateOrderButton=>createOrderButton;
        public string TotalText=>totalLabel.text;
        public string NotificationText=>notificationText.text;
        public bool NotificationVisible=>notificationPanel.activeSelf;
        public bool OrdersVisible=>ordersPage.activeSelf;
        public int VisibleOrderCount=>ordersContent.childCount;
        public EconomyService Economy=>economy;
        void Start()
        {
            computer=GetComponentInParent<InteractiveComputer>();
            if(computer==null){enabled=false;Debug.LogWarning("[Stone Market] PC bağlantısı bulunamadı.",this);return;}
            computer.BuyRequested+=OpenShop;computer.OrdersRequested+=OpenOrders;computer.StateChanged+=ComputerStateChanged;
            createOrderButton.onClick.AddListener(Submit);quantityInput.onValueChanged.AddListener(QuantityChanged);
            TidyServices.ServiceChanged+=ServiceChanged;subscribed=true;
            ConnectEconomy();OpenShop();ClearSelection();HideNotification();
        }
        void ServiceChanged(Type type){if(type==typeof(EconomyService))ConnectEconomy();}
        void ConnectEconomy()
        {
            if(economy!=null){economy.BalanceChanged-=BalanceChanged;economy.OrdersChanged-=RefreshOrders;}
            TidyServices.TryGet(out economy);
            if(economy!=null){economy.BalanceChanged+=BalanceChanged;economy.OrdersChanged+=RefreshOrders;}
            RefreshBalance();UpdateQuote();
        }
        public void RebuildProducts()
        {
            foreach(var card in cards)if(card!=null){card.gameObject.SetActive(false);Destroy(card.gameObject);}cards.Clear();
            if(catalog!=null)foreach(var product in catalog.Products)
            {
                if(product==null || !product.active)continue;
                bool valid=catalog.TryFind(product.id,out _,out _);
                var card=Instantiate(cardPrefab,productsContent);card.gameObject.SetActive(true);card.Bind(product,SelectProduct,valid);cards.Add(card);
            }
            productsScroll.verticalNormalizedPosition=1;
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(productsContent);
        }
        void RefreshBalance(){balanceLabel.text=economy!=null && economy.IsReady?MarketAmounts.Money(economy.Balance):"Kayıt kullanılamıyor";}
        void BalanceChanged(long _){RefreshBalance();UpdateQuote();}
        public void OpenShop()
        {
            shopPage.SetActive(true);ordersPage.SetActive(false);StyleTabs(true);
            // Catalog edits/new entries are reflected each time the shop opens.
            RebuildProducts();
        }
        public void OpenOrders(){shopPage.SetActive(false);ordersPage.SetActive(true);ClearSelection();RefreshOrders();StyleTabs(false);}
        void StyleTabs(bool shop)
        {shopTab.targetGraphic.color=shop?new Color(.16f,.48f,.4f):new Color(.1f,.17f,.21f);ordersTab.targetGraphic.color=!shop?new Color(.16f,.48f,.4f):new Color(.1f,.17f,.21f);}
        public void SelectProduct(StoneProduct product)
        {
            if(computer==null || computer.State!=ComputerUseState.Using)return;
            if(catalog==null || product==null || !catalog.TryFind(product.id,out var current,out var error))
            {Notify("Eksik veya geçersiz ürün verisi. Kataloğu kontrol edin.",false);return;}
            selected=current;quotedPrice=current.unitPrice;requestId=Guid.NewGuid().ToString("N");submitted=false;
            selectedName.text=current.displayName;descriptionLabel.text=current.description;unitPriceLabel.text="Birim fiyat  "+MarketAmounts.Money(quotedPrice);
            quantityInput.interactable=true;quantityInput.SetTextWithoutNotify("1");UpdateQuote();
        }
        void ClearSelection()
        {
            selected=null;requestId=null;submitted=false;
            selectedName.text="Taş seçin";descriptionLabel.text="Sipariş oluşturmak için mağazadan bir ürün seçin.";unitPriceLabel.text="Birim fiyat  —";
            quantityInput.DeactivateInputField();quantityInput.SetTextWithoutNotify("1");quantityInput.interactable=false;UpdateQuote();
        }
        void QuantityChanged(string _){UpdateQuote();}
        public void UpdateQuote()
        {
            int count=0;
            bool valid=selected!=null && catalog!=null && MarketAmounts.TryQuantity(quantityInput.text,catalog.MaximumOrderQuantity,out count);
            long total=0;
            if(valid)valid=MarketAmounts.TryTotal(quotedPrice,count,out total);
            totalLabel.text=valid?MarketAmounts.Money(total):"—";
            quantityHint.text=selected==null?"":valid?"En fazla "+catalog.MaximumOrderQuantity+" adet":
                "Geçersiz adet. 1–"+catalog.MaximumOrderQuantity+" arasında tam sayı girin.";
            quantityHint.color=valid?new Color(.64f,.74f,.76f):new Color(1,.48f,.44f);
            // Leave the button available for invalid input to show an explicit error notification on click.
            createOrderButton.interactable=selected!=null && !submitted && economy!=null && economy.IsReady;
        }
        public void Submit()
        {
            if(computer==null || computer.State!=ComputerUseState.Using || submitted)return;
            if(selected==null || catalog==null){Notify("Eksik ürün verisi. Önce bir taş seçin.",false);return;}
            if(!MarketAmounts.TryQuantity(quantityInput.text,catalog.MaximumOrderQuantity,out int count))
            {Notify("Geçersiz adet. 1–"+catalog.MaximumOrderQuantity+" arasında tam sayı girin.",false);return;}
            if(economy==null || !economy.IsReady){Notify(economy?.LoadError??"Para sistemi hazır değil.",false);return;}
            quantityInput.DeactivateInputField();
            if(!economy.TryPlaceOrder(catalog,selected.id,count,quotedPrice,requestId,out _,out var error)){Notify(error,false);return;}
            submitted=true;createOrderButton.interactable=false;
            Notify("Siparişiniz başarıyla oluşturuldu!",true);RefreshBalance();RefreshOrders();
        }
        public void RefreshOrders()
        {
            foreach(Transform child in ordersContent){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var history=economy?.Orders;
            emptyOrdersLabel.gameObject.SetActive(history==null || history.Count==0);
            if(history!=null)for(int i=history.Count-1;i>=0;i--)
            {
                var order=history[i];var row=Instantiate(orderRowPrefab,ordersContent);row.gameObject.SetActive(true);
                row.text="<color=#6CD9B9>#"+order.OrderId.Substring(0,8).ToUpperInvariant()+"</color>   "+TMP_TextUtilitiesToPlain(order.ProductName)+
                    "   <color=#B8C6CE>×"+order.Quantity+"</color>\n"+MarketAmounts.Money(order.TotalPaid)+"    •    "+StatusText(order.Status);
            }
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(ordersContent);ordersScroll.verticalNormalizedPosition=1;
        }
        static string TMP_TextUtilitiesToPlain(string text)=>(text??string.Empty).Replace("<","‹").Replace(">","›");
        public static string StatusText(StoneOrderStatus status)
        {switch(status){case StoneOrderStatus.Pending:return "Bekliyor";case StoneOrderStatus.Processing:return "Hazırlanıyor";case StoneOrderStatus.Delivered:return "Teslim edildi";case StoneOrderStatus.Cancelled:return "İptal edildi";default:return "Bilinmiyor";}}
        void Notify(string message,bool success)
        {
            notificationText.text=message;notificationBackground.color=success?new Color(.07f,.29f,.22f):new Color(.38f,.09f,.1f);
            notificationGroup.alpha=1;notificationPanel.SetActive(true);notificationUntil=Time.unscaledTime+4;
        }
        void HideNotification(){notificationUntil=0;notificationPanel.SetActive(false);notificationGroup.alpha=0;}
        void Update()
        {
            if(notificationUntil<=0)return;
            float remaining=notificationUntil-Time.unscaledTime;
            if(remaining<=0)HideNotification();else notificationGroup.alpha=Mathf.Clamp01(remaining/.25f);
        }
        void ComputerStateChanged(ComputerUseState state)
        {
            if(state==ComputerUseState.Using){RebuildProducts();RefreshBalance();UpdateQuote();return;}
            HideNotification();ClearSelection();
        }
        void OnDisable(){if(notificationPanel!=null)HideNotification();}
        void OnDestroy()
        {
            if(!subscribed)return;
            TidyServices.ServiceChanged-=ServiceChanged;
            if(computer!=null){computer.BuyRequested-=OpenShop;computer.OrdersRequested-=OpenOrders;computer.StateChanged-=ComputerStateChanged;}
            if(economy!=null){economy.BalanceChanged-=BalanceChanged;economy.OrdersChanged-=RefreshOrders;}
            if(createOrderButton!=null)createOrderButton.onClick.RemoveListener(Submit);
            if(quantityInput!=null)quantityInput.onValueChanged.RemoveListener(QuantityChanged);
        }
    }
}
