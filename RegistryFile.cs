using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GroupPolicy.Parser
{
    public class RegistryFile
    {
        private const uint SIGNATURE = 0x67655250;
        private static Array ValueTypes = Enum.GetValues(typeof(RegistryValueType));

        public string Path { get; set; }
        public uint Signature { get; set; }
        public uint Version { get; set; }

        public RegistrySetting[] Settings { get; private set; }
        public byte[] Data
        {
            get
            {   
                using (var stream = new MemoryStream()) 
                using (var writer = new BinaryWriter(stream, Encoding.Unicode))
                {
                    writer.Write(this.Signature);
                    writer.Write(this.Version);

                    foreach (var instruction in this.Settings)
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

                    return stream.ToArray();
                }
            }
        }

        public RegistryFile()
        {
            this.Settings = new RegistrySetting[0];
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
                    setting.Type = (RegistryValueType)reader.ReadUInt32();
                    reader.ReadChar();
                    setting.Size = reader.ReadUInt32();
                    reader.ReadChar();
                    setting.BinaryData = reader.ReadBytes((int)setting.Size);
                    reader.ReadChar();
                    setting.Data = GetData(setting);

                    settings.Add(setting);
                }
            }

            this.Settings = settings.ToArray();
        }

        public void Save()
        {
            File.WriteAllBytes(this.Path, this.Data);
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
            switch (instruction.Type)
            {
                case RegistryValueType.REG_SZ:
                case RegistryValueType.REG_EXPAND_SZ:
                    return Encoding.Unicode.GetString(instruction.BinaryData).TrimEnd('\0');
                case RegistryValueType.REG_DWORD:
                    return BitConverter.ToUInt32(instruction.BinaryData, 0);
                case RegistryValueType.REG_QWORD:
                    return BitConverter.ToUInt64(instruction.BinaryData, 0);
                case RegistryValueType.REG_MULTI_SZ:
                    if (instruction.BinaryData.Length == 0) return new string[0];

                    var collection = new List<string>();
                    using (var stream = new MemoryStream(instruction.BinaryData))
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
                    return instruction.BinaryData;
            }
        }

        private static byte[] SetData(RegistrySetting instruction)
        {
            switch (instruction.Type)
            {
                case RegistryValueType.REG_SZ:
                case RegistryValueType.REG_EXPAND_SZ:
                    return Encoding.Unicode.GetBytes((string)instruction.Data + '\0');
                case RegistryValueType.REG_DWORD:
                    return BitConverter.GetBytes((uint)instruction.Data);
                case RegistryValueType.REG_QWORD:
                    return BitConverter.GetBytes((ulong)instruction.Data);
                case RegistryValueType.REG_MULTI_SZ:
                    return Encoding.Unicode.GetBytes(string.Join("\0", (string[])instruction.Data) + "\0\0");
                default:
                    return (byte[])instruction.Data;
            }
        }
    }    
}
