using System;
using UnityEngine;
namespace StoneGame.Orders
{
    public enum StoneOrderStatus { Pending,Processing,Delivered,Cancelled }
    [Serializable]
    public sealed class StoneOrder
    {
        [SerializeField] string orderId,requestId,productId,productName,createdAtUtc;
        [SerializeField] string deliveryScene;
        [SerializeField] int quantity;
        [SerializeField] long unitPrice,totalPaid;
        [SerializeField] StoneOrderStatus status;
        public string OrderId=>orderId;
        public string RequestId=>requestId;
        public string ProductId=>productId;
        public string ProductName=>productName;
        public string CreatedAtUtc=>createdAtUtc;
        public int Quantity=>quantity;
        public long UnitPrice=>unitPrice;
        public long TotalPaid=>totalPaid;
        public StoneOrderStatus Status=>status;
        public string DeliveryScene=>deliveryScene;
        internal void SetDeliveryScene(string scene){deliveryScene=scene;}
        public StoneOrder(string request,string product,string name,int count,long price,long total)
        {orderId=Guid.NewGuid().ToString("N");requestId=request;productId=product;productName=name;quantity=count;unitPrice=price;totalPaid=total;createdAtUtc=DateTime.UtcNow.ToString("O");status=StoneOrderStatus.Pending;}
        internal void SetStatus(StoneOrderStatus value){status=value;}
        internal StoneOrder Clone()=>(StoneOrder)MemberwiseClone();
        public string ItemInstanceId(int index)
        {if(index<0 || index>=quantity)throw new ArgumentOutOfRangeException(nameof(index));return "market_"+orderId+"_"+index;}
        internal bool IsValid()
        {
            return Guid.TryParseExact(orderId,"N",out _) && Guid.TryParseExact(requestId,"N",out _) && !string.IsNullOrWhiteSpace(productId) &&
                !string.IsNullOrWhiteSpace(productName) && quantity>0 && unitPrice>=0 && MarketTotalValid() &&
                DateTimeOffset.TryParse(createdAtUtc,out _) && Enum.IsDefined(typeof(StoneOrderStatus),status);
        }
        bool MarketTotalValid(){try{return checked(unitPrice*quantity)==totalPaid;}catch(OverflowException){return false;}}
    }
}
