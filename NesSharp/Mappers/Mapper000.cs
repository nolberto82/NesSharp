using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesSharp.Mappers
{
	public class Mapper000 : Mapper
	{
		public Mapper000(byte[] file)
		{
			rom = file;
			InitMemory(rom);
			SetupMapper();

			if (prgbanks == 1)
			{
				Array.Copy(rom, 0x10, ram, 0xC000, prgrom);
				Array.Copy(ram, 0xC000, ram, 0x8000, prgrom);
				Array.Copy(rom, 0x10 + prgrom, vram, 0, chrrom);
			}
			else
			{
				Array.Copy(rom, 0x10, ram, 0x8000, prgsize);
				Array.Copy(rom, 0x10 + prgsize, ram, 0xC000, prgsize);
				if (chrsize > 0)
					Array.Copy(rom, 0x10 + prgrom, vram, 0, chrrom);
			}
		}

		public override void CpuWrite(int addr, byte v)
		{
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

		public override void SetupMapper()
		{
			base.SetupMapper();
		}
	}
}
