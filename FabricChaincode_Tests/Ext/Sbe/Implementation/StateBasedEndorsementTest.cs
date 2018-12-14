/*
Copyright IBM Corp., DTCC All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System;
using System.Collections.Generic;
using Google.Protobuf;
using Hyperledger.Fabric.Protos.Common;
using Hyperledger.Fabric.Shim.Ext.Sbe;
using Hyperledger.Fabric.Shim.Ext.Sbe.Implementation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperledger.Fabric.Shim.Tests.Ext.Sbe.Implementation
{
    [TestClass]
    public class StateBasedEndorsementTest
    {
        [TestMethod]
        public void TestAddOrgs()
        {
            // add an org
            StateBasedEndorsement ep = new StateBasedEndorsement(null);
            ep.AddOrgs(RoleType.RoleTypePeer, "Org1");

            byte[] epBytes = ep.Policy();
            Assert.IsNotNull(epBytes);
            Assert.IsTrue(epBytes.Length>0);
            byte[] expectedEPBytes = StateBasedEndorsementUtils.SignedByFabricEntity("Org1", MSPRole.Types.MSPRoleType.Peer).ToByteString().ToByteArray();
            CollectionAssert.AreEqual(expectedEPBytes,epBytes);
        }

        [TestMethod]
        public void TestDelOrgs()
        {

            byte[] initEPBytes = StateBasedEndorsementUtils.SignedByFabricEntity("Org1", MSPRole.Types.MSPRoleType.Peer).ToByteString().ToByteArray();
            StateBasedEndorsement ep = new StateBasedEndorsement(initEPBytes);
            List<string> listOrgs = ep.ListOrgs();

            Assert.IsNotNull(listOrgs);
            CollectionAssert.Contains(listOrgs,"Org1");
            Assert.AreEqual(listOrgs.Count,1);
                
            ep.AddOrgs(RoleType.RoleTypeMember, "Org2");
            ep.DelOrgs("Org1");

            byte[] epBytes = ep.Policy();
            Assert.IsNotNull(epBytes);
            Assert.IsTrue(epBytes.Length > 0);
            byte[] expectedEPBytes = StateBasedEndorsementUtils.SignedByFabricEntity("Org2", MSPRole.Types.MSPRoleType.Member).ToByteString().ToByteArray();
            CollectionAssert.AreEqual(expectedEPBytes, epBytes);
        }

        [TestMethod]
        public void TestListOrgs()
        {
            byte[] initEPBytes = StateBasedEndorsementUtils.SignedByFabricEntity("Org1", MSPRole.Types.MSPRoleType.Peer).ToByteString().ToByteArray();
            StateBasedEndorsement ep = new StateBasedEndorsement(initEPBytes);
            List<String> listOrgs = ep.ListOrgs();
            Assert.IsNotNull(listOrgs);
            Assert.AreEqual(listOrgs.Count, 1);
            CollectionAssert.Contains(listOrgs, "Org1");
        }
    }
}
