using System;
using System.Timers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading.Tasks;

namespace Codec
{
	abstract class Codec
	{
		public abstract byte[] Encode(byte[] src);
		public abstract byte[] Decode(byte[] src);
	}

	class RFA : Codec
	{
        //Singleton
        private static RFA instance = null;
        public static RFA Instance {
            get {
                if (instance == null)
                    instance = new RFA();
                return instance;
            }
        }

		private uint seed;
		const ulong MAGIC_NUMBER = 42;
		const ulong MULTIPLIER = 10807;
		const ulong MOD = 0x7fffffff;

        public uint Seed
        {
            set{seed = value;}
            get{return seed;}
        }

        private uint rand() {
			seed = (uint)((seed*MULTIPLIER)&MOD);
			return (uint)((seed+MAGIC_NUMBER)*seed&MOD);
		}
        private uint _crypt(uint word, uint key) {
			return word^key^rand();
		}

		public RFA() 
        {
            seed = 42;
        }

		public override byte[] Encode(byte[] src) {
            MemoryStream buffer = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(buffer);

			Seed = (uint)DateTime.Now.Ticks;

			uint secretKey = rand();
			Seed = secretKey;

			writer.Write((uint)IPAddress.HostToNetworkOrder((int)secretKey));
			writer.Write((uint)IPAddress.HostToNetworkOrder((int)_crypt((uint)src.Length, secretKey)));

            // complement array to the multiple of 4
            byte[] complement = new byte[(src.Length + 3) / 4 * 4];
            src.CopyTo(complement, 0);
            BinaryReader reader = new BinaryReader(new MemoryStream(complement));

			secretKey = (uint)src.Length;
			for(int index = 0; index < src.Length; index+=4)
			{
				uint tmp = (uint)IPAddress.HostToNetworkOrder(reader.ReadInt32());
				writer.Write((uint)IPAddress.HostToNetworkOrder((int)_crypt(tmp, secretKey)));
				secretKey = tmp;
			}

            return buffer.ToArray();
		}
		public override byte[] Decode(byte[] src) {
			MemoryStream buffer = new MemoryStream();
            BinaryReader reader = new BinaryReader(new MemoryStream(src));
            BinaryWriter writer = new BinaryWriter(buffer);

			uint secretKey = (uint)IPAddress.NetworkToHostOrder(reader.ReadInt32());
			Seed = secretKey;

			uint size = _crypt((uint)IPAddress.NetworkToHostOrder(reader.ReadInt32()), secretKey);
			secretKey = size;

			for(int index = 8; index < src.Length; index += 4)
			{
				uint tmp = (uint)IPAddress.NetworkToHostOrder(reader.ReadInt32());
				tmp = _crypt(tmp, secretKey);
				writer.Write((uint)IPAddress.NetworkToHostOrder((int)tmp));
				secretKey = tmp;
			}

            return buffer.ToArray().Take((int)size).ToArray();
		}
	}
}