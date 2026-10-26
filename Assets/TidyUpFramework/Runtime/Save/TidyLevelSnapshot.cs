using System;
using System.Collections.Generic;
using UnityEngine;

namespace TidyUpFramework.Save
{
    [Serializable]
    public sealed class TidyObjectSnapshot
    {
        public string Id;
        public int State;
        public string TargetId;
        public int TargetOrder = -1;
        public bool HasPose = true;
        public Vector3 Position;
        public Quaternion Rotation = Quaternion.identity;
        public bool PickedUp;
        public int Mistakes;
        public int CarryOrder = -1;
    }

    [Serializable]
    public sealed class TidyCleanableSnapshot
    {
        public string Id;
        public float Dirt;
    }

    [Serializable]
    public sealed class TidyLevelSnapshot
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public string LevelId;
        public string SceneName;
        public string SavedAtUtc;
        public float Progress;
        public float ElapsedSeconds;
        public int Score;
        public int CorrectPlacements;
        public int Mistakes;
        public int HintsUsed;
        public int CleanedSurfaces;
        public int DisposedClutter;
        public int BestCombo;
        public bool HasPlayer;
        public Vector3 PlayerPosition;
        public float PlayerYaw;
        public float PlayerPitch;
        public List<TidyObjectSnapshot> Objects = new List<TidyObjectSnapshot>();
        public List<TidyCleanableSnapshot> Cleanables = new List<TidyCleanableSnapshot>();
        public List<string> ScoredObjects = new List<string>();

        public DateTime SavedAt => DateTime.TryParse(SavedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var time) ? time : DateTime.MinValue;
    }
}
