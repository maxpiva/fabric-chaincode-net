/*
Copyright IBM Corp., DTCC All Rights Reserved.

SPDX-License-Identifier: Apache-2.0
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace Hyperledger.Fabric.Shim.Ext.Sbe
{
    /**
     * StateBasedEndorsement provides a set of convenience methods to create and
     * modify a state-based endorsement policy. Endorsement policies created by
     * this convenience layer will always be a logical AND of "ORG.peer"
     * principals for one or more ORGs specified by the caller.
     */
    public interface IStateBasedEndorsement
    {
        /**
         * Returns the endorsement policy as bytes
         * @return
         */
        byte[] Policy();

        /**
         * Adds the specified orgs to the list of orgs that are required
         * to endorse. All orgs MSP role types will be set to the role that is
         * specified in the first parameter. Among other aspects the desired role
         * depends on the channel's configuration: if it supports node OUs, it is
         * likely going to be the PEER role, while the MEMBER role is the suited
         * one if it does not.
         *
         * @param roleType
         * @param organizations
         */
        void AddOrgs(RoleType roleType, params string[] organizations);

        /**
         * deletes the specified channel orgs from the existing key-level endorsement
         * policy for this KVS key.
         * @param organizations
         */
        void DelOrgs(params string[] organizations);

        /**
         * Returns an array of channel orgs that are required to endorse changes
         *
         * @return
         */
        List<string> ListOrgs();
    }

/**
* RoleType of an endorsement policy's identity
*/
    public enum RoleType
    {
        RoleTypeMember=0,
        RoleTypePeer=3
    }


    public static class RoleTypeExtensions
    {
        public static string ToValue(this RoleType role)
        {
            switch (role)
            {
                case RoleType.RoleTypeMember:
                    return "MEMBER";
                case RoleType.RoleTypePeer:
                    return "PEER";
            }

            return string.Empty;
        }

        public static List<RoleType> All() => Enum.GetValues(typeof(RoleType)).Cast<RoleType>().ToList();

        public static RoleType RoleTypeFromValue(this string value)
        {
            switch (value)
            {
                case "MEMBER":
                    return RoleType.RoleTypeMember;
                case "PEER":
                    return RoleType.RoleTypePeer;
            }

            throw new ArgumentException($"role type {value} does not exist");
        }
    }
}