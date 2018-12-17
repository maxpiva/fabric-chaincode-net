/*
Copyright IBM Corp., DTCC All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Hyperledger.Fabric.Protos.Common;
using Hyperledger.Fabric.Shim.Helper;

using Serilog;

namespace Hyperledger.Fabric.Shim.Ext.Sbe.Implementation
{
    /**
     * Implements {@link StateBasedEndorsement}
     */
    public class StateBasedEndorsement : IStateBasedEndorsement
    {
        private static readonly ILogger logger = Log.ForContext<StateBasedEndorsement>();
        private readonly Dictionary<string, MSPRole.Types.MSPRoleType> orgs = new Dictionary<string, MSPRole.Types.MSPRoleType>();

        public StateBasedEndorsement(byte[] ep)
        {
            if (ep == null)
                ep = new byte[] { };
            try
            {
                SignaturePolicyEnvelope spe = SignaturePolicyEnvelope.Parser.ParseFrom(ep);
                SetMSPIDsFromSP(spe);
            }
            catch (InvalidProtocolBufferException e)
            {
                throw new ArgumentException("error unmarshaling endorsement policy bytes", e);
            }
        }


        public byte[] Policy()
        {
            SignaturePolicyEnvelope spe = PolicyFromMSPIDs();
            return spe.ToByteArray();
        }

        public void AddOrgs(RoleType role, params string[] organizations)
        {
            MSPRole.Types.MSPRoleType mspRole;
            if ((int) RoleType.RoleTypeMember == (int) role)
            {
                mspRole = MSPRole.Types.MSPRoleType.Member;
            }
            else
            {
                mspRole = MSPRole.Types.MSPRoleType.Peer;
            }

            foreach (string neworg in organizations)
            {
                orgs[neworg] = mspRole;
            }
        }


        public void DelOrgs(params string[] organizations)
        {
            foreach (string delorg in organizations)
            {
                if (orgs.ContainsKey(delorg))
                    orgs.Remove(delorg);
            }
        }


        public List<string> ListOrgs()
        {
            return orgs.Keys.ToList();
        }

        private void SetMSPIDsFromSP(SignaturePolicyEnvelope spe)
        {
            spe.Identities.Where(a => a.PrincipalClassification == MSPPrincipal.Types.Classification.Role).ToList().ForEach(AddOrg);
        }

        private void AddOrg(MSPPrincipal identity)
        {
            try
            {
                MSPRole mspRole = MSPRole.Parser.ParseFrom(identity.Principal);
                orgs[mspRole.MspIdentifier] = mspRole.Role;
            }
            catch (InvalidProtocolBufferException e)
            {
                logger.Warning("error unmarshaling msp principal");
                throw new ArgumentException("error unmarshaling msp principal", e);
            }
        }


        private SignaturePolicyEnvelope PolicyFromMSPIDs()
        {
            List<string> mspids = ListOrgs().OrderByAlphaNumeric(a => a).ToList();
            List<MSPPrincipal> principals = new List<MSPPrincipal>();
            List<SignaturePolicy> sigpolicy = new List<SignaturePolicy>();
            for (int i = 0; i < mspids.Count; i++)
            {
                string mspid = mspids[i];
                principals.Add(new MSPPrincipal {PrincipalClassification = MSPPrincipal.Types.Classification.Role, Principal = new MSPRole {MspIdentifier = mspid, Role = orgs[mspid]}.ToByteString()});
                sigpolicy.Add(StateBasedEndorsementUtils.SignedBy(i));
            }

            // create the policy: it requires exactly 1 signature from all of the principals
            SignaturePolicyEnvelope spe = new SignaturePolicyEnvelope {Version = 0, Rule = StateBasedEndorsementUtils.NOutOf(mspids.Count, sigpolicy)};
            spe.Identities.AddRange(principals);
            return spe;
        }
    }
}