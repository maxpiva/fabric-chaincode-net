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

namespace Hyperledger.Fabric.Shim.Ledger
{
    public interface IKeyModification
    {

        /**
         * Returns the transaction id.
         *
         * @return
         */
        string TxId { get; }

        /**
         * Returns the key's value at the time returned by {@link #getTimestamp()}.
         *
         * @return
         */
        byte[] Value { get; }

        /**
         * Returns the key's value at the time returned by {@link #getTimestamp()},
         * decoded as a UTF-8 string.
         *
         * @return
         */
        string StringValue { get; }

        /**
         * Returns the timestamp of the key modification entry.
         *
         * @return
         */
        DateTime? Timestamp { get; }

        /**
         * Returns the deletion marker.
         *
         * @return
         */
        bool IsDeleted { get; }

    }
}