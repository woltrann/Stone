using System;
using System.Collections.Generic;
using StoneGame.Delivery;
using StoneGame.Orders;
namespace StoneGame.Economy
{
    public sealed partial class EconomyService
    {
        public List<StoneDeliveryItem> GetDeliveryItems(string sceneName)
        {
            var list=new List<StoneDeliveryItem>();if(state==null)return list;
            foreach(var item in state.deliveryItems)if(item.sceneName==sceneName)list.Add(item.Clone());return list;
        }
        public StoneOrder FindOrder(string id)=>state?.orders.Find(o=>o.OrderId==id);
        public StoneDeliveryItem GetDeliveryItem(string orderId,int index)=>state?.deliveryItems.Find(p=>p.orderId==orderId && p.index==index)?.Clone();
        public bool TryBeginDelivery(string id,string sceneName,out string error)
        {
            if(!BuildBeginDelivery(id,sceneName,out var next,out error))return false;
            return next==null || Commit(next,null,out error);
        }
        public EconomySaveOperation BeginDeliveryAsync(string id,string sceneName)
        {
            if(!BuildBeginDelivery(id,sceneName,out var next,out var error))return EconomySaveOperation.Completed(false,error);
            return next==null?EconomySaveOperation.Completed(true):BeginBackgroundCommit(next,true);
        }
        bool BuildBeginDelivery(string id,string sceneName,out EconomySnapshot next,out string error)
        {
            next=null;if(!Available(out error))return false;
            var order=state.orders.Find(o=>o.OrderId==id);
            if(order==null || string.IsNullOrEmpty(sceneName) || (order.Status!=StoneOrderStatus.Pending && order.Status!=StoneOrderStatus.Processing))
            {error="Sipariş teslimata uygun değil.";return false;}
            if(!string.IsNullOrEmpty(order.DeliveryScene) && order.DeliveryScene!=sceneName){error="Sipariş başka bir sahnenin teslimatına bağlı.";return false;}
            if(order.Status==StoneOrderStatus.Processing && order.DeliveryScene==sceneName)return true;
            next=Copy();order=next.orders.Find(o=>o.OrderId==id);order.SetDeliveryScene(sceneName);order.SetStatus(StoneOrderStatus.Processing);return true;
        }
        public bool TryPrepareDeliveryItem(StoneDeliveryItem item,out string error)
        {
            if(!BuildPreparedItem(item,out var next,out error))return false;
            return next==null || Commit(next,null,out error,false);
        }
        public EconomySaveOperation PrepareDeliveryItemAsync(StoneDeliveryItem item)
        {
            if(!BuildPreparedItem(item,out var next,out var error))return EconomySaveOperation.Completed(false,error);
            return next==null?EconomySaveOperation.Completed(true):BeginBackgroundCommit(next);
        }
        bool BuildPreparedItem(StoneDeliveryItem item,out EconomySnapshot next,out string error)
        {
            next=null;if(!Available(out error))return false;
            var order=FindOrder(item?.orderId);
            if(item==null || order==null || order.Status!=StoneOrderStatus.Processing || !item.Valid(order))
            {error="Taş teslimat kimliği veya kayıt verisi geçersiz.";return false;}
            foreach(var prior in state.deliveryItems)if(prior.orderId==item.orderId && prior.index==item.index)
            {error=null;return true;}
            // Reserve only the next ordinal. A durable reservation can be materialized after a crash.
            for(int i=0;i<item.index;i++)if(!state.deliveryItems.Exists(p=>p.orderId==item.orderId && p.index==i && p.confirmed))
            {error="Önceki taşın oluşturulması henüz doğrulanmadı.";return false;}
            next=Copy();var reservation=item.Clone();reservation.confirmed=false;next.deliveryItems.Add(reservation);return true;
        }
        public bool TryConfirmDeliveryItem(string orderId,int index,out string error)
        {
            if(!BuildConfirmedItem(orderId,index,out var next,out error))return false;
            return next==null || Commit(next,null,out error,false);
        }
        public EconomySaveOperation ConfirmDeliveryItemAsync(string orderId,int index)
        {
            if(!BuildConfirmedItem(orderId,index,out var next,out var error))return EconomySaveOperation.Completed(false,error);
            return next==null?EconomySaveOperation.Completed(true):BeginBackgroundCommit(next);
        }
        bool BuildConfirmedItem(string orderId,int index,out EconomySnapshot next,out string error)
        {
            next=null;if(!Available(out error))return false;
            var item=state.deliveryItems.Find(p=>p.orderId==orderId && p.index==index);
            if(item==null){error="Taş oluşturma kaydı bulunamadı.";return false;}
            if(item.confirmed)return true;
            next=Copy();next.deliveryItems.Find(p=>p.orderId==orderId && p.index==index).confirmed=true;return true;
        }
        public bool TryCheckpointDeliveryItems(List<StoneDeliveryItem> items,out string error)
        {
            if(!BuildCheckpoint(items,out var next,out error))return false;
            return next==null || Commit(next,null,out error,false);
        }
        public EconomySaveOperation CheckpointDeliveryItemsAsync(List<StoneDeliveryItem> items)
        {
            if(!BuildCheckpoint(items,out var next,out var error))return EconomySaveOperation.Completed(false,error);
            return next==null?EconomySaveOperation.Completed(true):BeginBackgroundCommit(next);
        }
        bool BuildCheckpoint(List<StoneDeliveryItem> items,out EconomySnapshot next,out string error)
        {
            next=null;if(!Available(out error))return false;
            foreach(var update in items)
            {
                var order=state.orders.Find(o=>o.OrderId==update.orderId);
                if(!update.Valid(order)){error="Teslimat dünya kaydı geçersiz.";return false;}
                int at=state.deliveryItems.FindIndex(p=>p.orderId==update.orderId && p.index==update.index && p.sceneName==update.sceneName);
                if(at<0)continue;
                if(update.SameWorldState(state.deliveryItems[at]))continue;
                if(next==null)next=Copy();var copy=update.Clone();copy.confirmed=state.deliveryItems[at].confirmed;next.deliveryItems[at]=copy;
            }
            return true;
        }
        bool AllDeliveryItemsConfirmed(StoneOrder order)
        {
            for(int i=0;i<order.Quantity;i++)if(!state.deliveryItems.Exists(p=>p.orderId==order.OrderId && p.index==i && p.confirmed))return false;
            return true;
        }
        static void ValidateDeliveryData(EconomySnapshot loaded)
        {
            // Existing version-1 wallet/order files have no delivery field yet.
            if(loaded.deliveryItems==null)loaded.deliveryItems=new List<StoneDeliveryItem>();
            var ids=new HashSet<string>();foreach(var item in loaded.deliveryItems)
                if(item==null || !item.Valid(loaded.orders.Find(o=>o.OrderId==item.orderId)) || !ids.Add(item.orderId+":"+item.index))
                    throw new FormatException("Geçersiz veya tekrarlanan teslimat taşı kaydı.");
        }
    }
}
