using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;


namespace GroupPolicy.Parser
{
    [Guid("39887E5B-301C-4802-88E3-682F06AE7C4D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IRegistryFile
    {
        [DispId(1)]
        int Count { get; }

        [DispId(2)]
        RegistrySetting Item(int index);

        [DispId(3)]
        void Open(string path);

        [DispId(4)]
        void Save();
    }

    [Guid("086630A6-FEF8-43E2-A226-9C3FBACBAD7F")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(IRegistryFile))]
    public class RegistryFile : IRegistryFile
    {
        private const uint SIGNATURE = 0x67655250;
        private static Array ValueTypes = Enum.GetValues(typeof(RegistryValueType));

        public string Path { get; set; }
        public uint Signature { get; set; }
        public uint Version { get; set; }

        public RegistrySetting[] Settings { get; private set; }

        public int Count { get { return this.Settings.Length; } }

        public RegistryFile()
        {
            this.Settings = new RegistrySetting[0];
        }

        public void Save()
        {
            using (var file = File.OpenWrite(this.Path))
            using (var writer = new BinaryWriter(file, Encoding.Unicode)) 
            {
                writer.Write(this.Signature);
                writer.Write(this.Version);
                foreach(var instruction in this.Settings) 
                {
                    writer.Write('[');
                    writer.Write(Encoding.Unicode.GetBytes(instruction.KeyPath + '\0'));
                    writer.Write(';');
                    writer.Write(Encoding.Unicode.GetBytes(instruction.Value + '\0'));
                    writer.Write(';');
                    writer.Write((uint)instruction.Type);
                    writer.Write(';');
                    var data = SetData(instruction);
                    writer.Write(data.Length);
                    writer.Write(';');
                    writer.Write(data);
                    writer.Write(']');
                }
            }
        }

        public void Open(string path)
        {
            this.Path = path;

            var settings = new List<RegistrySetting>();
            using (var file = File.OpenRead(path))
            using (var reader = new BinaryReader(file, Encoding.Unicode))
            {
                this.Signature = reader.ReadUInt32();
                if (this.Signature != SIGNATURE)
                {
                    throw new NotSupportedException("File format is not supported");
                }
                this.Version = reader.ReadUInt32();

                var length = reader.BaseStream.Length;
                while (reader.BaseStream.Position < length)
                {
                    var setting = new RegistrySetting();
                    reader.ReadChar();
                    setting.KeyPath = ReadString(reader);
                    reader.ReadChar();
                    setting.Value = ReadString(reader);
                    reader.ReadChar();
                    setting.Type = reader.ReadUInt32();
                    reader.ReadChar();
                    setting.Size = reader.ReadUInt32();
                    reader.ReadChar();
                    setting.Data = reader.ReadBytes((int)setting.Size);
                    reader.ReadChar();
                    setting.FormattedData = GetData(setting);

                    settings.Add(setting);
                }
            }

            this.Settings = settings.ToArray();
        }

        public RegistrySetting Item(int index)
        {
            return this.Settings[index];
        }

        private static RegistryValueType GetType(uint type)
        {
            return Enum.IsDefined(typeof(RegistryValueType), (int)type) ? (RegistryValueType)type : RegistryValueType.REG_NONE;
        }

        private static string ReadString(BinaryReader reader)
        {
            char current;
            string temp = string.Empty;
            while ((current = reader.ReadChar()) != '\0')
            {
                temp += current;
            }

            return temp;
        }

        private static object GetData(RegistrySetting instruction)
        {
            switch (GetType(instruction.Type))
            {
                case RegistryValueType.REG_SZ:
                case RegistryValueType.REG_EXPAND_SZ:
                    return Encoding.Unicode.GetString(instruction.Data).TrimEnd('\0');
                case RegistryValueType.REG_DWORD:
                    return BitConverter.ToUInt32(instruction.Data, 0);
                case RegistryValueType.REG_QWORD:
                    return BitConverter.ToUInt64(instruction.Data, 0);
                case RegistryValueType.REG_MULTI_SZ:
                    if (instruction.Data.Length == 0) return new string[0];

                    var collection = new List<string>();
                    using (var stream = new MemoryStream(instruction.Data))
                    using (var reader = new BinaryReader(stream, Encoding.Unicode))
                    {
                        var length = reader.BaseStream.Length;
                        while (reader.BaseStream.Position < length)
                        {
                            collection.Add(ReadString(reader));
                            if (reader.PeekChar() == '\0')
                                break;
                        }
                        return collection.ToArray();
                    }
                default:
                    return instruction.Data;
            }
        }

        private static byte[] SetData(RegistrySetting instruction)
        {
            switch (GetType(instruction.Type))
            {
                case RegistryValueType.REG_SZ:
                case RegistryValueType.REG_EXPAND_SZ:
                    return Encoding.Unicode.GetBytes((string)instruction.FormattedData + '\0');
                case RegistryValueType.REG_DWORD:
                    return BitConverter.GetBytes((uint)instruction.FormattedData);
                case RegistryValueType.REG_QWORD:
                    return BitConverter.GetBytes((ulong)instruction.FormattedData);
                case RegistryValueType.REG_MULTI_SZ:
                    return Encoding.Unicode.GetBytes(string.Join("\0", (string[])instruction.FormattedData) + "\0\0");
                default:
                    return (byte[])instruction.FormattedData;
            }
        }
    }    
}
