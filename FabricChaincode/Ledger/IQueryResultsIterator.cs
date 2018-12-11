/*
Copyright IBM Corp. All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System;
using System.Collections.Generic;

namespace Hyperledger.Fabric.Shim.Ledger
{
    /**
     * QueryResultsIterator allows a chaincode to iterate over a set of key/value pairs returned by range, execute and history queries.
     *
     * @param <T>
     */
    public interface IQueryResultsIterator<T> : IEnumerable<T>, IDisposable
    {
    }
}
