using System;
using System.Collections.Generic;
using StoneGame.Cutting;
using TidyUpFramework.Objects;
using UnityEngine;

namespace StoneGame.Shop
{
    [Serializable]
    public sealed class StoneProduct
    {
        [Tooltip("Stable save identifier. Do not change after orders have been placed.")] public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite image;
        [Min(0)] public long unitPrice=100;
        public GameObject prefab;
        public TidyObjectDefinition definition;
        public bool active=true;

        public bool Validate(out string error)
        {
            error=null;
            if(string.IsNullOrWhiteSpace(id) || id!=id.Trim()) error="Ürünün benzersiz ID alanı boş veya kenarlarında boşluk var.";
            else if(string.IsNullOrWhiteSpace(displayName)) error="Ürün adı eksik.";
            else if(unitPrice<0) error="Ürün fiyatı negatif olamaz.";
            else if(prefab==null || definition==null) error="Taş prefabı veya TidyObjectDefinition eksik.";
            else
            {
                var tidy=prefab.GetComponent<TidyObject>();var raw=prefab.GetComponent<RawStone>();
                if(tidy==null || tidy.Definition!=definition) error="Prefab kökündeki TidyObject Definition ile katalog Definition eşleşmiyor.";
                else if(raw==null || raw.CutProfile==null) error="Prefab kökünde RawStone veya StoneCutProfile eksik.";
                else if(prefab.GetComponent<Rigidbody>()==null || prefab.GetComponentInChildren<Collider>()==null) error="Taş prefabında Rigidbody veya Collider eksik.";
                else if(!raw.CutProfile.ValidateCut(out var profileError)) error="Taş kesme profili geçersiz: "+profileError;
            }
            return error==null;
        }
    }

    [CreateAssetMenu(menuName="Stone Game/Stone Market Catalog",fileName="StoneMarketCatalog")]
    public sealed class StoneMarketCatalog : ScriptableObject
    {
        [SerializeField] List<StoneProduct> products=new List<StoneProduct>();
        [SerializeField,Min(1)] int maximumOrderQuantity=999;
        public IReadOnlyList<StoneProduct> Products=>products;
        public int MaximumOrderQuantity=>Mathf.Max(1,maximumOrderQuantity);
        public bool TryFind(string id,out StoneProduct product,out string error,bool requireActive=true)
        {
            product=null;error=null;
            foreach(var p in products)
            {
                if(p==null || !string.Equals(p.id,id,StringComparison.Ordinal))continue;
                if(product!=null){product=null;error="Katalogda aynı ürün ID'si birden fazla kez kullanılıyor: "+id;return false;}
                product=p;
            }
            if(product==null){error="Ürün katalogda bulunamadı: "+id;return false;}
            if(requireActive && !product.active){error="Bu ürün şu anda satışta değil.";return false;}
            return product.Validate(out error);
        }
        public List<string> ValidateCatalog()
        {
            var errors=new List<string>();var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var p in products)
            {
                if(p==null){errors.Add("Boş ürün satırı var.");continue;}
                if(!ids.Add(p.id??string.Empty))errors.Add("Tekrarlanan ürün ID: "+p.id);
                if(!p.Validate(out var error))errors.Add((p.displayName??p.id)+": "+error);
            }
            return errors;
        }
        void OnValidate()
        {
            maximumOrderQuantity=Mathf.Max(1,maximumOrderQuantity);
            foreach(var p in products)if(p!=null)p.unitPrice=Math.Max(0,p.unitPrice);
            foreach(var error in ValidateCatalog())Debug.LogWarning("[Stone Market] "+error,this);
        }
    }
}
