using System;
using UnityEngine;
namespace StoneGame.Economy
{
    [CreateAssetMenu(menuName="Stone Game/Economy Settings",fileName="EconomySettings")]
    public sealed class EconomySettings : ScriptableObject
    {
        [Min(0)] public long startingBalance=10000;
        void OnValidate(){startingBalance=Math.Max(0,startingBalance);}
    }
}
