using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ModManager.GameModules
{
    public class Signature
    {
        #region Variables
        public const int LENGTH = 4;
        private static readonly Regex VALID_PATTERN = new Regex(@"^[A-Z1-9_]{4}$");
        private static readonly uint[] KnownSubRecordSignatures = {
            EDID, FULL, NAME, XCLC, MHDT
        };

        private readonly bool m_isValid = false;

        public static Signature DEFAULT = new Signature();
        #endregion

        #region Constructors
        private Signature()
        { }

        public Signature(string recordType)
            : this(b(recordType))
        {
        }

        public Signature(uint recordType)
            : this(BitConverter.GetBytes(recordType))
        {
        }

        public Signature(byte[] bytes)
        {
            var str = string.Empty;
            if (bytes.Length == LENGTH)
                str = Encoding.ASCII.GetString(bytes);
            else if (bytes.Length > LENGTH)
                str = Encoding.ASCII.GetString(bytes.Take(LENGTH).ToArray());

            if (!string.IsNullOrEmpty(str) && VALID_PATTERN.IsMatch(str)) {
                this.RecordType = b(str);
                this.m_isValid = true;
            }
        }
        #endregion

        #region Properties
        public uint RecordType { get; set; } = 0;

        public bool IsValid
        {
            get => m_isValid;
        }

        public bool IsHeader
        {
            get => this.RecordType == TES3 || this.RecordType == TES4; 
        }

        public bool IsGroup
        {
            get => this.RecordType == GRUP;
        }

        public bool IsSubRecord
        {
            get => Array.IndexOf(KnownSubRecordSignatures, this.RecordType) > -1;
        }

        public bool IsXXXX
        {
            get => this.RecordType == XXXX;
        }
        #endregion

        #region Methods
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

        public override string ToString()
        {
            byte[] bytes = BitConverter.GetBytes(this.RecordType);
            return Encoding.ASCII.GetString(bytes);
        }

        public override int GetHashCode()
        {
            return this.RecordType.GetHashCode();
        }

        public string GetHashString()
        {
            return "0x" + this.RecordType.ToString("X");
        }

        private static uint b(string signature)
        {
            if (signature == null || signature.Length != LENGTH) return 0;

            var bytes = Encoding.ASCII.GetBytes(signature);
            if (bytes.Length != LENGTH) return 0;

            return (uint)((bytes[3] << 24) | (bytes[2] << 16) | (bytes[1] << 8) | bytes[0]);
        }
        #endregion

        #region Operators
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
            return signature.RecordType;
        }
        #endregion

        #region RecordTypes
        public static readonly uint XXXX = b("XXXX");
        public static readonly uint TES3 = b("TES3");
        public static readonly uint TES4 = b("TES4");
        public static readonly uint GRUP = b("GRUP");
        public static readonly uint NAME = b("NAME");
        public static readonly uint DATA = b("DATA");
        public static readonly uint CELL = b("CELL");
        public static readonly uint PGRD = b("PGRD");
        public static readonly uint SCPT = b("SCPT");
        public static readonly uint INDX = b("INDX");
        public static readonly uint INAM = b("INAM");
        public static readonly uint INTV = b("INTV");
        public static readonly uint SCHD = b("SCHD");
        public static readonly uint GMST = b("GMST");
        public static readonly uint GLOB = b("GLOB");
        public static readonly uint CLAS = b("CLAS");
        public static readonly uint FACT = b("FACT");
        public static readonly uint RACE = b("RACE");
        public static readonly uint SOUN = b("SOUN");
        public static readonly uint REGN = b("REGN");
        public static readonly uint BSGN = b("BSGN");
        public static readonly uint LTEX = b("LTEX");
        public static readonly uint STAT = b("STAT");
        public static readonly uint DOOR = b("DOOR");
        public static readonly uint MISC = b("MISC");
        public static readonly uint WEAP = b("WEAP");
        public static readonly uint CONT = b("CONT");
        public static readonly uint SPEL = b("SPEL");
        public static readonly uint CREA = b("CREA");
        public static readonly uint BODY = b("BODY");
        public static readonly uint LIGH = b("LIGH");
        public static readonly uint ENCH = b("ENCH");
        public static readonly uint NPC_ = b("NPC_");
        public static readonly uint ARMO = b("ARMO");
        public static readonly uint CLOT = b("CLOT");
        public static readonly uint REPA = b("REPA");
        public static readonly uint ACTI = b("ACTI");
        public static readonly uint APPA = b("APPA");
        public static readonly uint LOCK = b("LOCK");
        public static readonly uint PROB = b("PROB");
        public static readonly uint INGR = b("INGR");
        public static readonly uint BOOK = b("BOOK");
        public static readonly uint ALCH = b("ALCH");
        public static readonly uint LEVI = b("LEVI");
        public static readonly uint LEVC = b("LEVC");
        public static readonly uint SNDG = b("SNDG");
        public static readonly uint DIAL = b("DIAL");
        public static readonly uint SKIL = b("SKIL");
        public static readonly uint MGEF = b("MGEF");
        public static readonly uint INFO = b("INFO");
        public static readonly uint LAND = b("LAND");
        public static readonly uint CNAM = b("CNAM");
        public static readonly uint HEDR = b("HEDR");
        public static readonly uint INCC = b("INCC");
        public static readonly uint MAST = b("MAST");
        public static readonly uint ONAM = b("ONAM");
        public static readonly uint SNAM = b("SNAM");
        public static readonly uint EDID = b("EDID");
        public static readonly uint FULL = b("FULL");
        public static readonly uint NAM1 = b("NAM1");
        public static readonly uint NAM2 = b("NAM2");
        public static readonly uint DESC = b("DESC");
        public static readonly uint TRDT = b("TRDT");
        public static readonly uint XCLC = b("XCLC");
        public static readonly uint MHDT = b("MHDT");
        #endregion
    }
}
