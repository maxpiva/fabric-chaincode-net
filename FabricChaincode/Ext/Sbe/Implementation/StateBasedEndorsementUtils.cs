/*
Copyright IBM Corp., DTCC All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System.Collections.Generic;
using Google.Protobuf;
using Hyperledger.Fabric.Protos.Common;

namespace Hyperledger.Fabric.Shim.Ext.Sbe.Implementation
{
    /**
     * Utility to create {@link SignaturePolicy} and {@link SignaturePolicyEnvelope}
     */
    public class StateBasedEndorsementUtils
    {
        /**
         * Creates a SignaturePolicy requiring a given signer's signature
         *
         * @param index
         * @return
         */
        public static SignaturePolicy SignedBy(int index)
        {
            return new SignaturePolicy {SignedBy = index};
        }

        /**
         * Creates a policy which requires N out of the slice of policies to evaluate to true
         *
         * @param n
         * @param policies
         * @return
         */
        public static SignaturePolicy NOutOf(int n, List<SignaturePolicy> policies)
        {
            SignaturePolicy.Types.NOutOf no = new SignaturePolicy.Types.NOutOf {N = n};
            no.Rules.AddRange(policies);
            return new SignaturePolicy {NOutOf = no};
        }

        /**
         * Creates a {@link SignaturePolicyEnvelope}
         * requiring 1 signature from any fabric entity, having the passed role, of the specified MSP
         *
         * @param mspId
         * @param role
         * @return
         */
        public static SignaturePolicyEnvelope SignedByFabricEntity(string mspId, MSPRole.Types.MSPRoleType role)
        {
            // specify the principal: it's a member of the msp we just found
            MSPPrincipal principal = new MSPPrincipal {PrincipalClassification = MSPPrincipal.Types.Classification.Role, Principal = new MSPRole {MspIdentifier = mspId, Role = role}.ToByteString()};
            SignaturePolicyEnvelope spe = new SignaturePolicyEnvelope {Version = 0, Rule = NOutOf(1, new List<SignaturePolicy> {SignedBy(0)})};
            spe.Identities.Add(principal);
            return spe;
        }
    }
}