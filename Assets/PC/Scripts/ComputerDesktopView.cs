using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StoneGame.Computers
{
    [DisallowMultipleComponent]
    public sealed class ComputerDesktopView : MonoBehaviour
    {
        [SerializeField] Canvas desktopCanvas;
        [SerializeField] CanvasGroup interactionGroup;
        [SerializeField] Button buyButton;
        [SerializeField] Button ordersButton;
        [SerializeField] Button exitButton;
        [SerializeField] TMP_FontAsset interfaceFont;
        InteractiveComputer owner;
        public Canvas Canvas=>desktopCanvas;
        public CanvasGroup InteractionGroup=>interactionGroup;
        public Button BuyButton=>buyButton;
        public Button OrdersButton=>ordersButton;
        public Button ExitButton=>exitButton;
        public TMP_FontAsset InterfaceFont=>interfaceFont;
        public bool IsInteractive=>interactionGroup!=null && interactionGroup.interactable;
        void Awake()
        {
            buyButton.onClick.AddListener(Buy);ordersButton.onClick.AddListener(Orders);exitButton.onClick.AddListener(Exit);
            SetInteractive(false);
        }
        public void Bind(InteractiveComputer computer,Camera camera) { owner=computer;desktopCanvas.worldCamera=camera;SetInteractive(false); }
        public void SetInteractive(bool active)
        {
            if(interactionGroup!=null) {interactionGroup.interactable=active;interactionGroup.blocksRaycasts=active;}
        }
        void Buy() { if(IsInteractive && owner!=null) owner.RequestBuy(); }
        void Orders() { if(IsInteractive && owner!=null) owner.RequestOrders(); }
        void Exit() { if(IsInteractive && owner!=null) owner.TryExit(); }
        void OnDestroy()
        {
            if(buyButton!=null) buyButton.onClick.RemoveListener(Buy);
            if(ordersButton!=null) ordersButton.onClick.RemoveListener(Orders);
            if(exitButton!=null) exitButton.onClick.RemoveListener(Exit);
        }
    }
}
