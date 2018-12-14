using System.Collections.Generic;
using System.Threading;
using Google.Protobuf;
using Hyperledger.Fabric.Protos.Peer;
using Hyperledger.Fabric.Shim.Helper;
using Hyperledger.Fabric.Shim.Implementation;
using Hyperledger.Fabric.Shim.Ledger;

namespace Hyperledger.Fabric.Shim
{
    public static class SyncExtensions
    {
        public static Response InvokeChaincode(this IChaincodeStub stub, string chaincodeName, List<byte[]> arguments, string channel) => stub.InvokeChaincodeAsync(chaincodeName,arguments,channel).RunAndUnwrap();
        public static byte[] GetState(this IChaincodeStub stub, string key) => stub.GetStateAsync(key).RunAndUnwrap();
        public static void PutState(this IChaincodeStub stub, string key, byte[] value) => stub.PutStateAsync(key, value).RunAndUnwrap();
        public static void DelState(this IChaincodeStub stub, string key) => stub.DelStateAsync(key).RunAndUnwrap();
        public static IQueryResultsEnumerable<IKeyValue> GetStateByRange(this IChaincodeStub stub, string startKey, string endKey) => stub.GetStateByRangeAsync(startKey, endKey).ToSyncEnumerable();
        public static IQueryResultsEnumerable<IKeyValue> GetStateByPartialCompositeKey(this IChaincodeStub stub, string compositeKey) => stub.GetStateByPartialCompositeKeyAsync(compositeKey).ToSyncEnumerable();
        public static IQueryResultsEnumerable<IKeyValue> GetStateByPartialCompositeKey(this IChaincodeStub stub, string objectType, params string[] attributes) => stub.GetStateByPartialCompositeKeyAsync(objectType, attributes).ToSyncEnumerable();
        public static IQueryResultsEnumerable<IKeyValue> GetStateByPartialCompositeKey(this IChaincodeStub stub, CompositeKey compositeKey) => stub.GetStateByPartialCompositeKeyAsync(compositeKey).ToSyncEnumerable();
        public static IQueryResultsEnumerable<IKeyValue> GetQueryResult(this IChaincodeStub stub, string query) => stub.GetQueryResultAsync(query).ToSyncEnumerable();
        public static IQueryResultsEnumerable<IKeyModification> GetHistoryForKey(this IChaincodeStub stub, string key) => stub.GetHistoryForKeyAsync(key).ToSyncEnumerable();
        public static byte[] GetPrivateData(this IChaincodeStub stub, string collection, string key) => stub.GetPrivateDataAsync(collection, key).RunAndUnwrap();
        public static void PutPrivateData(this IChaincodeStub stub, string collection, string key, byte[] value) => stub.PutPrivateDataAsync(collection, key, value).RunAndUnwrap();
        public static void DelPrivateData(this IChaincodeStub stub, string collection, string key) => stub.DelPrivateDataAsync(collection, key).RunAndUnwrap();
        public static IQueryResultsEnumerable<IKeyValue> GetPrivateDataByRange(this IChaincodeStub stub, string collection, string startKey, string endKey) => stub.GetPrivateDataByRangeAsync(collection, startKey, endKey).ToSyncEnumerable();
        public static IQueryResultsEnumerable<IKeyValue> GetPrivateDataByPartialCompositeKey(this IChaincodeStub stub, string collection, string compositeKey) => stub.GetPrivateDataByPartialCompositeKeyAsync(collection, compositeKey).ToSyncEnumerable();
        public static IQueryResultsEnumerable<IKeyValue> GetPrivateDataByPartialCompositeKey(this IChaincodeStub stub, string collection, CompositeKey compositeKey) => stub.GetPrivateDataByPartialCompositeKeyAsync(collection, compositeKey).ToSyncEnumerable();
        public static IQueryResultsEnumerable<IKeyValue> GetPrivateDataByPartialCompositeKey(this IChaincodeStub stub, string collection, string objectType, params string[] attributes) => stub.GetPrivateDataByPartialCompositeKeyAsync(collection, objectType,attributes).ToSyncEnumerable();
        public static IQueryResultsEnumerable<IKeyValue> GetPrivateDataQueryResult(this IChaincodeStub stub, string collection, string query) => stub.GetPrivateDataQueryResultAsync(collection, query).ToSyncEnumerable();
        public static Response InvokeChaincode(this IChaincodeStub stub, string chaincodeName, List<byte[]> arguments) => stub.InvokeChaincodeAsync(chaincodeName, arguments).RunAndUnwrap();
        public static Response InvokeChaincodeWithStringArgs(this IChaincodeStub stub, string chaincodeName, List<string> arguments, string channel) => stub.InvokeChaincodeWithStringArgsAsync(chaincodeName,arguments,channel).RunAndUnwrap();
        public static Response InvokeChaincodeWithStringArgs(this IChaincodeStub stub, string chaincodeName, List<string> arguments) => stub.InvokeChaincodeWithStringArgsAsync(chaincodeName, arguments).RunAndUnwrap();
        public static Response InvokeChaincodeWithStringArgs(this IChaincodeStub stub, string chaincodeName, params string[] arguments) => stub.InvokeChaincodeWithStringArgsAsync(chaincodeName, default(CancellationToken), arguments).RunAndUnwrap();
        public static string GetStringState(this IChaincodeStub stub, string key) => stub.GetStringStateAsync(key).RunAndUnwrap();
        public static void PutPrivateData(this IChaincodeStub stub, string collection, string key, string value) => stub.PutPrivateDataAsync(collection, key, value).RunAndUnwrap();
        public static string GetPrivateDataUTF8(this IChaincodeStub stub, string collection, string key) => stub.GetPrivateDataUTF8Async(collection, key).RunAndUnwrap();
        public static void PutStringState(this IChaincodeStub stub, string key, string value) => stub.PutStringStateAsync(key, value).RunAndUnwrap();
        public static byte[] GetStateValidationParameter(this IChaincodeStub stub, string key) => stub.GetStateValidationParameterAsync(key).RunAndUnwrap();
        public static void SetStateValidationParameter(this IChaincodeStub stub, string key, byte[] value) => stub.SetStateValidationParameterAsync(key, value).RunAndUnwrap();
        public static IQueryResultsEnumerable<IKeyValue> GetStateByRangeWithPagination(this IChaincodeStub stub, string startKey, string endKey, int pageSize, string bookmark) => stub.GetStateByRangeWithPaginationAsync(startKey, endKey, pageSize, bookmark).ToSyncEnumerable();
        public static IQueryResultsEnumerable<IKeyValue> GetStateByPartialCompositeKeyWithPagination(this IChaincodeStub stub, CompositeKey compositeKey, int pageSize, string bookmark) => stub.GetStateByPartialCompositeKeyWithPaginationAsync(compositeKey, pageSize, bookmark).ToSyncEnumerable();
        public static IQueryResultsEnumerable<IKeyValue> GetQueryResultWithPagination(this IChaincodeStub stub, string query, int pageSize, string bookmark) => stub.GetQueryResultWithPaginationAsync(query, pageSize, bookmark).ToSyncEnumerable();
        public static byte[] GetPrivateDataValidationParameter(this IChaincodeStub stub, string collection, string key) => stub.GetPrivateDataValidationParameterAsync(collection, key).RunAndUnwrap();
        public static void SetPrivateDataValidationParameter(this IChaincodeStub stub, string collection, string key, byte[] value) => stub.SetPrivateDataValidationParameterAsync(collection, key, value).RunAndUnwrap();
        public static void Start(this ChaincodeBaseAsync cba, string[] args) => cba.StartAsync(args).RunAndUnwrap();

        public static ByteString GetState(this Handler handler, string channelId, string txId, string collection, string key) => handler.GetStateAsync(channelId, txId, collection, key).RunAndUnwrap();
        public static void PutState(this Handler handler, string channelId, string txId, string collection, string key, ByteString value) => handler.PutStateAsync(channelId, txId, collection, key, value).RunAndUnwrap();
        public static void DeleteState(this Handler handler, string channelId, string txId, string collection, string key) => handler.DeleteStateAsync(channelId, txId, collection, key).RunAndUnwrap();
        public static QueryResponse GetStateByRange(this Handler handler, string channelId, string txId, string collection, string startKey, string endKey, ByteString metadata) => handler.GetStateByRangeAsync(channelId, txId, collection, startKey, endKey, metadata).RunAndUnwrap();
        public static QueryResponse QueryStateNext(this Handler handler, string channelId, string txId, string queryId) => handler.QueryStateNextAsync(channelId, txId, queryId).RunAndUnwrap();
        public static QueryResponse QueryStateClose(this Handler handler, string channelId, string txId, string queryId) => handler.QueryStateCloseAsync(channelId, txId, queryId).RunAndUnwrap();
        public static QueryResponse GetQueryResult(this Handler handler, string channelId, string txId, string collection, string query, ByteString metadata) => handler.GetQueryResultAsync(channelId, txId, collection, query, metadata).RunAndUnwrap();
        public static QueryResponse GetHistoryForKey(this Handler handler, string channelId, string txId, string key) => handler.GetHistoryForKeyAsync(channelId, txId, key).RunAndUnwrap();
        public static Response InvokeChaincode(this Handler handler, string channelId, string txId, string chaincodeName, List<byte[]> args) => handler.InvokeChaincodeAsync(channelId, txId, chaincodeName, args).RunAndUnwrap();
    }
}
