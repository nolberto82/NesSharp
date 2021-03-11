using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using u8 = System.Byte;
using u16 = System.UInt16;
using System.Linq;
using NesSharp.Mappers;

namespace NesSharp
{
	public class Mapper
	{
		public byte[] ram;
		public byte[] rom;
		public byte[] vram;
		public byte[] oam;
		public int prgbanks;
		public int chrbanks;
		public int prgrom;
		public int chrrom;
		public int prgsize;
		public int chrsize;
		public int mappernum;
		private Nes c;

		public Mapper()
		{
			c = Nes.Instance;
		}

		public virtual byte CpuRead(int addr)
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

		public virtual void CpuWrite(int addr, byte v)
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
					int oamaddr = v << 8;
					for (int i = 0; i < 256; i++)
						oam[i] = ram[oamaddr + i];
					c.cpu.ppucycles += 513;
					break;
				case 0x4016:
					c.control.ControlWrite(v);
					break;
				default:
					ram[addr] = v;
					break;
			}

			if (c.cpu.breakpoints.Count > 0)
			{
				var res = c.cpu.breakpoints.FirstOrDefault(b => b.Offset == addr);
				if (res != null && res.BpType == Breakpoint.Type.Write)
				{
					c.cpu.Breakmode = true;
				}
			}
		}

		public byte PpuRead(int addr)
		{
			return vram[addr];
		}

		public virtual void PpuWrite(int addr, byte v)
		{
			for (int i = 0; i < 6; i++)
				Buffer.BlockCopy(vram, 0x3f00, vram, 0x3f20 + i * 32, 32);

			for (int i = 0; i < 3; i++)
				Buffer.BlockCopy(vram, 0x3f10, vram, 0x3f04 + i * 0x4, 1);

			if (addr == 0x3f10)
				vram[0x3f00] = v;
			else if (addr == 0x3f00)
				vram[0x3f10] = v;
			else if (addr == 0x3f00)
				vram[0x3f10] = v;

			vram[addr & 0x3fff] = v;
		}

		public virtual bool InitMemory(byte[] rom)
		{
			//rom = File.ReadAllBytes(c.gui.gamename);

			if (Encoding.ASCII.GetString(rom, 0, 3) != "NES")
			{
				return false;
			}

			ram = new byte[0x10000];
			vram = new byte[0x4000];
			oam = new byte[0x100];

			//SetupMapper();

			return true;
		}

		public virtual void SetupMapper()
		{
			prgbanks = rom[4];
			chrbanks = rom[5];
			prgrom = prgbanks * 0x4000;
			chrrom = chrbanks * 0x2000;
			prgsize = prgrom / prgbanks;
			chrsize = chrrom > 0 ? chrrom / chrbanks : 0;
			mappernum = (rom[6] & 0xf0) >> 4;
		}
	}
}
