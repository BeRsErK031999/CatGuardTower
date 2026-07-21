using System;
using System.Collections.Generic;
using System.Linq;

namespace CatGuard.EditorTools.MapAuthoring
{
    public enum MapAuthoringIssueSeverity
    {
        Warning,
        Error
    }

    public sealed class MapAuthoringValidationIssue
    {
        public MapAuthoringValidationIssue(
            MapAuthoringIssueSeverity severity,
            string code,
            string assetPath,
            string context,
            string message)
        {
            Severity = severity;
            Code = code ?? string.Empty;
            AssetPath = string.IsNullOrWhiteSpace(assetPath) ? "<unsaved asset>" : assetPath;
            Context = context ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public MapAuthoringIssueSeverity Severity { get; }
        public string Code { get; }
        public string AssetPath { get; }
        public string Context { get; }
        public string Message { get; }

        public override string ToString()
        {
            var contextSuffix = string.IsNullOrWhiteSpace(Context) ? string.Empty : $" ({Context})";
            return $"[{Severity}] [{Code}] {AssetPath}{contextSuffix}: {Message}";
        }
    }

    public sealed class MapAuthoringValidationResult
    {
        private readonly List<MapAuthoringValidationIssue> issues = new();

        public IReadOnlyList<MapAuthoringValidationIssue> Issues => issues;
        public IEnumerable<MapAuthoringValidationIssue> Errors => issues.Where(issue => issue.Severity == MapAuthoringIssueSeverity.Error);
        public bool IsValid => !Errors.Any();

        public void Add(MapAuthoringValidationIssue issue)
        {
            if (issue != null)
            {
                issues.Add(issue);
            }
        }

        public bool ContainsCode(string code)
        {
            return issues.Any(issue => string.Equals(issue.Code, code, StringComparison.Ordinal));
        }
    }
}
