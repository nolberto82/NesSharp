using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using u16 = System.UInt16;
using u8 = System.Byte;

namespace NesSharp
{
	public class Ppu
	{
		public byte[] gfxdata;
		public u8 mirrornametable;
		public int[] palettes;
		public int ppu_scanline;
		public int[][] ppugfxdata;
		public bool ppunmi;
		public u8 ppuoamdata;
		private bool background8px;
		private bool backgroundrender;
		private Core c;
		private Image emuimg;
		private Sprite emusprite;
		private Texture emutex;
		private bool latchtoggle;
		private int nametableaddr;
		private Image nametableimg;
		private List<u8> oammem;
		private int patternaddr;
		private u8 ppu_cyc;
		private int ppu_dots;
		private u8 ppu_dummy2007;
		private u16 ppu_t;
		private u16 ppu_v;
		private bool ppu_w;
		private u16 ppu_x;
		private u8 ppuaddr;
		private u8 ppuctrl;
		private u8 ppudata;
		private u8 ppumask;
		private u8 ppuoamaddr;
		public u8 ppuoamdma;
		private u8 ppuscroll;
		private u8 ppustatus;
		private u8 scroll_x;
		private u8 scroll_y;
		private bool sprites8px;
		private bool spritesrender;
		private bool vramaddrincrease;
		private RenderWindow window;

		public Ppu(Core core, RenderWindow w)
		{
			c = core;
			window = w;
			ppu_scanline = 0;
			//oamdata = 2;

			gfxdata = new byte[256 * 240 * 4];
			ppugfxdata = new int[2][];
			ppugfxdata[0] = new int[128 * 128];
			ppugfxdata[1] = new int[128 * 128];

			nametableimg = new Image(256, 240, new Color(155, 55, 55));
			emuimg = new Image(256, 240);
			emutex = new Texture(emuimg);
			emusprite = new Sprite();

			if (palettes == null)
			{
				byte[] temp = File.ReadAllBytes("palettes/ASQ_realityA.pal");
				palettes = new int[temp.Length / 3];

				for (int i = 0; i < temp.Length / 3; i++)
				{
					int[] number = new int[1];
					Buffer.BlockCopy(temp, i * 3, number, 0, 3);
					palettes[i] = number[0];
				}
			}
		}

		public enum Mirror
		{
			Horizontal,
			Vertical,
			FourScreen,
			SingleScreen
		}
		public void AddrWrite(u8 val) //0x2006
		{
			if (!ppu_w)
			{
				ppu_t = (u16)((ppu_t & ~0b1111111100000000) | (val & 0b00111111) << 8);// (ppu_t & 0xff) | val << 8;
			}
			else
			{
				ppuctrl &= 0xfc;
				ppu_t = (u16)((ppu_t & ~0b11111111));
				ppu_t |= val;// (ppu_t & 0xff00) | val;
				ppu_v = ppu_t;
			}

			ppu_w = !ppu_w;
		}

		public void ClearVBlank()
		{
			ppustatus &= 0x7f;
			c.mapper.ram[0x2002] = ppustatus;
		}

		public void ControlWrite(u8 val)
		{
			ppuctrl = val;

			ppu_t = (u16)(ppu_t & ~0b00110000000000);
			ppu_t |= (u16)((val & 3) << 10);

			if ((val & 0x10) > 0)
			{
				ppustatus |= val;
				c.mapper.ram[0x2002] |= val;
			}

			vramaddrincrease = (ppuctrl & 0x04) > 0;
		}

		public u8 DataRead()
		{
			u8 val = 0;
			if (ppu_v < 0x3f00)
			{
				val = ppu_dummy2007;
				ppu_dummy2007 = c.mapper.vram[ppu_v];
			}

			if (vramaddrincrease)
			{
				ppu_v += 32;
			}
			else
			{
				ppu_v++;
			}
			return val;
		}

		public void DataWrite(u8 val) //0x2007
		{
			c.mapper.PpuWrite(ppu_v, val);

			if ((ppuctrl & 0x04) > 0)
				ppu_v += 32;
			else
				ppu_v++;
		}
		public void DrawFrame()
		{
			emutex.Update(gfxdata);
			emusprite.Texture = emutex;
			window.Draw(emusprite);
			window.Display();
			//File.WriteAllBytes("gfxdata.bin",gfxdata);
		}

		public void MaskWrite(u8 val) //0x2001
		{
			ppumask |= val;

			backgroundrender = (ppumask & 0x08) > 0;
			spritesrender = (ppumask & 0x10) > 0;
		}

		public void OamAddrWrite(byte val) //0x2003
		{
			ppuoamaddr = val;
			oammem = new List<byte>();
		}

		public void OamDataWrite(u8 val) //0x2004
		{
			ppuoamdata = val;
			oammem.Add(val);
		}

		public void RenderScanline()
		{
			if (ppu_scanline < 239)
			{
				if (spritesrender)
				{
					int oamaddr = 0x0100 * ppuoamdma;
					u8 y = c.mapper.ram[oamaddr + 0];
					u8 x = c.mapper.ram[oamaddr + 3];

					if ((y + 8) == ppu_scanline)
						SetSpriteZero();
				}

				if ((ppumask & 0x10) > 0) //is sprite rendering on?
				{
					//RenderSprites();
				}

				if (backgroundrender) //is background rendering on?
				{
					//PPUViewer();
					RenderBackground();
				}

				if (spritesrender) //is sprite rendering on?
				{
					RenderSprites(0x20);
				}

				//gfxdata[256 * ppu_scanline] = palettes[c.mapper.PpuRead(0x3f00)];
			}
			else if (ppu_scanline == 241)
			{
				if (ppu_cyc == 0)
				{
					SetVBlank();
					SetNMI();
					SetSpriteZero();
					DrawFrame();
					ppu_cyc++;
				}
			}
			else if (ppu_scanline == 261)
			{
				if (ppu_cyc == 1)
				{
					ClearVBlank();
					ClearSpriteZero();
					ppu_cyc = 0;
				}
			}
			else if (ppu_scanline == 262)
			{
				ppu_cyc = 0;
				ppu_scanline = -1;
				ppunmi = false;
			}

			ppu_scanline++;
			if (ppu_dots < 341)
				ppu_dots++;
			else
				ppu_dots = 0;
		}

		public void ScrollWrite(u8 val) //0x2005
		{
			if (!ppu_w)
			{
				ppu_t = (u16)((ppu_t & ~0b00011111) | (val >> 3) & 0b00011111);// (ppu_t & 0x7fe0) | (val >> 3);
				ppu_x = (u16)(val & 0x07);
				scroll_x = val;
			}
			else
			{
				ppu_t = (u16)(ppu_t & ~0b111001111100000);
				ppu_t |= (u16)((val & 7) << 12 | (val & 0b11111000) << 2);//(ppu_t & 0xc1f) | ((val & 0x07) << 12) | ((val & 0xF8) << 2);
				scroll_y = val;
			}

			ppu_w = !ppu_w;
		}

		public void SetNMI()
		{
			if ((ppuctrl & 0x80) > 0)
			{
				ppunmi = true;
			}
		}

		public u8 StatusRead(u16 addr)
		{
			u8 val = ppustatus;
			ClearVBlank();
			c.mapper.ram[addr] = ppustatus;
			ppu_w = false;
			return val; ;
		}
		private void ClearSpriteZero()
		{
			ppustatus &= 0xbf;
		}

		private int GetAttributeIndex(int x, int y, int attrib)
		{
			//get the right attribute
			if ((y & 2) > 0)
			{
				if ((x & 2) > 0)
					return (attrib & 0xc0) >> 6;
				else
					return (attrib & 0x30) >> 4;
			}
			else
			{
				if ((x & 2) > 0)
					return (attrib & 0x0c) >> 2;
				else
					return (attrib & 0x03) >> 0;
			}
		}

		private void NameTableViewer()
		{
			for (int y = 0; y < 30; y++)
			{
			}
		}

		private void PPUViewer()
		{
			for (int i = 0; i < 2; i++)
			{
				int patternaddr = i * 0x1000;
				int tileid = 0;
				for (int y = 0; y < 16; y++)
				{
					for (int x = 0; x < 16; x++)
					{
						for (int r = 0; r < 8; r++)
						{
							int byte1 = c.mapper.PpuRead(patternaddr + (tileid * 16) + r + 0);
							int byte2 = c.mapper.PpuRead(patternaddr + (tileid * 16) + r + 8);

							for (int cl = 0; cl < 8; cl++)
							{
								int bit0 = (byte1 & 1) > 0 ? 1 : 0;
								int bit1 = (byte2 & 1) > 0 ? 2 : 0;

								byte1 >>= 1;
								byte2 >>= 1;

								int palindex = bit0 | bit1;
								if (palindex == 0)
								{
									ppugfxdata[i][128 * r + (7 - cl) + x * 8 + y * 128 * 8] = palettes[c.mapper.PpuRead(0x3f00)];
									continue;
								}

								int pixel = palettes[c.mapper.PpuRead(0x3f00 + palindex)];
								ppugfxdata[i][128 * r + (7 - cl) + x * 8 + y * 128 * 8] = pixel;
							}
						}
						tileid++;
					}
				}
			}
		}

		private void RenderBackground()
		{
			int patternaddr = (ppuctrl & 0x10) > 0 ? 0x1000 : 0x0000;
			int paladdr = (ppuctrl & 0x10) > 0 ? 0x3f00 : 0x3f10;
			int left8 = (ppuctrl & 0x02) > 0 ? 1 : 0;
			int y = (ppu_scanline / 8);// +scroll_y;

			int sx = scroll_x;// +(ppuctrl & 1 ? 256 : 0);
			int xMin = (sx / 8) + left8;
			int xMax = (sx + 256) / 8;

			for (int x = xMin; x <= xMax; x++)
			{
				int addr = 0;
				int natx = 0;

				if (x < 32)
				{
					addr = 0x2000 + 32 * y + x;
				}
				else if (x < 64)
				{
					addr = 0x2400 + 32 * y + (x - 32);
					natx = 32;
				}
				else
				{
					addr = 0x2800 + 32 * y + (x - 64);
					natx = 64;
				}

				if (ppuctrl == 0x89)
				{
					int yu = 0;
				}

				if ((ppuctrl & 1) > 0)
					addr ^= 0x400;

				if (addr == 0x2040)
				{
					int yy = 0;
				}

				int offx = x * 8 - sx;
				int offy = y * 8;

				int baseaddr = addr & 0x2c00;

				int tileid = c.mapper.vram[addr];

				if (addr == 0x2084)
				{
					int yu = 0;
				}

				int bit2 = GetAttributeIndex(addr & 0x1f, ((addr & 0x3e0) >> 5), c.mapper.vram[baseaddr + 0x3c0 + (y / 4) * 8 + ((x - natx) / 4)]);

				for (int row = 0; row < 8; row++)
				{
					byte byte1 = c.mapper.vram[patternaddr + tileid * 16 + row + 0];
					byte byte2 = c.mapper.vram[patternaddr + tileid * 16 + row + 8];

					for (int col = 0; col < 8; col++)
					{
						int xp = offx + (7 - col);
						int yp = offy + row;

						if (xp < 0 || xp >= 256 || yp < 0 || yp >= 240)
							continue;

						int bit0 = (byte1 & 1) > 0 ? 1 : 0;
						int bit1 = (byte2 & 1) > 0 ? 1 : 0;

						byte1 >>= 1;
						byte2 >>= 1;

						int colorindex = bit2 * 4 + (bit0 | bit1 * 2);

						int color = palettes[c.mapper.PpuRead(paladdr | colorindex)];
						gfxdata[(yp * 256 * 4) + (xp * 4) + 0] = (byte)(color >> 0);
						gfxdata[(yp * 256 * 4) + (xp * 4) + 1] = (byte)(color >> 8);
						gfxdata[(yp * 256 * 4) + (xp * 4) + 2] = (byte)(color >> 16);
						gfxdata[(yp * 256 * 4) + (xp * 4) + 3] = 255;

						//DrawFrame();
					}
				}
			}
		}

		private void RenderSprites(u8 frontback)
		{
			int oamaddr = 0x0100 * ppuoamdma;
			int nametableaddr = 0x2000 | ppuctrl & 3 * 0x400;
			int patternaddr = (ppuctrl & 0x08) > 0 ? 0x1000 : 0x0000;
			int paladdr = (ppuctrl & 0x10) > 0 ? 0x3f10 : 0x3f00;
			int left8 = (ppuctrl & 0x04) > 0 ? 1 : 0;

			int y, tileid, att, x, i;

			for (int j = 64; j > 0; j--)
			{
				i = j % 64;

				if (ppuoamdma == 0)
				{
					y = oammem[i * 4 + 0] + 1 & 0xff;
					tileid = oammem[i * 4 + 1];
					att = oammem[i * 4 + 2];
					x = oammem[i * 4 + 3] & 0xff + left8;
				}
				else
				{
					y = c.mapper.ram[oamaddr | i * 4 + 0];
					tileid = c.mapper.ram[oamaddr | i * 4 + 1];
					att = c.mapper.ram[oamaddr | i * 4 + 2];
					x = c.mapper.ram[oamaddr | i * 4 + 3];
				}

				if ((att & frontback) > 0)
					continue;

				if (y >= 0xef || x >= 0xf9)
					continue;

				bool flipH = (att & 0x40) > 0;
				bool flipV = (att & 0x80) > 0;

				for (int r = 0; r < 8; r++)
				{
					u8 byte1 = c.mapper.vram[patternaddr + tileid * 16 + r + 0];
					u8 byte2 = c.mapper.vram[patternaddr + tileid * 16 + r + 8];

					for (int cl = 0; cl < 8; cl++)
					{
						int col = 7 - cl;
						int row = r;

						if (flipH & flipV)
						{
							col = cl;
							row = 7 - r;
						}
						else if (flipV)
						{
							row = 7 - r;
						}
						else if (flipH)
						{
							col = cl;
						}

						int bit0 = (byte1 & 1) > 0 ? 1 : 0;
						int bit1 = (byte2 & 1) > 0 ? 1 : 0;

						byte1 >>= 1;
						byte2 >>= 1;

						int palindex = bit0 | bit1 * 2;

						int colorindex = palindex + (att & 3) * 4;

						int xp = x + col;
						int yp = y + row;
						if (xp < 0 || xp >= 255 || yp < 0 || yp >= 240)
							continue;

						if (palindex != 0)
						{
							int color = palettes[c.mapper.PpuRead(paladdr | colorindex)];
							gfxdata[256 * (y + row) * 4 + (x + col) * 4 + 0] = (byte)(color >> 0);
							gfxdata[256 * (y + row) * 4 + (x + col) * 4 + 1] = (byte)(color >> 8);
							gfxdata[256 * (y + row) * 4 + (x + col) * 4 + 2] = (byte)(color >> 16);
							gfxdata[256 * (y + row) * 4 + (x + col) * 4 + 3] = 255;
						}
					}
				}
			}
		}

		private void SetSpriteZero()
		{
			ppustatus |= 0x40;
		}

		private void SetVBlank()
		{
			ppustatus |= 0x80;
		}
	}
}