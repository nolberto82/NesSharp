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
		private int banknum;
		private int bank8000;
		private int banka000;
		private int bankc000;
		private int banke000;

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
				if (numwrites <= 5)
				{
					if ((v & 0x80) > 0)
					{
						shiftreg = 0x10;
						numwrites = 0;
						banknum = v;
					}
					else
					{
						if (numwrites == 0)
							banknum = v;

						numwrites++;
						shiftreg >>= 1;
						shiftreg |= ((v & 1) << 4) & 0x10;

						if (numwrites == 5)
							Array.Copy(rom, 0x10 + prgsize * banknum, ram, 0x8000, prgsize);
					}
					return;
				}

				if (addr >= 0x8000 && addr <= 0x9fff)
				{
					chrsize = (shiftreg & 0x10) == 0 ? 0x2000 : 0x1000;
					prgsize = (shiftreg & 0x08) == 0 ? 0x4000 : 0x2000;

				}
				else if (addr >= 0xa000 && addr <= 0xbfff)
				{
					if (banka000 == 0)
					{
						banknum = v;
					}

					banka000++;

					if (banka000 == 5)
					{
						Array.Copy(rom, 0x10 + chrsize * banknum, vram, 0, chrsize);
						banka000 = 0;
					}
				}
				else if (addr >= 0xc000 && addr <= 0xdfff)
				{
					//if (chrsize == 0x4000)
					//{
					Array.Copy(rom, 0x10 + chrsize * v, vram, 0, chrsize);
					//}
				}
				else if (addr >= 0xe000 && addr <= 0xffff)
				{
					if (banke000 == 0)
					{
						banknum = v;
					}

					banke000++;

					if (banke000 == 5)
					{
						Array.Copy(rom, 0x10 + prgsize * banknum, vram, 0, prgsize);
						banke000 = 0;
					}
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
