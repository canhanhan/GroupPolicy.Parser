using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GroupPolicy.Parser
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("277E34AE-9A5A-4A0E-B458-1AB5FAD7A21C")]
    public class RegistrySetting
    {
        [DispId(0)]
        public string KeyPath { get; set; }

        [DispId(1)]
        public string Value { get; set; }

        [DispId(2)]
        public uint Type { get; set; }

        [DispId(3)]
        public uint Size { get; set; }

        [DispId(4)]
        public byte[] BinaryData { get; set; }

        [DispId(5)]
        public object Data { get; set; }
    }
}
