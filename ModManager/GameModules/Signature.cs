using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ModManager.GameModules
{
    public class Signature
    {
        public const int LENGTH = 4;
        private static readonly Regex VALID_PATTERN = new Regex(@"^[A-Z1-9_]{4}$");
        private static readonly string[] KnownSubRecordSignatures = { 
            "EDID", "FULL", "NAME", "XCLC", "MHDT"
        };

        public string RecordType { get; set; } = string.Empty;

        private readonly bool m_isValid = false;

        private Signature()
        { }

        public Signature(string recordType)
        {
            RecordType = recordType;
        }

        public Signature(byte[] bytes)
        {
            var str = string.Empty;
            if (bytes.Length == LENGTH)
                str = new string(bytes.Select(x => Convert.ToChar(x)).ToArray());
            else if (bytes.Length > LENGTH)
                str = new string(bytes.Take(LENGTH).Select(x => Convert.ToChar(x)).ToArray());

            if (!string.IsNullOrEmpty(str) && VALID_PATTERN.IsMatch(str)) {
                this.RecordType = str;
                this.m_isValid = true;
            }
        }

        public Signature(uint recordType)
            : this(BitConverter.GetBytes(recordType))
        {
        }

        public bool IsValid
        {
            get => m_isValid;
        }

        public bool IsHeader
        {
            get => this.RecordType == "TES3" || this.RecordType == "TES4"; 
        }

        public bool IsGroup
        {
            get => this.RecordType == "GRUP";
        }

        public bool IsSubRecord
        {
            get => Array.IndexOf(KnownSubRecordSignatures, this.RecordType) > -1;
        }

        public bool IsXXXX
        {
            get => this.RecordType == "XXXX";
        }

        public static Signature ReadFrom(Stream stream)
        {
            if (stream.Length > LENGTH && stream.CanRead)
            {
                var bytes = new byte[LENGTH];
                stream.Read(bytes, 0, LENGTH);
                return new Signature(bytes);
            }
            return DEFAULT;
        }

        public static Signature DEFAULT = new Signature();

        public override string ToString()
        {
            return this.RecordType;
        }

        public override int GetHashCode()
        {
            return this.RecordType.GetHashCode();
        }

        public static explicit operator Signature(string recordType)
        {
            return new Signature(recordType);
        }

        public static implicit operator string(Signature signature)
        {
            return signature.ToString();
        }

        public static explicit operator Signature(uint recordType)
        {
            return new Signature(recordType);
        }

        public static implicit operator uint(Signature signature)
        {
            var str = signature.ToString();
            if (string.IsNullOrEmpty(str)) return 0;
            return BitConverter.ToUInt32(Encoding.ASCII.GetBytes(str));
        }
    }
}
