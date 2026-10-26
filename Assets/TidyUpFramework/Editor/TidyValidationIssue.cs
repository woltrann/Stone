using System;
using UnityEngine;

namespace TidyUpFramework.Editor
{
    public enum TidyIssueSeverity
    {
        Info,
        Warning,
        Error
    }

    public sealed class TidyValidationIssue
    {
        public TidyValidationIssue(TidyIssueSeverity severity, string message, UnityEngine.Object context = null, string fixLabel = null, Action fix = null)
        {
            Severity = severity;
            Message = message;
            Context = context;
            FixLabel = fixLabel;
            Fix = fix;
        }

        public TidyIssueSeverity Severity { get; }
        public string Message { get; }
        public UnityEngine.Object Context { get; }
        public string FixLabel { get; }
        public Action Fix { get; }
        public bool CanFix => Fix != null;
    }
}
