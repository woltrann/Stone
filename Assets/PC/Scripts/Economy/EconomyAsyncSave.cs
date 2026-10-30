using System;
using System.Diagnostics;
using System.Threading.Tasks;
namespace StoneGame.Economy
{
    public sealed class EconomySaveOperation
    {
        public bool IsCompleted{get;internal set;}
        public bool Success{get;internal set;}
        public string Error{get;internal set;}
        internal static EconomySaveOperation Completed(bool success,string error=null)=>new EconomySaveOperation{IsCompleted=true,Success=success,Error=error};
    }
    public sealed partial class EconomyService
    {
        sealed class WriteResult{public bool success;public string error;public double milliseconds;}
        sealed class PendingWrite
        {public EconomySnapshot snapshot;public bool notifyOrders;public EconomySaveOperation operation;public Task<WriteResult> task;}
        PendingWrite pendingWrite;
        public bool HasPendingWrite=>pendingWrite!=null;
        public double LastBackgroundWriteMilliseconds{get;private set;}
        EconomySaveOperation BeginBackgroundCommit(EconomySnapshot next,bool notifyOrders=false)
        {
            if(committing)return EconomySaveOperation.Completed(false,"Başka bir kayıt işlemi devam ediyor.");
            var operation=new EconomySaveOperation();committing=true;
            var pending=new PendingWrite{snapshot=next,notifyOrders=notifyOrders,operation=operation};pendingWrite=pending;
            pending.task=Task.Run(()=>
            {
                var watch=Stopwatch.StartNew();var result=new WriteResult();
                try{result.success=store.TryWrite(Encode(next),out result.error);}catch(Exception ex){result.error="Kayıt tamamlanamadı: "+ex.Message;}
                result.milliseconds=watch.Elapsed.TotalMilliseconds;return result;
            });
            return operation;
        }
        // Called on the Unity thread. Only detached DTO encoding, hashing and disk I/O run on the worker.
        public void PumpWrites(){CompletePendingWrite(false);}
        public void FlushPendingWrites(){CompletePendingWrite(true);}
        void CompletePendingWrite(bool wait)
        {
            var pending=pendingWrite;if(pending==null || (!wait && !pending.task.IsCompleted))return;
            var result=pending.task.GetAwaiter().GetResult();pendingWrite=null;
            try
            {
                LastBackgroundWriteMilliseconds=result.milliseconds;
                if(result.success)Publish(pending.snapshot,null,pending.notifyOrders);
                pending.operation.Success=result.success;pending.operation.Error=result.error;
            }
            finally{committing=false;pending.operation.IsCompleted=true;}
        }
    }
}
