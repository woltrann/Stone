using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using StoneGame.Cutting;

public static class StoneProbabilityChecks
{
    public static object Main()
    {
        var originalRandom = UnityEngine.Random.state;
        var profile = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<StoneCutProfile>("Assets/STONE/Data/CutProfiles/HamTas.asset"));
        profile.hideFlags = HideFlags.HideAndDontSave;
        var ores = profile.OreChances.Select(e => e.ore).ToArray();
        var passed = new List<string>();
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); passed.Add(label); }
        void Set(string field, object value) => typeof(StoneCutProfile).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(profile, value);
        bool RollEmpty(int n) { for(int i=0;i<n;i++) if(!profile.TryRollOre(out var ore) || ore != null) return false; return true; }
        bool RollFull(int n) { for(int i=0;i<n;i++) if(!profile.TryRollOre(out var ore) || ore == null) return false; return true; }
        try
        {
            UnityEngine.Random.InitState(87361);
            // Test a known distribution on the temporary clone, independent of Inspector tuning.
            var weights = new[] {.5, .25, .20, .05};
            if (ores.Length != weights.Length) throw new Exception("This fixture requires four ore definitions.");
            Set("containsOrePercent",5f);
            Set("oreChances",ores.Select((o,i)=>new OreChanceEntry {ore=o,chancePercent=(float)(weights[i]*100)}).ToList());
            const int samples = 200000;
            var counts = new Dictionary<string,int>();
            int empty = 0;
            for(int i=0;i<samples;i++)
            {
                if (!profile.TryRollOre(out var ore)) throw new Exception("Valid profile rejected");
                if(ore == null) empty++; else { counts.TryGetValue(ore.name,out var count); counts[ore.name] = count+1; }
            }
            Check(Math.Abs(empty - samples * .95) < 6 * Math.Sqrt(samples * .95 * .05), "5% ore: empty frequency within 6 sigma");
            for(int i=0;i<ores.Length;i++)
            {
                var p=.05 * weights[i];
                Check(Math.Abs(counts[ores[i].name] - samples*p) < 6*Math.Sqrt(samples*p*(1-p)), ores[i].name+" weighted frequency within 6 sigma");
            }
            Set("containsOrePercent",0f); Set("oreChances",new List<OreChanceEntry>());
            Check(RollEmpty(10000), "0% with empty list: always empty");
            Set("containsOrePercent",100f);
            Check(!profile.ValidateDistribution(out _), "100% with empty list: rejected");
            Set("oreChances",new List<OreChanceEntry>{new OreChanceEntry{ore=ores[0],chancePercent=100f}});
            Check(RollFull(10000), "100% single entry: always ore");
            Set("oreChances",ores.Select((o,i)=>new OreChanceEntry {ore=o,chancePercent=(float)(weights[i]*100)}).ToList());
            Check(RollFull(10000), "100% multiple entries: always ore");
            foreach(var invalid in new[] {99f,101f,-1f,float.NaN,float.PositiveInfinity})
            {
                Set("oreChances",new List<OreChanceEntry>{new OreChanceEntry{ore=ores[0],chancePercent=invalid}});
                Check(!profile.ValidateDistribution(out _), "Invalid weight rejected: "+invalid);
            }
            Set("oreChances",new List<OreChanceEntry>{new OreChanceEntry{ore=null,chancePercent=100f}});
            Check(!profile.ValidateDistribution(out _), "Positive weight without ore: rejected");
            Set("oreChances",new List<OreChanceEntry>{new OreChanceEntry{ore=ores[0],chancePercent=50f},new OreChanceEntry{ore=ores[0],chancePercent=50f}});
            Check(!profile.ValidateDistribution(out _), "Duplicate ore: rejected");
            Set("containsOrePercent",float.NaN);
            Check(!profile.ValidateDistribution(out _), "Invalid first-stage probability: rejected");
            return new { passed, samples, empty, ores=counts };
        }
        finally { UnityEngine.Random.state=originalRandom; UnityEngine.Object.DestroyImmediate(profile); }
    }
}
