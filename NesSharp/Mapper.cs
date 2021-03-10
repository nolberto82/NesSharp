using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using u8 = System.Byte;
using u16 = System.UInt16;

namespace NesSharp
{
    public class Mapper
    {
		public byte[] ram;
		public byte[] rom;
		public byte[] vram;
		private Main c;

		public Mapper(Main core, string gamename)
        {
            c = core;

            if (!File.Exists(gamename))
                Environment.Exit(1);

            rom = File.ReadAllBytes(gamename);

            if (Encoding.ASCII.GetString(rom, 0, 3) != "NES")
            {
                return;
            }

            ram = new byte[0x10000];
            vram = new byte[0x4000];

            SetUpMapper();
        }

		public byte CpuRead(int addr)
		{
			switch (addr)
			{
				case 0x2002:
					return c.ppu.StatusRead((u16)addr);
				case 0x2007:
					return c.ppu.DataRead();
				case 0x4016:
					return c.control.ControlRead();
				default:
					break;
			}

			return ram[addr];
		}

		public u8 CpuReadDebug(int addr)
		{
			return ram[addr];
		}

		public void CpuWrite(int addr, byte v)
		{
			switch (addr)
			{
				case 0x2000:
					c.ppu.ControlWrite(v);
					break;
				case 0x2001:
					c.ppu.MaskWrite(v);
					break;
				case 0x2003:
					c.ppu.OamAddrWrite(v);
					break;
				case 0x2004:
					c.ppu.OamDataWrite(v);
					break;
				case 0x2005:
					c.ppu.ScrollWrite(v);
					break;
				case 0x2006:
					c.ppu.AddrWrite(v);
					break;
				case 0x2007:
					c.ppu.DataWrite(v);
					break;
				case 0x4014:
					c.ppu.ppuoamdma = v;
					break;
				case 0x4016:
					c.control.ControlWrite(v);
					break;
				default:
					ram[addr] = v;
					break;
			}
		}

		public byte PpuRead(int addr)
		{
			return vram[addr];
		}

		public void PpuWrite(int addr, byte v)
		{
			for (int i = 0; i < 32; i++)
				Buffer.BlockCopy(vram, 0x3f10, vram,0x3f00 + i * 0x4,1);

			for (int i = 0; i < 7; i++)
				Buffer.BlockCopy(vram, 0x3f00, vram, 0x3f00 + i * 0x20, 1);

			vram[addr & 0x3fff] = v;
		}

		private void SetUpMapper()
		{
			int prgbanks = rom[4];
			int chrbanks = rom[5];
			int prgrom = prgbanks * 0x4000;
			int chrrom = chrbanks * 0x2000;
			int prgsize = prgrom / prgbanks;
			int chrsize = chrrom > 0 ? chrrom / chrbanks : 0;
			int mappernum = (rom[6] & 0xf0)>> 4;
			c.ppu.mirrornametable = 0;

			if ((rom[6] & 0x08) > 0)
			{
				c.ppu.mirrornametable = 2;
			}
			else if ((rom[6] & 0x01) > 0)
			{
				c.ppu.mirrornametable = 1;
			}

			switch (mappernum)
			{
				case 0:
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
					break;
				case 1:

					break;
			}
		}
	}
}
