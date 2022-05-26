using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.GameModules
{
    public interface IRecord
    {
        public GameID GameId { get; }

        public Signature RecordType { get; set; }

        public uint DataSize { get; set; }

        public bool Compressed { get; set; }

        public byte[] Data { get; set; }

        public void CopyTo(IRecord other);
    }
}
