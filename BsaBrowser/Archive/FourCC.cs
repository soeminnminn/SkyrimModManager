using System;
using System.Text;
using System.Text.RegularExpressions;

namespace BsaBrowser.Archive
{
    public sealed class FourCC
    {
        #region Variables
        public const int LENGTH = 4;
        private static readonly Regex VALID_PATTERN = new Regex(@"^[A-Z0-9_\s\x00]{4}$");
        #endregion

        #region Constructors
        private FourCC()
        { }

        public FourCC(string fourCC)
            : this()
        {
            if (!string.IsNullOrEmpty(fourCC) && VALID_PATTERN.IsMatch(fourCC))
            {
                this.Magic = MAKEFOURCC(fourCC);
            }
        }

        public FourCC(uint fourCC)
            : this()
        {
            this.Magic = fourCC;
        }

        public FourCC(byte[] fourCC)
            : this()
        {
            var str = string.Empty;
            if (fourCC.Length >= LENGTH)
            {
                byte[] bytes = new byte[LENGTH];

                for (int i = 0; i < LENGTH; i++)
                {
                    bytes[i] = fourCC[i];
                }
                str = Encoding.ASCII.GetString(bytes);
            }

            if (!string.IsNullOrEmpty(str) && VALID_PATTERN.IsMatch(str))
            {
                this.Magic = MAKEFOURCC(str);
            }
        }
        #endregion

        #region Properties
        public uint Magic { get; private set; } = 0;
        #endregion

        #region Methods
        private static uint MAKEFOURCC(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            if (value.Length < 4) return 0;

            return MAKEFOURCC(value[0], value[1], value[2], value[3]);
        }

        private static uint MAKEFOURCC(char ch0, char ch1, char ch2, char ch3)
        {
            return ((uint)(byte)ch0 | ((uint)(byte)ch1 << 8) | ((uint)(byte)ch2 << 16 | ((uint)(byte)ch3 << 24)));
        }

        private static byte[] UintBytes(uint value)
        {
            return new byte[] { (byte)(value & 0xFF), (byte)((value >> 8) & 0xFF), (byte)((value >> 16) & 0xFF), (byte)((value >> 24) & 0xFF) };
        }

        public override string ToString()
        {
            byte[] bytes = UintBytes(this.Magic);
            return Encoding.ASCII.GetString(bytes);
        }

        public override int GetHashCode()
        {
            return this.Magic.GetHashCode();
        }

        public string GetHashString()
        {
            return "0x" + this.Magic.ToString("X");
        }
        #endregion

        #region Operators
        public static explicit operator FourCC(string value)
        {
            return new FourCC(value);
        }

        public static implicit operator string(FourCC fourCC)
        {
            return fourCC.ToString();
        }

        public static explicit operator FourCC(uint value)
        {
            return new FourCC(value);
        }

        public static implicit operator uint(FourCC fourCC)
        {
            return fourCC.Magic;
        }
        #endregion
    }
}
