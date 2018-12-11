/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
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
        public static readonly string NAMESPACE = DELIMITER;
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
            if (!compositeKey.StartsWith(NAMESPACE))
                throw CompositeKeyFormatException.ForInputString(compositeKey, compositeKey, 0);
            // relying on the fact that NAMESPACE == DELIMETER
            string[] segments = compositeKey.Split(new [] {DELIMITER}, StringSplitOptions.RemoveEmptyEntries);
            return new CompositeKey(segments[0], segments.Skip(1));
        }


        public static void ValidateSimpleKeys(params string[] keys)
        {
            foreach(string key in keys) 
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith(NAMESPACE))
                    throw CompositeKeyFormatException.ForSimpleKey(key);
            }
        }
        /**
         * To ensure that simple keys do not go into composite key namespace,
         * we validate simple key to check whether the key starts with 0x00 (which
         * is the namespace for compositeKey). This helps in avoding simple/composite
         * key collisions.
         *
         * @throws CompositeKeyFormatException if First character of the key
         */
        private string GenerateCompositeKeyString(string objectType, List<string> attrs)
        {
            // object type must be a valid composite key segment
            ValidateCompositeKeySegment(objectType);

            if (attrs == null || attrs.Count==0)
                return NAMESPACE + objectType + DELIMITER;
            // the attributes must be valid composite key segments
            attrs.ForEach(ValidateCompositeKeySegment);
            // return NAMESPACE + objectType + DELIMITER + (attribute + DELIMITER)*

            StringBuilder builder = new StringBuilder();
            builder.Append(NAMESPACE);
            builder.Append(objectType);
            builder.Append(DELIMITER);
            attrs.ForEach(a => builder.Append(a).Append(DELIMITER));
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