// Copyright (c) Meta Platforms, Inc. and affiliates.
using System;
using UnityEngine;

namespace Meta.Utilities.Narrative
{
    /// <summary>
    /// A wrapper around a string with a special property drawer and case/whitespace insensitivity 
    /// </summary>
    [Serializable]
    public struct TaskID
    {
        public static readonly TaskID None = null;

        [SerializeField] public string ID;

        public TaskID(string str) => ID = str;

        public static implicit operator string(TaskID id) => id.ID;

        public static implicit operator TaskID(string str) => new(str);

        public override int GetHashCode() => (ID ?? "").Replace(" ", "").GetHashCode(StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj)
        {
            if (obj is TaskID id) return Equals(id);
            if (obj is string str) return Equals(str);
            return false;
        }

        public bool Equals(TaskID otherID) => string.Equals(ID.Replace(" ", ""), otherID.ID.Replace(" ", ""), StringComparison.OrdinalIgnoreCase);

        public bool Equals(string str) => string.Equals(str ?? "".Replace(" ", ""), ID.Replace(" ", ""), StringComparison.OrdinalIgnoreCase);

        public static bool operator ==(TaskID a, TaskID b) => a.Equals(b);

        public static bool operator !=(TaskID a, TaskID b) => !a.Equals(b);

        public static bool operator ==(TaskID id, string str) => id.Equals(str);

        public static bool operator !=(TaskID id, string str) => !id.Equals(str);

        public static bool operator ==(string str, TaskID id) => id.Equals(str);

        public static bool operator !=(string str, TaskID id) => !id.Equals(str);

        public override string ToString() => ID;

        private static string ComparisonForm(string s)
        {
            return s == null ? null : string.IsNullOrWhiteSpace(s) ? null : s.ToLowerInvariant().Replace(" ", "");
        }
    }
}