namespace NesSharp
{
    using ImGuiNET;
    using Saffron2D.GuiCollection;
    using SFML.Graphics;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using ImVec2 = System.Numerics.Vector2;
    using u16 = System.UInt16;
    using u8 = System.Byte;

    public class Ppu
    {
        public byte[] gfxdata;

        public byte[] sp0data;

        public u8 mirrornametable;

        public int[] palettes;

        public int ppu_scanline;

        public int[][] ppugfxdata;

        public bool ppunmi;

        public u8 ppuoamdata;

        public Sprite emusprite;

        public Texture emutex;

        private bool background8px;

        public bool isbackgroundrendering;

        private Nes c;

        private Image emuimg;

        private int nametableaddr;

        private Image nametableimg;

        private List<u8> oammem;

        private int patternaddr;

        private u8 ppu_cyc;

        private int ppu_dots;

        private u8 ppu_dummy2007;

        private int loopyT;

        private int loopyV;

        private bool ppu_w;

        private u8 ppu_x;

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

        private bool spritesize;

        private bool vramaddrincrease;

        private RenderWindow window;

        public ulong frame;

        private bool ppuready;

        private int cycle;

        private bool sprite0;

        private bool vblank;

        public Ppu(RenderWindow w)
        {
            c = Nes.Instance;
            window = w;

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
            /// <summary>
            /// Defines the Horizontal.
            /// </summary>
            Horizontal,
            /// <summary>
            /// Defines the Vertical.
            /// </summary>
            Vertical,
            /// <summary>
            /// Defines the FourScreen.
            /// </summary>
            FourScreen,
            /// <summary>
            /// Defines the SingleScreen.
            /// </summary>
            SingleScreen
        }

        public int GetVramAddress()
        {
            return loopyV;
        }

        public void Reset()
        {
            ppu_scanline = 0;
            ppumask = 0;

            gfxdata = new byte[256 * 256 * 4];
            sp0data = new byte[256 * 256 * 4];
            ppugfxdata = new int[2][];
            ppugfxdata[0] = new int[128 * 128];
            ppugfxdata[1] = new int[128 * 128];

            nametableimg = new Image(256, 240, new Color(155, 55, 55));
            emuimg = new Image(256, 240);
            emutex = new Texture(emuimg);
            emusprite = new Sprite();
        }

        public void ControlWrite(u8 val) //0x2000
        {
            ppuctrl = val;

            loopyT &= 0x73ff;
            nametableaddr = 0x2000 | (val & 3) << 10;

            if ((val & 0x10) > 0)
            {
                ppustatus |= val;
                c.mapper.ram[0x2002] |= val;
            }

            vramaddrincrease = (ppuctrl & 0x04) > 0;
            spritesize = (ppuctrl & 0x20) > 0;
        }

        public void MaskWrite(u8 val) //0x2001
        {
            ppumask = val;

            isbackgroundrendering = (ppumask & 0x08) > 0;
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
        }

        public void ScrollWrite(u8 val) //0x2005
        {
            if (ppu_w)
            {
                loopyT = loopyT & 0x7fe0 | (val & 0xf8) >> 3;
                scroll_y = (u8)(val & 7);
            }
            else
            {
                loopyT = (loopyT & 0xc1f) | (val & 7) << 12 | (val & 0xf8);// (ppu_t & 0x7fe0) | (val >> 3);
                ppu_x = (u8)(val & 0x07);
                scroll_x = val;
            }

            ppu_w = !ppu_w;
        }

        public void AddrWrite(u8 val) //0x2006
        {
            if (!ppu_w)
            {
                loopyT = (u16)((loopyT & 0x80ff) | (val & 0x3f) << 8);// (ppu_t & 0xff) | val << 8;
            }
            else
            {
                ppuctrl &= 0xfe;
                loopyT = loopyT & 0xff00 | val;
                loopyV = loopyT;
            }

            ppu_w = !ppu_w;
        }

        public void DataWrite(u8 val) //0x2007
        {
            c.mapper.PpuWrite(loopyV, val);

            if ((ppuctrl & 0x04) > 0)
                loopyV += 32;
            else
                loopyV++;
        }

        public u8 DataRead()
        {
            u8 val = 0;
            if (loopyV < 0x3f00)
            {
                val = ppu_dummy2007;
                ppu_dummy2007 = c.mapper.vram[loopyV];
            }

            if (vramaddrincrease)
                loopyV += 32;
            else
                loopyV++;
            return val;
        }

        public void DrawFrame()
        {
            emutex.Update(gfxdata);
            emusprite.Texture = emutex;
            window.Draw(emusprite);
            window.Display();
        }

        public void RenderScanline()
        {
            if (ppu_scanline < 239)
            {
                if (isbackgroundrendering) //is background rendering on?
                    RenderBackgroundNew();

                if (spritesrender) //is sprite rendering on?
                    RenderSprites(0x20);
            }
            else if (ppu_scanline == 241)
            {
                if (ppu_cyc == 0)
                {
                    SetVBlank();
                    SetNMI();
                    ClearSpriteZero();
                    //SetSpriteZero();
                    //DrawFrame();
                    ppu_cyc++;
                }
            }
            else if (ppu_scanline == 261)
            {
                if (ppu_cyc == 1)
                {
                    ClearVBlank();
                    //ClearSpriteZero();
                    ppu_cyc = 2;
                }
            }
            else if (ppu_scanline == 262)
            {
                ppu_cyc = 0;
                ppu_scanline = -1;
                ppunmi = false;
                frame++;
            }

            ppu_scanline++;
            if (ppu_dots < 341)
                ppu_dots++;
            else
                ppu_dots = 0;
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
            if ((ppustatus & 0x80) > 0)
            {
                int yu = 0;
            }
            u8 val = ppustatus;
            ClearVBlank();
            c.mapper.ram[addr] = ppustatus;
            ppu_w = false;
            return val;
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
                            int byte1 = c.mapper.vram[patternaddr + (tileid * 16) + r + 0];
                            int byte2 = c.mapper.vram[patternaddr + (tileid * 16) + r + 8];

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

        public void Step()
        {
            int ntaddr = 0, ataddr = 0;
            int ntbyte = 0, atbyte = 0;
            int lobyte = 0, hibyte = 0;

            for (int i = 0; i < 3; i++)
            {
                if (ppu_scanline > -1 && ppu_scanline < 240)
                {
                    switch (cycle % 8)
                    {
                        case 1:
                        case 2:
                            ntaddr = 0x2000 | (loopyV & 0x0fff);
                            break;
                        case 3:
                        case 4:
                            ataddr = 0x23C0 | (loopyV & 0x0C00) | ((loopyV >> 4) & 0x38) | ((loopyV >> 2) & 0x07);
                            break;
                        case 5:
                        case 6:

                            break;
                        case 7:
                        case 8:

                            break;
                    }

                    ntbyte = c.mapper.vram[ntaddr];
                    atbyte = c.mapper.vram[ataddr];


                }
                else if (ppu_scanline == 241)
                {
                    if (cycle == 0)
                    {
                        SetVBlank();
                        SetNMI();
                        ClearSpriteZero();
                    }
                }
                else if (ppu_scanline == 261)
                {
                    if (cycle == 0)
                    {
                        ClearVBlank();
                    }
                }
                else if (ppu_scanline == 262)
                {
                    ppu_cyc = 0;
                    ppu_scanline = -1;
                    ppunmi = false;
                    frame++;
                }

                cycle++;
                if (cycle > 340)
                {
                    cycle = 0;
                    ppu_scanline++;
                    if (ppu_scanline == 260)
                        ppu_scanline = 0;
                }
            }
        }

        private void RenderBackgroundNew2()
        {
            int patternaddr = (ppuctrl & 0x10) > 0 ? 0x1000 : 0x0000;
            int paladdr = 0x3f00;
            int left8 = (ppuctrl & 0x02) > 0 ? 1 : 0;

            int y = ppu_scanline / 8;

            for (int x = 0; x < 32; x++)
            {
                int ntaddr = 0x2000 | (loopyV & 0x0fff);
                int ataddr = 0x23C0 | (loopyV & 0x0C00) | ((loopyV >> 4) & 0x38) | ((loopyV >> 2) & 0x07);

                int ntbyte = c.mapper.vram[ntaddr];
                int atbyte = c.mapper.vram[ataddr];

                int row = (y + scroll_y) % 8;

                byte byte1 = c.mapper.vram[patternaddr + ntbyte * 16 + y + 0];
                byte byte2 = c.mapper.vram[patternaddr + ntbyte * 16 + y + 8];

                if ((ntaddr & 0x80) == 0x80)
                {
                    int up = 0;
                }

                int offx = x * 8 - scroll_x;
                int offy = y * 8 - scroll_y;

                int shift = 0x80;

                int bit2 = GetAttributeIndex(loopyV & 0x1f, (loopyV & 0x3e0) >> 5, atbyte);

                for (int col = 0; col < 8; col++)
                {
                    //int xx = 8 - col - scroll_x;
                    //int pos = ((y * 256) * 4) + ((x * 8) * 4) + col;

                    int bit0 = (byte1 & shift) > 0 ? 1 : 0;
                    int bit1 = (byte2 & shift) > 0 ? 1 : 0;
                    shift >>= 1;

                    int colorindex = bit2 * 4 + (bit0 | bit1 * 2);

                    int xp = offx + col;
                    int yp = offy + row;

                    if (xp < 0 || xp >= 256 || yp < 0 || yp >= 240)
                        continue;

                    int color = palettes[c.mapper.vram[paladdr | colorindex]];
                    gfxdata[(yp * 256 * 4) + (xp * 4) + 0] = (u8)(color >> 0);
                    gfxdata[(yp * 256 * 4) + (xp * 4) + 1] = (u8)(color >> 8);
                    gfxdata[(yp * 256 * 4) + (xp * 4) + 2] = (u8)(color >> 16);
                    gfxdata[(yp * 256 * 4) + (xp * 4) + 3] = 255;
                    //RenderFrame();
                }
                //RenderFrame();
                IncreaseV();
            }
            IncreaseY();
        }

        private void RenderBackgroundNewN()
        {
            int patternaddr = (ppuctrl & 0x10) > 0 ? 0x1000 : 0x0000;
            int paladdr = 0x3f00;
            int left8 = (ppuctrl & 0x02) > 0 ? 1 : 0;
            int y = (ppu_scanline / 8);// +scroll_y;

            int sx = scroll_x;// +(ppuctrl & 1 ? 256 : 0);
            int sy = scroll_y;
            int xMin = (sx / 8);// + left8;
            int xMax = (sx + 256) / 8;
            int yMin = (sy / 8);// + left8;
            int yMax = (sy + 240) / 8;

            for (int x = 0; x <= 32; x++)
            {
                int addr = 0;
                int natx = 0;

                int ntaddr = 0x2000 | (loopyV & 0x0fff);
                int ataddr = 0x23C0 | (loopyV & 0x0c00) | ((loopyV >> 4) & 0x38) | ((loopyV >> 2) & 0x07);

                if (ntaddr == 0x2084)
                {
                    int yu = 0;
                }

                int tileid = c.mapper.vram[ntaddr];
                int bit2 = c.mapper.vram[ntaddr];
                //int bit2 = GetAttributeIndex(addr & 0x1f, ((addr & 0x3e0) >> 5), c.mapper.vram[baseaddr + 0x3c0 + (y / 4) * 8 + ((x - natx) / 4)]);

                int offx = x * 8 - sx;
                int offy = y * 8 - sy;

                int row = ppu_scanline % 8;

                byte byte1 = c.mapper.vram[patternaddr + tileid * 16 + row + 0];
                byte byte2 = c.mapper.vram[patternaddr + tileid * 16 + row + 8];

                //byte1 = c.mapper.vram[patternaddr + tileid * 16 + row + 0];
                //byte2 = c.mapper.vram[patternaddr + tileid * 16 + row + 8];

                int shift = 0x80;

                for (int col = 0; col < 8; col++)
                {
                    int bit0 = (byte1 & shift) > 0 ? 1 : 0;
                    int bit1 = (byte2 & shift) > 0 ? 1 : 0;

                    shift >>= 1;

                    int colorindex = bit2 * 4 + (bit0 | bit1 * 2);

                    int xp = offx + col;
                    int yp = offy + row;

                    if (xp < 0 || xp >= 256 || yp < 0 || yp >= 240)
                        continue;

                    int color = palettes[c.mapper.vram[paladdr | colorindex]];
                    gfxdata[(yp * 256 * 4) + (xp * 4) + 0] = (u8)(color >> 0);
                    gfxdata[(yp * 256 * 4) + (xp * 4) + 1] = (u8)(color >> 8);
                    gfxdata[(yp * 256 * 4) + (xp * 4) + 2] = (u8)(color >> 16);
                    gfxdata[(yp * 256 * 4) + (xp * 4) + 3] = 255;
                    //DrawFrame();
                }
                //DrawFrame();
                IncreaseV();
            }
        }

        private void RenderBackgroundNew()
        {
            int patternaddr = (ppuctrl & 0x10) > 0 ? 0x1000 : 0x0000;
            int paladdr = 0x3f00;
            int left8 = (ppuctrl & 0x02) > 0 ? 1 : 0;
            int y = (ppu_scanline / 8);// +scroll_y;

            int sx = scroll_x;// +(ppuctrl & 1 ? 256 : 0);
            int sy = scroll_y;
            int xMin = (sx / 8);// + left8;
            int xMax = (sx + 256) / 8;
            int yMin = (sy / 8);// + left8;
            int yMax = (sy + 240) / 8;

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
                }

                if ((ppuctrl & 3) > 0)
                    addr ^= 0x400;

                if (addr == 0x224f)
                {
                    int yu = 0;
                }

                if (sy > 0xef)
                    sy = 0;

                int offx = x * 8 - sx;
                int offy = y * 8 - sy;

                int baseaddr = addr & 0x2c00;

                int tileid = c.mapper.vram[addr];

                int bit2 = GetAttributeIndex(addr & 0x1f, ((addr & 0x3e0) >> 5), c.mapper.vram[baseaddr + 0x3c0 + (y / 4) * 8 + ((x - natx) / 4)]);

                int row = ppu_scanline % 8;

                byte byte1 = c.mapper.vram[patternaddr + tileid * 16 + row + 0];
                byte byte2 = c.mapper.vram[patternaddr + tileid * 16 + row + 8];

                int shift = 0x80;

                for (int col = 0; col < 8; col++)
                {
                    int bit0 = (byte1 & shift) > 0 ? 1 : 0;
                    int bit1 = (byte2 & shift) > 0 ? 1 : 0;

                    shift >>= 1;

                    int palindex = bit0 | bit1 * 2;

                    int colorindex = bit2 * 4 + palindex;

                    int xp = offx + col;
                    int yp = offy + row;

                    if (xp < 0 || xp >= 256 || yp < 0 || yp >= 240)
                        continue;

                    int color = palettes[c.mapper.vram[paladdr | colorindex]];
                    gfxdata[(yp * 256 * 4) + (xp * 4) + 0] = (u8)(color >> 0);
                    gfxdata[(yp * 256 * 4) + (xp * 4) + 1] = (u8)(color >> 8);
                    gfxdata[(yp * 256 * 4) + (xp * 4) + 2] = (u8)(color >> 16);
                    gfxdata[(yp * 256 * 4) + (xp * 4) + 3] = 255;

                    sp0data[(yp * 256 * 4) + (xp * 4) + 0] = (u8)palindex;

                    //DrawFrame();
                }
                //DrawFrame();
            }
        }

        private void RenderSprites(u8 frontback)
        {
            int oamaddr = 0x0100 * ppuoamdma;
            int nametableaddr = 0x2000 | ppuctrl & 3 * 0x400;
            int patternaddr = (ppuctrl & 0x08) > 0 ? 0x1000 : 0x0000;
            int paladdr = 0x3f10;
            int left8 = (ppuctrl & 0x04) > 0 ? 1 : 0;

            u8 x, y;
            int tileid, att, i;

            for (int j = 64; j > 0; j--)
            {
                i = j % 64;

                y = (u8)(c.mapper.oam[i * 4 + 0] + 1);
                tileid = c.mapper.oam[i * 4 + 1];
                att = c.mapper.oam[i * 4 + 2];
                x = c.mapper.oam[i * 4 + 3];// & 0xff + left8;

                int size = 8;
                if (spritesize)
                {
                    size = 16;
                }

                bool flipH = (att & 0x40) > 0;
                bool flipV = (att & 0x80) > 0;

                int byte1 = 0;
                int byte2 = 0;

                if (size == 16)
                {
                    if (tileid == 1)
                    {
                        if (y < 0xf1)
                        {
                            int yu = 0;
                        }
                    }

                    if ((tileid & 1) == 0)
                        patternaddr = 0x0000;
                    else
                        patternaddr = 0x1000;

                    tileid &= 0xfe;

                    for (int r = 0; r < 16; r++)
                    {
                        int rr = r % 8;

                        if (tileid == 0)
                        {
                            if (y < 0xf1)
                            {
                                int yu = 0;
                            }
                        }

                        if (r < 8)
                        {
                            byte1 = c.mapper.vram[patternaddr + tileid * 16 + rr + 0];
                            byte2 = c.mapper.vram[patternaddr + tileid * 16 + rr + 8];
                        }
                        else
                        {
                            byte1 = c.mapper.vram[patternaddr + (tileid + 1) * 16 + rr + 0];
                            byte2 = c.mapper.vram[patternaddr + (tileid + 1) * 16 + rr + 8];
                        }

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
                            if (x < 0 || x >= 255 || y < 0 || y >= 240)
                                break;

                            if (palindex != 0)
                            {
                                byte bgpalindex = sp0data[256 * (y + row) * 4 + (x + col) * 4 + 0];
                                if (bgpalindex != 0 && i == 0 && yp == ppu_scanline & x < 255)
                                    SetSpriteZero();

                                if ((att & frontback) == 0 || bgpalindex == 0)
                                {
                                    int color = palettes[c.mapper.vram[paladdr | colorindex]];
                                    gfxdata[256 * (y + row) * 4 + (x + col) * 4 + 0] = (u8)(color >> 0);
                                    gfxdata[256 * (y + row) * 4 + (x + col) * 4 + 1] = (u8)(color >> 8);
                                    gfxdata[256 * (y + row) * 4 + (x + col) * 4 + 2] = (u8)(color >> 16);
                                    gfxdata[256 * (y + row) * 4 + (x + col) * 4 + 3] = 255;
                                }
                            }
                            //DrawFrame();
                        }
                    }
                }
                else
                {
                    for (int r = 0; r < 8; r++)
                    {
                        byte1 = c.mapper.vram[patternaddr + tileid * 16 + r + 0];
                        byte2 = c.mapper.vram[patternaddr + tileid * 16 + r + 8];

                        //u8 byte1 = c.mapper.PpuRead(patternaddr + tileid * 16 + r + 0);
                        //u8 byte2 = c.mapper.PpuRead(patternaddr + tileid * 16 + r + 8);

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
                                break;

                            if (palindex != 0)
                            {
                                byte bgpalindex = sp0data[256 * (y + row) * 4 + (x + col) * 4 + 0];
                                if (bgpalindex != 0 && i == 0 && yp == ppu_scanline & x < 255)
                                    SetSpriteZero();

                                if ((att & frontback) == 0 || bgpalindex == 0)
                                {
                                    int color = palettes[c.mapper.vram[paladdr | colorindex]];
                                    gfxdata[256 * (y + row) * 4 + (x + col) * 4 + 0] = (u8)(color >> 0);
                                    gfxdata[256 * (y + row) * 4 + (x + col) * 4 + 1] = (u8)(color >> 8);
                                    gfxdata[256 * (y + row) * 4 + (x + col) * 4 + 2] = (u8)(color >> 16);
                                    gfxdata[256 * (y + row) * 4 + (x + col) * 4 + 3] = 255;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void SetSpriteZero()
        {
            ppustatus |= 0x40;
            c.mapper.ram[0x2002] = ppustatus;
            sprite0 = true;
        }

        private void SetVBlank()
        {
            if (c.cpu.totalcycles <= 59560)
                return;

            ppustatus |= 0x80;
            c.mapper.ram[0x2002] = ppustatus;
            vblank = true;
        }

        private void ClearSpriteZero()
        {
            ppustatus &= 0xbf;
            sprite0 = false;
        }

        private void ClearVBlank()
        {
            ppustatus &= 0x7f;
            vblank = false;
        }

        private void IncreaseV()
        {
            if ((loopyV & 0x1f) == 0x1f)
            {
                loopyV &= 0xffe0;
                loopyV ^= 0x400;
            }
            else
                loopyV++;
        }

        private void IncreaseY()
        {
            if ((loopyV & 0x7000) == 0x7000)
            {
                loopyV &= ~0x7000;

                if ((loopyV & 0x3e0) == 0x3a0)
                {
                    loopyV ^= 0x0800;
                    loopyV &= ~0x03E0;
                }
                else
                {
                    if ((loopyV & 0x3e0) == 0x3e0)
                        loopyV &= ~0x03E0;
                    else
                        loopyV += 0x20;
                }
            }
            else
                loopyV += 0x1000;
        }

        private void RenderFrame()
        {
            bool wopen = false;
            emutex.Update(gfxdata);
            emusprite.Texture = emutex;

            uint id = emusprite.Texture.NativeHandle;

            if (ImGui.Begin("Nes Sharp", ref wopen))
            {
                ImGui.SetWindowPos(new ImVec2(0, 25));
                ImGui.SetWindowSize(new ImVec2(512, 480));
                ImGui.Image((IntPtr)id, new ImVec2(512, 464));
            }

            GuiImpl.Render(window);
            window.Display();
        }
    }
}
