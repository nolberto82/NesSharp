using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesSharp.Mappers
{
	public class Mapper002 : Mapper
	{
		public Mapper002(byte[] file)
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
			if (addr >=0x8000 && addr < 0x10000)
			{
				int bank = v & 7;
				Array.Copy(rom, 0x10 + prgsize * bank, ram, 0x8000, prgsize);
				return;
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
