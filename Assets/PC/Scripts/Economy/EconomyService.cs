using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using StoneGame.Orders;
using StoneGame.Shop;
using UnityEngine;

namespace StoneGame.Economy
{
    public static class MarketAmounts
    {
        public static bool TryQuantity(string text,int maximum,out int quantity)
        {
            quantity=0;if(string.IsNullOrEmpty(text) || text.Length>10)return false;
            foreach(char c in text)if(c<'0'||c>'9')return false;
            return int.TryParse(text,NumberStyles.None,CultureInfo.InvariantCulture,out quantity) && quantity>0 && quantity<=maximum;
        }
        public static bool TryTotal(long unitPrice,int quantity,out long total)
        {total=0;if(unitPrice<0 || quantity<=0)return false;try{total=checked(unitPrice*quantity);return true;}catch(OverflowException){return false;}}
        public static string Money(long amount)=>amount.ToString("N0",CultureInfo.GetCultureInfo("tr-TR"))+" TL";
    }
    [Serializable] sealed class EconomySnapshot
    {
        public int version=1;
        public long balance;
        public List<StoneOrder> orders=new List<StoneOrder>();
        public List<StoneGame.Delivery.StoneDeliveryItem> deliveryItems=new List<StoneGame.Delivery.StoneDeliveryItem>();
    }
    [Serializable] sealed class EconomyEnvelope {public string payload,sha256;}
    public sealed partial class EconomyService
    {
        readonly IEconomyStore store;
        EconomySnapshot state;
        bool committing;
        public bool IsReady=>state!=null;
        public string LoadError{get;private set;}
        public long Balance=>state!=null?state.balance:0;
        public IReadOnlyList<StoneOrder> Orders=>state!=null?state.orders.AsReadOnly():Array.Empty<StoneOrder>();
        public event Action<long> BalanceChanged;
        public event Action<StoneOrder> OrderPlaced;
        public event Action OrdersChanged;
        public event Action NewGameStarted;
        public EconomyService(IEconomyStore storage,long startingBalance)
        {
            store=storage??throw new ArgumentNullException(nameof(storage));
            if(!store.TryRead(out var json,out var error)){LoadError=error;return;}
            if(json==null)
            {
                if(startingBalance<0){LoadError="Başlangıç bakiyesi negatif olamaz.";return;}
                var initial=new EconomySnapshot{balance=startingBalance};
                if(!store.TryWrite(Encode(initial),out error)){LoadError=error;return;}
                state=initial;return;
            }
            try
            {
                var envelope=JsonUtility.FromJson<EconomyEnvelope>(json);
                if(envelope==null || string.IsNullOrEmpty(envelope.payload) || Hash(envelope.payload)!=envelope.sha256)
                    throw new FormatException("Kayıt bütünlük kontrolü başarısız.");
                var loaded=JsonUtility.FromJson<EconomySnapshot>(envelope.payload);
                if(loaded==null || loaded.version!=1 || loaded.balance<0 || loaded.orders==null)throw new FormatException("Kayıt biçimi veya bakiye geçersiz.");
                var ids=new HashSet<string>();var requests=new HashSet<string>();
                foreach(var order in loaded.orders)if(order==null || !order.IsValid() || !ids.Add(order.OrderId) || !requests.Add(order.RequestId))throw new FormatException("Geçersiz veya tekrarlanan sipariş.");
                ValidateDeliveryData(loaded);
                state=loaded;
            }
            catch(Exception ex){LoadError="Ekonomi kaydı geçersiz; bakiye sıfırlanmadı: "+ex.Message;}
        }
        public bool CanAfford(long amount)=>IsReady && amount>=0 && state.balance>=amount;
        public bool TryAddMoney(long amount,out string error)
        {
            if(!Available(out error))return false;
            if(amount<=0 || amount>long.MaxValue-state.balance){error="Eklenecek para geçersiz veya bakiye sınırını aşıyor.";return false;}
            var next=Copy();next.balance+=amount;return Commit(next,null,out error);
        }
        public bool TrySpend(long amount,out string error)
        {
            if(!Available(out error))return false;
            if(amount<0){error="Negatif harcama yapılamaz.";return false;}
            if(!CanAfford(amount)){error=InsufficientFunds;return false;}
            if(amount==0)return true;
            var next=Copy();next.balance-=amount;return Commit(next,null,out error);
        }
        public const string InsufficientFunds="Yetersiz bakiye! Siparişi oluşturmak için yeterli paranız bulunmuyor.";
        public bool TryPlaceOrder(StoneMarketCatalog catalog,string productId,int quantity,long quotedPrice,string requestId,out StoneOrder order,out string error)
        {
            order=null;if(!Available(out error))return false;
            if(!Guid.TryParseExact(requestId,"N",out _)){error="Sipariş istek kimliği geçersiz.";return false;}
            // Durable idempotency: replaying the same successful request never charges twice, even after loading.
            foreach(var existing in state.orders)if(existing.RequestId==requestId)
            {
                if(existing.ProductId!=productId || existing.Quantity!=quantity || existing.UnitPrice!=quotedPrice){error="Aynı istek kimliği farklı sipariş için kullanılamaz.";return false;}
                order=existing;return true;
            }
            if(catalog==null){error="Ürün kataloğu eksik.";return false;}
            if(!catalog.TryFind(productId,out var product,out error))return false;
            if(quantity<=0 || quantity>catalog.MaximumOrderQuantity){error="Geçersiz adet. 1–"+catalog.MaximumOrderQuantity+" arasında tam sayı girin.";return false;}
            if(product.unitPrice!=quotedPrice){error="Ürün fiyatı değişti. Ürünü yeniden seçin.";return false;}
            if(!MarketAmounts.TryTotal(product.unitPrice,quantity,out long total)){error="Sipariş toplamı sayı sınırını aşıyor.";return false;}
            if(!CanAfford(total)){error=InsufficientFunds;return false;}
            var next=Copy();var created=new StoneOrder(requestId,product.id,product.displayName,quantity,product.unitPrice,total);
            next.balance-=total;next.orders.Add(created);
            if(!Commit(next,created,out error))return false;
            order=created;return true;
        }
        public bool TrySetOrderStatus(string orderId,StoneOrderStatus status,out string error)
        {
            if(!BuildStatusChange(orderId,status,out var next,out error))return false;
            return next==null || Commit(next,null,out error);
        }
        public EconomySaveOperation SetOrderStatusAsync(string orderId,StoneOrderStatus status)
        {
            if(!BuildStatusChange(orderId,status,out var next,out var error))return EconomySaveOperation.Completed(false,error);
            return next==null?EconomySaveOperation.Completed(true):BeginBackgroundCommit(next,true);
        }
        bool BuildStatusChange(string orderId,StoneOrderStatus status,out EconomySnapshot next,out string error)
        {
            next=null;if(!Available(out error))return false;
            var order=state.orders.Find(o=>o.OrderId==orderId);
            if(order==null || !Enum.IsDefined(typeof(StoneOrderStatus),status)){error="Sipariş veya durum geçersiz.";return false;}
            // Cancellation only changes delivery state; refunds belong to a future explicit transaction.
            if(order.Status==status)return true;
            bool allowed=order.Status==StoneOrderStatus.Pending && (status==StoneOrderStatus.Processing || status==StoneOrderStatus.Cancelled) ||
                order.Status==StoneOrderStatus.Processing && (status==StoneOrderStatus.Delivered || status==StoneOrderStatus.Cancelled);
            if(!allowed){error="Bu sipariş durum geçişine izin verilmiyor.";return false;}
            if(status==StoneOrderStatus.Delivered && !AllDeliveryItemsConfirmed(order))
            {error="Siparişteki bütün taşlar oluşturulmadan teslim tamamlanamaz.";return false;}
            next=Copy();next.orders.Find(o=>o.OrderId==orderId).SetStatus(status);return true;
        }
        public bool StartNewGame(long initialBalance,out string error)
        {
            FlushPendingWrites();
            error=null;if(committing || initialBalance<0){error="Yeni oyun başlangıç bakiyesi geçersiz veya başka işlem sürüyor.";return false;}
            if(!Commit(new EconomySnapshot{balance=initialBalance},null,out error))return false;
            if(NewGameStarted!=null)foreach(Action listener in NewGameStarted.GetInvocationList())try{listener();}catch(Exception ex){Debug.LogException(ex);}
            return true;
        }
        bool Available(out string error){FlushPendingWrites();error=LoadError;if(!IsReady){error=error??"Para sistemi henüz hazır değil.";return false;}if(committing){error="Başka bir ödeme işlemi devam ediyor.";return false;}error=null;return true;}
        EconomySnapshot Copy()
        {
            var copy=new EconomySnapshot{version=state.version,balance=state.balance};
            foreach(var order in state.orders)copy.orders.Add(order.Clone());
            foreach(var item in state.deliveryItems)copy.deliveryItems.Add(item.Clone());return copy;
        }
        bool Commit(EconomySnapshot next,StoneOrder order,out string error,bool notifyOrders=true)
        {
            error=null;if(committing){error="Başka bir işlem devam ediyor.";return false;}
            committing=true;
            try
            {
                if(!store.TryWrite(Encode(next),out error))return false;
                Publish(next,order,notifyOrders);
                return true;
            }
            finally{committing=false;}
        }
        void Publish(EconomySnapshot next,StoneOrder order,bool notifyOrders)
        {
            long previous=Balance;state=next;LoadError=null;
            if(previous!=Balance)Notify(BalanceChanged,Balance);
            if(order!=null)Notify(OrderPlaced,order);
            if(notifyOrders && OrdersChanged!=null)foreach(Action listener in OrdersChanged.GetInvocationList())try{listener();}catch(Exception ex){Debug.LogException(ex);}
        }
        static void Notify<T>(Action<T> action,T value){if(action!=null)foreach(Action<T> listener in action.GetInvocationList())try{listener(value);}catch(Exception ex){Debug.LogException(ex);}}
        static string Encode(EconomySnapshot snapshot)
        {var payload=JsonUtility.ToJson(snapshot);return JsonUtility.ToJson(new EconomyEnvelope{payload=payload,sha256=Hash(payload)},true);}
        static string Hash(string text)
        =>EconomySaveHash.Compute(text);
    }
}
