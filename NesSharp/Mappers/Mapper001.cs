using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesSharp.Mappers
{
	public class Mapper001 : Mapper
	{
		private int shiftreg;
		private int numwrites;

		public Mapper001(byte[] file)
		{
			rom = file;
			InitMemory(rom);
			SetupMapper();

			Array.Copy(rom, 0x10, ram, 0x8000, prgsize);
			Array.Copy(rom, 0x10 + prgsize * (prgbanks - 1), ram, 0xC000, prgsize);

			if (chrsize > 0)
				Array.Copy(rom, 0x10 + prgrom, vram, 0, chrrom);
			else
				Array.Copy(rom, 0x10 + prgsize * prgbanks, vram, 0, chrsize);
		}

		public override void CpuWrite(int addr, byte v)
		{
			if (addr >= 0x8000 && addr < 0x10000)
			{
				if ((v & 0x80) > 0)
				{
					shiftreg = 0x10;
					numwrites = 0;
				}
				else
				{
					numwrites++;
					shiftreg >>= 1;
					shiftreg |= ((v & 1) << 4) & 0x10;

					if (numwrites == 5)
						Array.Copy(rom, 0x10 + prgsize * shiftreg, ram, 0x8000, prgsize);
				}
			}
			else if (addr >= 0x8000 && addr < 0x9fff)
			{
				chrsize = (shiftreg & 0x10) == 0 ? 0x4000 : 0x2000;
				prgsize = (shiftreg & 0x08) == 0 ? 0x8000 : 0x4000;

			}
			else if (addr >= 0xa000 && addr < 0xbfff)
			{
				if (prgsize == 0x4000)
				{

				}
			}

			base.CpuWrite(addr, v);
		}

		public override byte CpuRead(int addr)
		{


			return base.CpuRead(addr);
		}

		public override void PpuWrite(int addr, byte v)
		{
			base.PpuWrite(addr, v);
		}
	}
}
