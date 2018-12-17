# fabric-chaincode-net


[![Build status](https://ci.appveyor.com/api/projects/status/72hk6ank8wc9m27o?svg=true)](https://ci.appveyor.com/project/maxpiva/fabric-chaincode-net)

v1.4

Direct .NET port from [fabric-chaincode-java](https://github.com/hyperledger/fabric-chaincode-java)

`Alpha`

* All unit test passing, including mock tests.
* Full Async Support to the bone, but Sync Methods are supported for easy porting.


Need to figure out how to integrate this in [fabric](https://github.com/hyperledger/fabric)
and then integrating the .net chaincode source and/or compiled uploads into [.NET SDK](https://github.com/maxpiva/fabric-sdk-net)

Help is appreciated since i'm not proficient in GO language

**TidBits**

* Users can inherit ChaincodeBaseAsync for async implementations or ChaincodeBase for sync ones.
* Users can inherit ChaincodeBaseMapperAsync or ChaincodeBaseMapper for automapping, you only need to implements the functions with the format:

  *Response **FunctionName**(IChaincodeStub stub)* or 

  *Task\<Response\> **FunctionName**Async(IChaincodeStub stub, CancellationToken token)* in case of async usage.
* Added FunctionName attribute for the above defined methods. in case your implementation name differs from the chaincode function.
* C# 8 is required. Since it uses new C# 8 IAsyncEnumerable

TODO:
* Examples
* Docker builds

