using System;
using System.Collections.Generic;
using System.Text;

namespace Hyperledger.Fabric.Shim
{
    [AttributeUsage(AttributeTargets.Method)]
    public class FunctionName : Attribute
    {
        public string Name { get; }
        public FunctionName(string name)
        {
            Name = name;
        }
    }
}
