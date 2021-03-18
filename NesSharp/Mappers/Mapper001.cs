﻿using System;
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
		private int prgbankswitch;
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
				if ((v & 0x80) > 0)
				{
					shiftreg = 0x10;
					numwrites = 0;
				}
				else
				{
					if (numwrites == 0)
					{
						banknum = v;
						//Array.Copy(rom, 0x10 + prgsize * v, ram, 0x8000, prgsize);
					}

					numwrites++;
					shiftreg >>= 1;
					shiftreg |= ((banknum & 1) << 4) & 0x10;

					if (numwrites == 5)
					{
						if (addr >= 0x8000 && addr <= 0x9fff)
						{
							chrsize = (shiftreg & 0x10) > 0 ? 0x2000 : 0x1000;
							//prgsize = (shiftreg & 0x08) > 0 ? 0x4000 : 0x2000;

							switch (shiftreg >> 3 & 3)
							{
								case 0:
								case 1:
									prgsize = 0x8000;
									prgbankswitch = 0x8000;
									break;
								case 2:
									prgsize = 0x4000;
									prgbankswitch = 0xc000;
									break;
								case 3:
									prgsize = 0x4000;
									prgbankswitch = 0x8000;
									break;
							}

						}
						else if (addr >= 0xa000 && addr <= 0xbfff)
						{
							if (prgsize == 0x4000)
							Array.Copy(rom, 0x10 + chrsize * banknum, vram, 0, chrsize);
						}
						else if (addr >= 0xc000 && addr <= 0xdfff)
						{
							if (prgsize == 0x4000)
								Array.Copy(rom, 0x10 + chrsize * banknum, vram, 0, chrsize);
						}
						else if (addr >= 0xe000 && addr <= 0xffff)
						{
							if (prgsize == 0x8000)
								banknum /= 2;
							Array.Copy(rom, 0x10 + prgsize * banknum, ram, 0x8000, prgsize);
							banknum = shiftreg & 0x0f;
							if (prgsize == 0x8000)
								banknum = shiftreg >> 1;
							//Array.Copy(rom, 0x10 + prgsize * banknum, ram, 0x8000, prgsize);
						}
						shiftreg = 0x10;
						numwrites = 0;
					}
				}
			}
			else
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
