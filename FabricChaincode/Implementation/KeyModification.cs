/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System;
using Google.Protobuf;
using Hyperledger.Fabric.Shim.Ledger;

namespace Hyperledger.Fabric.Shim.Implementation
{
    public class KeyModification : IKeyModification, IEquatable<KeyModification>
    {
        private readonly ByteString value;

        public KeyModification(Protos.Ledger.QueryResult.KeyModification km)
        {
            TxId = km.TxId;
            value = km.Value;
            Timestamp = km.Timestamp?.ToDateTime();
            IsDeleted = km.IsDelete;
        }


        public string TxId { get; }
        public byte[] Value => value.ToByteArray();
        public string StringValue => value.ToStringUtf8();
        public DateTime? Timestamp { get; }
        public bool IsDeleted { get; }


        public override int GetHashCode()
        {
            int prime = 31;
            int result = 1;
            result = prime * result + (IsDeleted ? 1231 : 1237);
            result = prime * result + ((Timestamp == null) ? 0 : Timestamp.GetHashCode());
            result = prime * result + ((TxId == null) ? 0 : TxId.GetHashCode());
            result = prime * result + ((value == null) ? 0 : value.GetHashCode());
            return result;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is KeyModification))
                return false;
            return Equals((KeyModification) obj);
        }

        public bool Equals(KeyModification obj)
        {
            if (this == obj) return true;
            if (obj == null) return false;
            if (IsDeleted != obj.IsDeleted) return false;
            if (!Timestamp.Equals(obj.Timestamp)) return false;
            if (!TxId.Equals(obj.TxId)) return false;
            if (!value.Equals(obj.value)) return false;
            return true;
        }
    }
}