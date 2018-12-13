// Copyright IBM 2017 All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//         http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
/*
package org.hyperledger.fabric.shim;

import static java.nio.charset.StandardCharsets.UTF_8;

import java.util.HashMap;
import java.util.Map;
*/
/**
 * Defines methods that all chaincodes must implement.
 */

using System.Threading;
using System.Threading.Tasks;

namespace Hyperledger.Fabric.Shim
{
    public interface IChaincodeSync
    {
        /**
	 * Called during an instantiate transaction after the container has been
	 * established, allowing the chaincode to initialize its internal data.
	 */
        Response Init(IChaincodeStub stub);

        /**
	 * Called for every Invoke transaction. The chaincode may change its state
	 * variables.
	 */
        Response Invoke(IChaincodeStub stub);
    }

    public interface IChaincodeAsync
    {
        /**
        * Called during an instantiate transaction after the container has been
        * established, allowing the chaincode to initialize its internal data.
        */
        Task<Response> InitAsync(IChaincodeStub stub, CancellationToken token=default(CancellationToken));

        /**
	 * Called for every Invoke transaction. The chaincode may change its state
	 * variables.
	 */
        Task<Response> InvokeAsync(IChaincodeStub stub, CancellationToken token = default(CancellationToken));
    }
}