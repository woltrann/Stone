using System;
using System.Collections.Generic;
using UnityEngine;
namespace StoneGame.Cutting
{
    // Only references to materials used for cut faces; no material content or ore configuration is copied.
    public sealed class StoneCutMaterialReferences:ScriptableObject
    {
        [Serializable]public sealed class Entry{public string id;public Material material;}
        [SerializeField]List<Entry> entries=new List<Entry>();
        public IReadOnlyList<Entry> Entries=>entries;
        static StoneCutMaterialReferences cached;
        static StoneCutMaterialReferences Library=>cached!=null?cached:(cached=Resources.Load<StoneCutMaterialReferences>("TableCutMaterials"));
        public static string IdFor(Material material)
        {if(material==null || Library==null)return null;foreach(var e in Library.entries)if(e.material==material)return e.id;return null;}
        public static Material Resolve(string id)
        {if(string.IsNullOrEmpty(id) || Library==null)return null;foreach(var e in Library.entries)if(e.id==id)return e.material;return null;}
        public static Material ResolveLegacyName(string name)
        {
            if(string.IsNullOrEmpty(name) || Library==null)return null;Material found=null;
            foreach(var e in Library.entries)if(e.material!=null && e.material.name==name){if(found!=null && found!=e.material)return null;found=e.material;}return found;
        }
        public bool Register(string id,Material material)
        {
            foreach(var e in entries)if(e.id==id){if(e.material==material)return false;e.material=material;return true;}
            entries.Add(new Entry{id=id,material=material});return true;
        }
    }
}
