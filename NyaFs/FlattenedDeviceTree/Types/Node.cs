using Extension.Array;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.FlattenedDeviceTree.Types
{
    public class Node
    {
        /// <summary>
        /// Node name
        /// </summary>
        public string Name;

        /// <summary>
        /// List of properties
        /// </summary>
        public List<Property> Properties = new List<Property>();

        /// <summary>
        /// List of nested nodes
        /// </summary>
        public List<Node> Nodes = new List<Node>();

        public Node()
        {
            Name = "";
        }

        public Node(string Name)
        {
            this.Name = Name;
        }

        public override string ToString()
        {
            return $"NODE {Name} P:{Properties.Count} N:{Nodes.Count}";
        }

        public Node GetNode(string Name)
        {
            foreach(var N in Nodes)
            {
                if (N.Name == Name)
                    return N;
            }

            return null;
        }

        public byte[] GetValue(string PropertyName)
        {
            foreach(var P in Properties)
            {
                if(P.Name == PropertyName)
                    return P.Value;
            }

            return null;
        }

        public string GetStringValue(string PropertyName)
        {
            var Val = GetValue(PropertyName);
            if (Val != null)
                return UTF8Encoding.UTF8.GetString(Val.ReadArray(0, Val.Length - 1));
            else
                return null;
        }

        public void AddUInt32Value(string Name, uint Value)
        {
            byte[] Raw = new byte[4];
            Raw.WriteUInt32BE(0, Value);

            Properties.Add(new Property(Name, Raw));
        }

        public void AddRawValue(string Name, byte[] Value)
        {
            Properties.Add(new Property(Name, Value));
        }

        public void AddStringValue(string Name, string Value)
        {
            Properties.Add(new Property(Name, UTF8Encoding.UTF8.GetBytes(Value + "\0")));
        }
    }
}
