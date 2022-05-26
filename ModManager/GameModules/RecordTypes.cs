using System;
using System.Linq;

namespace ModManager.GameModules
{
  public class RecordTypes
  {
    public const int RECORD_TYPE_LENGTH = 4;

    public static string bs(byte[] bytes)
    {
      if (bytes.Length != RECORD_TYPE_LENGTH) return "XXXX";
      return new String(bytes.Select(x => Convert.ToChar(x)).ToArray());
    }

    public static int bi(byte[] bytes)
    {
      if (bytes.Length != RECORD_TYPE_LENGTH) return 0;
      return (bytes[3] << 24) | (bytes[2] << 16) | (bytes[1] << 8) | bytes[0];
    }

    public static byte[] sb(string signature)
    {
      if (signature.Length != RECORD_TYPE_LENGTH) return new byte[] {};
      return signature.ToCharArray().Select(x => Convert.ToByte(x)).ToArray();
    }

    public static int b(string signature)
    {
      var bytes = sb(signature);
      return bi(bytes);
    }


    public static readonly int XXXX = b("XXXX");
    public static readonly int TES4 = b("TES4");
    public static readonly int NAME = b("NAME");
    public static readonly int DATA = b("DATA");
    public static readonly int CELL = b("CELL");
    public static readonly int PGRD = b("PGRD");
    public static readonly int SCPT = b("SCPT");
    public static readonly int INDX = b("INDX");
    public static readonly int INAM = b("INAM");
    public static readonly int INTV = b("INTV");
    public static readonly int SCHD = b("SCHD");
    public static readonly int GMST = b("GMST");
    public static readonly int GLOB = b("GLOB");
    public static readonly int CLAS = b("CLAS");
    public static readonly int FACT = b("FACT");
    public static readonly int RACE = b("RACE");
    public static readonly int SOUN = b("SOUN");
    public static readonly int REGN = b("REGN");
    public static readonly int BSGN = b("BSGN");
    public static readonly int LTEX = b("LTEX");
    public static readonly int STAT = b("STAT");
    public static readonly int DOOR = b("DOOR");
    public static readonly int MISC = b("MISC");
    public static readonly int WEAP = b("WEAP");
    public static readonly int CONT = b("CONT");
    public static readonly int SPEL = b("SPEL");
    public static readonly int CREA = b("CREA");
    public static readonly int BODY = b("BODY");
    public static readonly int LIGH = b("LIGH");
    public static readonly int ENCH = b("ENCH");
    public static readonly int NPC_ = b("NPC_");
    public static readonly int ARMO = b("ARMO");
    public static readonly int CLOT = b("CLOT");
    public static readonly int REPA = b("REPA");
    public static readonly int ACTI = b("ACTI");
    public static readonly int APPA = b("APPA");
    public static readonly int LOCK = b("LOCK");
    public static readonly int PROB = b("PROB");
    public static readonly int INGR = b("INGR");
    public static readonly int BOOK = b("BOOK");
    public static readonly int ALCH = b("ALCH");
    public static readonly int LEVI = b("LEVI");
    public static readonly int LEVC = b("LEVC");
    public static readonly int SNDG = b("SNDG");
    public static readonly int DIAL = b("DIAL");
    public static readonly int SKIL = b("SKIL");
    public static readonly int MGEF = b("MGEF");
    public static readonly int INFO = b("INFO");
    public static readonly int LAND = b("LAND");
    public static readonly int CNAM = b("CNAM");
    public static readonly int HEDR = b("HEDR");
    public static readonly int INCC = b("INCC");
    public static readonly int MAST = b("MAST");
    public static readonly int ONAM = b("ONAM");
    public static readonly int SNAM = b("SNAM");
    public static readonly int EDID = b("EDID");
    public static readonly int FULL = b("FULL");
    public static readonly int NAM1 = b("NAM1");
    public static readonly int NAM2 = b("NAM2");
    public static readonly int DESC = b("DESC");
    public static readonly int TRDT = b("TRDT");
  }
}