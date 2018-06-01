/*
Copyright IBM 2017 All Rights Reserved.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

         http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Hyperledger.Fabric.Shim.Ledger
{
    public class CompositeKey
    {
        public const int MAX_CODE_POINT = 0x10FFFF;
        public const int MIN_CODE_POINT = 0x000000;

        private static readonly string DELIMITER = char.ConvertFromUtf32(MIN_CODE_POINT);
        private static readonly string INVALID_SEGMENT_CHAR = char.ConvertFromUtf32(MAX_CODE_POINT);
        private static readonly string INVALID_SEGMENT_PATTERN = $"(?:{INVALID_SEGMENT_CHAR}|{DELIMITER})";

        private static readonly Regex invalid = new Regex(INVALID_SEGMENT_PATTERN, RegexOptions.Compiled);
        private readonly List<string> attributes;

        private readonly string compositeKey;

        public CompositeKey(string objectType, params string[] attributes) : this(objectType, attributes == null ? new List<string>() : attributes.ToList())
        {
        }

        public CompositeKey(string objectType, IEnumerable<string> attributes)
        {
            ObjectType = objectType ?? throw new NullReferenceException("objectType cannot be null");
            this.attributes = attributes.ToList();
            compositeKey = GenerateCompositeKeyString(objectType, this.attributes);
        }

        public string ObjectType { get; }

        public List<string> Attributes
        {
            get => attributes.ToList();
        }

        public override string ToString()
        {
            return compositeKey;
        }

        public static CompositeKey ParseCompositeKey(string compositeKey)
        {
            if (compositeKey == null) return null;
            string[] segments = compositeKey.Split(new string[] {DELIMITER}, StringSplitOptions.RemoveEmptyEntries);
            return new CompositeKey(segments[0], segments.Skip(1));
        }

        private string GenerateCompositeKeyString(string objectType, List<string> attributes)
        {
            // object type must be a valid composite key segment
            ValidateCompositeKeySegment(objectType);

            // the attributes must be valid composite key segments
            attributes.ForEach(a => ValidateCompositeKeySegment(a));
            StringBuilder builder = new StringBuilder();
            builder.Append(objectType);
            builder.Append(DELIMITER);
            attributes.ForEach(a => builder.Append(a).Append(DELIMITER));
            return builder.ToString();
        }

        private void ValidateCompositeKeySegment(string segment)
        {
            Match match = invalid.Match(segment);
            if (match.Success)
                throw CompositeKeyFormatException.ForInputString(segment, match.Groups[0].Value, match.Index);
        }
    }
}