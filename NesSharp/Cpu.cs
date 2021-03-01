using System;
using System.Text;

namespace NesSharp
{
    public class Cpu
    {
        Core c;

        private byte a;
        private byte y;
        private byte x;
        private byte ps;
        private byte sp;
        private int pc;

        private bool fc;
        private bool fz;
        private bool fi;
        private bool fd;
        private bool fb;
        private bool fu;
        private bool fv;
        private bool fn;

        private bool trace;

        private byte op;

        private bool pagecrossed;

        public bool running;

        public string opname;

        public int cycles;
        public int ppucycles;
        public int totalcycles;

        int[] cyclestable =
        {
            7,6,0,0,0,3,5,0,3,2,2,0,0,4,6,0,//00
            2,5,0,0,0,4,6,0,2,4,0,0,0,4,7,0,//10
            6,6,0,0,3,3,5,0,4,2,2,0,4,4,6,6,//20
            2,5,0,0,0,4,6,0,2,4,0,0,0,4,7,0,//30
            6,6,0,0,0,3,5,0,3,2,2,0,3,4,6,0,//40
            2,5,0,0,0,4,6,0,2,4,0,0,0,4,7,0,//50
            6,6,0,0,0,3,5,0,4,2,2,0,5,4,6,0,//60
            2,5,0,0,0,4,6,0,2,4,0,0,0,4,7,0,//70
            0,6,0,0,3,3,3,0,2,0,2,0,4,4,4,0,//80
            2,6,0,0,4,4,4,0,2,5,2,0,0,5,0,0,//90
            2,6,2,0,3,3,3,0,2,2,2,0,4,4,4,0,//A0
            2,5,0,0,4,4,4,0,2,4,2,0,4,4,4,0,//B0
            2,6,0,0,3,3,5,0,2,2,2,0,4,4,6,0,//C0
            2,5,0,0,0,4,6,0,2,4,0,0,0,4,7,0,//D0
            2,6,0,0,3,3,5,0,2,2,2,0,4,4,6,0,//E0
            2,5,0,0,0,4,6,0,2,4,0,0,0,4,7,0 //F0
        };

        public Cpu(Core core, string gamename)
        {
            c = core;
            c.mapper = new Mapper(c, gamename);
            pc = (c.mapper.CpuRead(0xfffd) << 8) | c.mapper.CpuRead(0xfffc);

            if (gamename == "Content/instr/nestest.nes")
            {
                //pc = 0xc000;
            }

            sp = 0xfd;
            ps = 0x24;
            fi = true;
            cycles = 0;
            totalcycles = 0;
            running = true;
            //ppucycles = cycles * 3;
            //trace = true;
        }

        public void Run()
        {
            while (true)
            {
                if (!running)
                {

                }
            }
        }

        public void Execute()
        {
            while (ppucycles < 341)
            {
                SetProcessorStatus();

                op = c.mapper.CpuRead(pc++);

                if (trace)
                {
                    c.tracer.DisassembleFCEUXFormat(op, pc - 1, a, x, y, ps, sp, totalcycles);
                }

                switch (op)
                {
                    //Implied
                    case 0x2a: { ROL(); } break;
                    case 0x0a: { ASL(); } break;
                    case 0x6a: { ROR(); } break;
                    case 0x4a: { LSR(); } break;

                    case 0x8a: { TXA(); } break;
                    case 0x98: { TYA(); } break;
                    case 0xaa: { TAX(); } break;
                    case 0xa8: { TAY(); } break;

                    case 0xe8: { INX(); } break;
                    case 0xc8: { INY(); } break;
                    case 0xca: { DEX(); } break;
                    case 0x88: { DEY(); } break;

                    case 0x38: { fc = true; } break;
                    case 0x18: { fc = false; } break;
                    case 0xf8: { fd = true; } break;
                    case 0xd8: { fd = false; } break;
                    case 0x78: { fi = true; } break;
                    case 0x58: { fi = false; } break;
                    case 0xb8: { fv = false; } break;

                    case 0xea: { } break;

                    //Immediate
                    case 0xa9: { LDA(); pc++; } break;
                    case 0xa2: { LDX(); pc++; } break;
                    case 0xa0: { LDY(); pc++; } break;

                    case 0x69: { ADC(); pc++; } break;
                    case 0xe9: { SBC(); pc++; } break;
                    case 0x09: { ORA(); pc++; } break;
                    case 0x29: { AND(); pc++; } break;
                    case 0x49: { EOR(); pc++; } break;

                    case 0xc9: { CMP(); pc++; } break;
                    case 0xe0: { CPX(); pc++; } break;
                    case 0xc0: { CPY(); pc++; } break;

                    //Zero Page
                    case 0xa5: { LDAM(GetZERP()); pc++; } break;
                    case 0xa6: { LDXM(GetZERP()); pc++; } break;
                    case 0xa4: { LDYM(GetZERP()); pc++; } break;

                    case 0x85: { STA(GetZERP()); pc++; } break;
                    case 0x86: { STX(GetZERP()); pc++; } break;
                    case 0x84: { STY(GetZERP()); pc++; } break;

                    case 0xe6: { INC(GetZERP()); pc++; } break;
                    case 0xc6: { DEC(GetZERP()); pc++; } break;
                    case 0x06: { ASLM(GetZERP()); pc++; } break;
                    case 0x46: { LSRM(GetZERP()); pc++; } break;
                    case 0x26: { ROLM(GetZERP()); pc++; } break;
                    case 0x66: { RORM(GetZERP()); pc++; } break;

                    case 0x65: { ADCM(GetZERP()); pc++; } break;
                    case 0xe5: { SBCM(GetZERP()); pc++; } break;
                    case 0x05: { ORAM(GetZERP()); pc++; } break;
                    case 0x25: { ANDM(GetZERP()); pc++; } break;
                    case 0x45: { EORM(GetZERP()); pc++; } break;

                    case 0x24: { BIT(GetZERP()); pc++; } break;
                    case 0xc5: { CMPM(GetZERP()); pc++; } break;
                    case 0xe4: { CPXM(GetZERP()); pc++; } break;
                    case 0xc4: { CPYM(GetZERP()); pc++; } break;

                    //Zero Page XY
                    case 0xb5: { LDAM(GetZERX()); pc++; } break;
                    case 0xb4: { LDYM(GetZERX()); pc++; } break;
                    case 0xb6: { LDXM(GetZERY()); pc++; } break;

                    case 0x95: { STA(GetZERX()); pc++; } break;
                    case 0x94: { STY(GetZERX()); pc++; } break;
                    case 0x96: { STX(GetZERY()); pc++; } break;

                    case 0xf6: { INC(GetZERX()); pc++; } break;
                    case 0xd6: { DEC(GetZERX()); pc++; } break;
                    case 0x16: { ASLM(GetZERX()); pc++; } break;
                    case 0x56: { LSRM(GetZERX()); pc++; } break;
                    case 0x36: { ROLM(GetZERX()); pc++; } break;
                    case 0x76: { RORM(GetZERX()); pc++; } break;

                    case 0x75: { ADCM(GetZERX()); pc++; } break;
                    case 0xf5: { SBCM(GetZERX()); pc++; } break;
                    case 0x15: { ORAM(GetZERX()); pc++; } break;
                    case 0x35: { ANDM(GetZERX()); pc++; } break;
                    case 0x55: { EORM(GetZERX()); pc++; } break;

                    case 0xd5: { CMPM(GetZERX()); pc++; } break;

                    //Absolute
                    case 0xad: { LDAM(GetABSO()); pc += 2; } break;
                    case 0xae: { LDXM(GetABSO()); pc += 2; } break;
                    case 0xac: { LDYM(GetABSO()); pc += 2; } break;

                    case 0x8d: { STA(GetABSO()); pc += 2; } break;
                    case 0x8e: { STX(GetABSO()); pc += 2; } break;
                    case 0x8c: { STY(GetABSO()); pc += 2; } break;

                    case 0xee: { INC(GetABSO()); pc += 2; } break;
                    case 0xce: { DEC(GetABSO()); pc += 2; } break;
                    case 0x0e: { ASLM(GetABSO()); pc += 2; } break;
                    case 0x4e: { LSRM(GetABSO()); pc += 2; } break;
                    case 0x2e: { ROLM(GetABSO()); pc += 2; } break;
                    case 0x6e: { RORM(GetABSO()); pc += 2; } break;

                    case 0x6d: { ADCM(GetABSO()); pc += 2; } break;
                    case 0xed: { SBCM(GetABSO()); pc += 2; } break;
                    case 0x0d: { ORAM(GetABSO()); pc += 2; } break;
                    case 0x2d: { ANDM(GetABSO()); pc += 2; } break;
                    case 0x4d: { EORM(GetABSO()); pc += 2; } break;

                    case 0xcd: { CMPM(GetABSO()); pc += 2; } break;
                    case 0x2c: { BIT(GetABSO()); pc += 2; } break;
                    case 0xec: { CPXM(GetABSO()); pc += 2; } break;
                    case 0xcc: { CPYM(GetABSO()); pc += 2; } break;

                    //Absolute XY
                    case 0xbd: { LDAM(GetABSX()); pc += 2; } break;
                    case 0xb9: { LDAM(GetABSY()); pc += 2; } break;
                    case 0xbc: { LDYM(GetABSX()); pc += 2; } break;
                    case 0xbe: { LDXM(GetABSY()); pc += 2; } break;

                    case 0x9d: { STA(GetABSX()); pc += 2; } break;
                    case 0x99: { STA(GetABSY()); pc += 2; } break;

                    case 0xfe: { INC(GetABSX()); pc += 2; } break;
                    case 0xde: { DEC(GetABSX()); pc += 2; } break;
                    case 0x1e: { ASLM(GetABSX()); pc += 2; } break;
                    case 0x5e: { LSRM(GetABSX()); pc += 2; } break;
                    case 0x3e: { ROLM(GetABSX()); pc += 2; } break;
                    case 0x7e: { RORM(GetABSX()); pc += 2; } break;

                    case 0x7d: { ADCM(GetABSX()); pc += 2; } break;
                    case 0x79: { ADCM(GetABSY()); pc += 2; } break;
                    case 0xfd: { SBCM(GetABSX()); pc += 2; } break;
                    case 0xf9: { SBCM(GetABSY()); pc += 2; } break;
                    case 0x1d: { ORAM(GetABSX()); pc += 2; } break;
                    case 0x19: { ORAM(GetABSY()); pc += 2; } break;
                    case 0x3d: { ANDM(GetABSX()); pc += 2; } break;
                    case 0x39: { ANDM(GetABSY()); pc += 2; } break;
                    case 0x5d: { EORM(GetABSX()); pc += 2; } break;
                    case 0x59: { EORM(GetABSY()); pc += 2; } break;

                    case 0xdd: { CMPM(GetABSX()); pc += 2; } break;
                    case 0xd9: { CMPM(GetABSY()); pc += 2; } break;

                    //Indirect X
                    case 0xa1: { LDAM(GetINDX()); pc++; } break;

                    case 0x81: { STA(GetINDX()); pc++; } break;

                    case 0xc1: { CMPM(GetINDX()); pc++; } break;

                    case 0x61: { ADCM(GetINDX()); pc++; } break;
                    case 0xe1: { SBCM(GetINDX()); pc++; } break;
                    case 0x01: { ORAM(GetINDX()); pc++; } break;
                    case 0x21: { ANDM(GetINDX()); pc++; } break;
                    case 0x41: { EORM(GetINDX()); pc++; } break;

                    //Indirect Y
                    case 0xb1: { LDAM(GetINDY()); pc++; } break;

                    case 0x91: { STA(GetINDY()); pc++; } break;

                    case 0xd1: { CMPM(GetINDY()); pc++; } break;

                    case 0x11: { ORAM(GetINDY()); pc++; } break;
                    case 0xf1: { SBCM(GetINDY()); pc++; } break;
                    case 0x71: { ADCM(GetINDY()); pc++; } break;
                    case 0x31: { ANDM(GetINDY()); pc++; } break;
                    case 0x51: { EORM(GetINDY()); pc++; } break;

                    //Branches
                    case 0x90: { BCC(GetRELA()); } break;
                    case 0x50: { BVC(GetRELA()); } break;
                    case 0xd0: { BNE(GetRELA()); } break;
                    case 0x30: { BMI(GetRELA()); } break;
                    case 0x10: { BPL(GetRELA()); } break;
                    case 0xf0: { BEQ(GetRELA()); } break;
                    case 0xb0: { BCS(GetRELA()); } break;
                    case 0x70: { BVS(GetRELA()); } break;

                    //Stack
                    case 0x48: { PHA(); } break;
                    case 0x08: { PHP(); } break;

                    case 0x68: { PLA(); } break;
                    case 0x28: { PLP(); } break;

                    case 0x9a: { TXS(); } break;
                    case 0xba: { TSX(); } break;

                    //Jumps
                    case 0x4c: { JMP(GetABSO()); } break;
                    case 0x6c: { JMP(GetINDI()); } break;
                    case 0x20: { JSR(GetABSO()); } break;

                    //Returns
                    case 0x60: { RTS(); } break;
                    case 0x40: { RTI(); } break;
                    case 0x00: { BRK(); } break;
                    default:
                        break;
                }

                if (pagecrossed)
                {
                    cycles++;
                    totalcycles++;
                    ppucycles += 3;
                    pagecrossed = false;
                }

                cycles += cyclestable[op];
                totalcycles += cyclestable[op];
                ppucycles += cyclestable[op] * 3;

                if (c.ppu.ppunmi)
                {
                    NMI();
                }
            }

            c.ppu.RenderScanline();
            ppucycles -= 341;
        }

        private void BRK()
        {
            running = false;
            return;
            throw new NotImplementedException();
        }

        public void NMI()
        {
            //pc++;
            c.mapper.CpuWrite(sp | 0x100, (byte)(pc >> 8));
            sp--;
            c.mapper.CpuWrite(sp | 0x100, (byte)pc);
            sp--;
            c.mapper.CpuWrite(sp | 0x100, ps);
            sp--;
            pc = (c.mapper.CpuRead(0xfffb) << 8) | c.mapper.CpuRead(0xfffa);
            c.ppu.ppunmi = false;
            cycles += 7;
            totalcycles += 7;
            ppucycles += 7 * 3;
        }

        private void RTI()
        {
            PLP();
            sp++;
            pc = c.mapper.CpuRead(sp | 0x100);
            sp++;
            pc |= c.mapper.CpuRead(sp | 0x100) << 8;
        }

        private void RTS()
        {
            sp++;
            pc = c.mapper.CpuRead(sp | 0x100);
            sp++;
            pc |= c.mapper.CpuRead(sp | 0x100) << 8;
            pc++;
        }

        private void JSR(int v)
        {
            pc++;
            c.mapper.CpuWrite(sp | 0x100, (byte)(pc >> 8));
            sp--;
            c.mapper.CpuWrite(sp | 0x100, (byte)pc);
            sp--;
            pc = v;
        }

        private void JMP(int v)
        {
            pc = v;
        }

        private void TSX()
        {
            x = sp;
            SetZero(x);
            SetNegative(x);
        }

        private void TXS()
        {
            sp = x;
        }

        private void PLP()
        {
            sp++;
            ps = c.mapper.CpuRead(sp | 0x100);
            UpdateFlags();
            //ps = (byte)(ps & 0xef);
        }

        private void PLA()
        {
            sp++;
            a = c.mapper.CpuRead(sp | 0x100);
            SetZero(a);
            SetNegative(a);
        }

        private void PHP()
        {
            c.mapper.CpuWrite(sp | 0x100, (byte)(ps | 0x30));
            sp--;
            UpdateFlags();
        }

        private void PHA()
        {
            c.mapper.CpuWrite(sp | 0x100, a);
            sp--;
        }

        private void BVS(int v)
        {
            if (fv)
            {
                pc = v;
                cycles++;
                totalcycles++;
                ppucycles += 3;
            }
            else
            {
                pc++;
            }
        }

        private void BCS(int v)
        {
            if (fc)
            {
                pc = v;
                cycles++;
                totalcycles++;
                ppucycles += 3;
            }
            else
            {
                pc++;
            }
        }

        private void BEQ(int v)
        {
            if (fz)
            {
                pc = v;
                cycles++;
                totalcycles++;
                ppucycles += 3;
            }
            else
            {
                pc++;
            }
        }

        private void BPL(int v)
        {
            if (!fn)
            {
                pc = v;
                cycles++;
                totalcycles++;
                ppucycles += 3;
            }
            else
            {
                pc++;
            }
        }

        private void BMI(int v)
        {
            if (fn)
            {
                pc = v;
                cycles++;
                totalcycles++;
                ppucycles += 3;
            }
            else
            {
                pc++;
            }
        }

        private void BNE(int v)
        {
            if (!fz)
            {
                pc = v;
                cycles++;
                totalcycles++;
                ppucycles += 3;
            }
            else
            {
                pc++;
            }
        }

        private void BVC(int v)
        {
            if (!fv)
            {
                pc = v;
                cycles++;
                totalcycles++;
                ppucycles += 3;
            }
            else
            {
                pc++;
            }
        }

        private void BCC(int v)
        {
            if (!fc)
            {
                pc = v;
                cycles++;
                totalcycles++;
                ppucycles += 3;
            }
            else
            {
                pc++;
            }
        }

        private void BIT(int v)
        {
            int b = c.mapper.CpuRead(v);
            int t = (byte)(a & b);
            SetZero(t);
            SetNegative(b);
            SetOverflow(b);
        }

        private void AND()
        {
            a &= c.mapper.CpuRead(pc);
            SetZero(a);
            SetNegative(a);
        }

        private void ANDM(int v)
        {
            a &= c.mapper.CpuRead(v);
            SetZero(a);
            SetNegative(a);
        }

        private void DEC(int v)
        {
            int b = c.mapper.CpuRead(v) - 1;
            c.mapper.CpuWrite(v, (byte)b);
            SetZero(b);
            SetNegative(b);
        }

        private void INC(int v)
        {
            int b = c.mapper.CpuRead(v) + 1;
            c.mapper.CpuWrite(v, (byte)b);
            SetZero(b);
            SetNegative(b);
        }

        private void STY(int v1)
        {
            pagecrossed = false;
            c.mapper.CpuWrite(v1, y);
        }

        private void STX(int v1)
        {
            pagecrossed = false;
            c.mapper.CpuWrite(v1, x);
        }

        private void STA(int v1)
        {
            pagecrossed = false;
            c.mapper.CpuWrite(v1, a);
        }

        private void LDY()
        {
            y = c.mapper.CpuRead(pc);
            SetZero(y);
            SetNegative(y);
        }

        private void LDYM(int v)
        {
            y = c.mapper.CpuRead(v);
            SetZero(y);
            SetNegative(y);
        }

        private void LDX()
        {
            x = c.mapper.CpuRead(pc);
            SetZero(x);
            SetNegative(x);
        }

        private void LDXM(int v)
        {
            x = c.mapper.CpuRead(v);
            SetZero(x);
            SetNegative(x);
        }

        private void CPY()
        {
            int v = c.mapper.CpuRead(pc);
            int t = y - v;
            fc = y >= v;
            fz = y == v;
            SetNegative(t);
        }

        private void CPYM(int v)
        {
            v = c.mapper.CpuRead(v);
            int t = y - v;
            fc = y >= v;
            fz = y == v;
            SetNegative(t);
        }

        private void CPX()
        {
            int v = c.mapper.CpuRead(pc);
            int t = x - v;
            fc = x >= v;
            fz = x == v;
            SetNegative(t);
        }

        private void CPXM(int v)
        {
            v = c.mapper.CpuRead(v);
            int t = x - v;
            fc = x >= v;
            fz = x == v;
            SetNegative(t);
        }

        private void CMP()
        {
            int v = c.mapper.CpuRead(pc);
            int t = a - v;
            fc = a >= v;
            fz = a == v;
            SetNegative(t);
        }

        private void CMPM(int v)
        {
            v = c.mapper.CpuRead(v);
            int t = a - v;
            fc = a >= v;
            fz = a == v;
            SetNegative(t);
        }

        private void EOR()
        {
            a = (byte)(a ^ c.mapper.CpuRead(pc));
            SetZero(a);
            SetNegative(a);
        }

        private void EORM(int v)
        {
            a = (byte)(a ^ c.mapper.CpuRead(v));
            SetZero(a);
            SetNegative(a);
        }

        private void ORA()
        {
            a = (byte)(a | c.mapper.CpuRead(pc));
            SetZero(a);
            SetNegative(a);
        }

        private void ORAM(int v)
        {
            a = (byte)(a | c.mapper.CpuRead(v));
            SetZero(a);
            SetNegative(a);
        }

        private void SBC()
        {
            int v = c.mapper.CpuRead(pc);
            int r = a + ~v + (fc ? 1 : 0);
            SetZero(r);
            SetNegative(r);
            fv = ((a ^ v) & (a ^ r) & 0x80) > 0;
            fc = (r & 0xff00) == 0;
            a = (byte)r;
        }

        private void SBCM(int v)
        {
            v = c.mapper.CpuRead(v);
            int r = a + ~v + (fc ? 1 : 0);
            SetZero(r);
            SetNegative(r);
            fv = ((a ^ v) & (a ^ r) & 0x80) > 0;
            fc = (r & 0xff00) == 0;
            a = (byte)r;
        }

        private void ADC()
        {
            int v = c.mapper.CpuRead(pc);
            int r = a + v + (fc ? 1 : 0);
            SetZero(r);
            SetNegative(r);
            fv = (~(a ^ v) & (a ^ r) & 0x80) > 0;
            fc = r > 255;
            a = (byte)r;
        }

        private void ADCM(int v)
        {
            v = c.mapper.CpuRead(v);
            int r = a + v + (fc ? 1 : 0);
            SetZero(r);
            SetNegative(r);
            fv = (~(a ^ v) & (a ^ r) & 0x80) > 0;
            fc = r > 255;
            a = (byte)r;
        }

        private void LDA()
        {
            a = c.mapper.CpuRead(pc);
            SetZero(a);
            SetNegative(a);
        }

        private void LDAM(int v)
        {
            a = c.mapper.CpuRead(v);
            SetZero(a);
            SetNegative(a);
        }

        private void UpdateFlags()
        {
            fc = (ps & 0x01) > 0;
            fz = (ps & 0x02) > 0;
            fi = (ps & 0x04) > 0;
            fd = (ps & 0x08) > 0;
            //fb = (ps & 0x10) > 0 ? true : false;
            fv = (ps & 0x40) > 0;
            fn = (ps & 0x80) > 0;
        }

        private void SetProcessorStatus()
        {
            int t;
            t = fc ? 0x01 : 0x00;
            t |= fz ? 0x02 : 0x00;
            t |= fi ? 0x04 : 0x00;
            t |= fd ? 0x08 : 0x00;
            t |= fb ? 0x10 : 0x00;
            t |= fv ? 0x40 : 0x00;
            t |= fn ? 0x80 : 0x00;

            ps = (byte)(t | 0x20);
        }

        private void DEY()
        {
            y--;
            SetZero(y);
            SetNegative(y);
        }

        private void DEX()
        {
            x--;
            SetZero(x);
            SetNegative(x);
        }

        private void INY()
        {
            y++;
            SetZero(y);
            SetNegative(y);
        }

        private void INX()
        {
            x++;
            SetZero(x);
            SetNegative(x);
        }

        private void TAY()
        {
            y = a;
            SetZero(y);
            SetNegative(y);
        }

        private void TAX()
        {
            x = a;
            SetZero(x);
            SetNegative(x);
        }

        private void TYA()
        {
            a = y;
            SetZero(a);
            SetNegative(a);
        }

        private void TXA()
        {
            a = x;
            SetZero(a);
            SetNegative(a);
        }

        private void ROL()
        {
            bool bit7 = (a & (1 << 7)) > 0;
            a <<= 1 & 0xff;
            if (fc)
                a |= (1 << 0);
            SetZero(a);
            SetNegative(a);
            fc = bit7;
        }

        private void ROLM(int v)
        {
            int r = c.mapper.CpuRead(v);
            bool bit7 = (r & (1 << 7)) > 0;
            r <<= 1;
            if (fc)
                r |= (1 << 0);
            fc = bit7;
            c.mapper.CpuWrite(v, (byte)r);
            SetZero(r);
            SetNegative(r);
        }

        private void ASL()
        {
            fc = (a & (1 << 7)) > 0;
            a = (byte)((a << 1) & 0xfe);
            SetZero(a);
            SetNegative(a);
        }

        private void ASLM(int v)
        {
            int r = c.mapper.CpuRead(v);
            fc = (r & (1 << 7)) > 0;
            r = (r << 1) & 0xfe;
            c.mapper.CpuWrite(v, (byte)r);
            SetZero(r);
            SetNegative(r);
        }

        private void ROR()
        {
            bool bit0 = (a & (1 << 0)) > 0;
            a >>= 1 & 0xff;
            if (fc)
                a |= (1 << 7);
            SetZero(a);
            SetNegative(a);
            fc = bit0;
        }

        private void RORM(int v)
        {
            int r = c.mapper.CpuRead(v);
            bool bit0 = (r & (1 << 0)) > 0;
            r >>= 1;
            if (fc)
                r |= (1 << 7);
            fc = bit0;
            c.mapper.CpuWrite(v, (byte)r);
            SetZero(r);
            SetNegative(r);
        }

        private void LSR()
        {
            fc = (a & (1 << 0)) > 0;
            a = (byte)((a >> 1) & 0x7f);
            SetZero(a);
            SetNegative(a);
        }

        private void LSRM(int v)
        {
            int r = c.mapper.CpuRead(v);
            fc = (a & (1 << 0)) > 0;
            r = (byte)((r >> 1) & 0x7f);
            c.mapper.CpuWrite(v, (byte)r);
            SetZero(r);
            SetNegative(r);
        }

        private void SetZero(int v)
        {
            fz = (byte)v == 0;
        }

        private void SetNegative(int v)
        {
            fn = ((byte)(v >> 7) & 1) > 0;
        }

        private void SetOverflow(int v)
        {
            fv = ((byte)(v >> 6) & 1) > 0;
        }

        private int GetZERP()
        {
            return c.mapper.CpuRead(pc);
        }

        private int GetZERX()
        {
            return (byte)(c.mapper.CpuRead(pc) + x);
        }

        private int GetZERY()
        {
            return (byte)(c.mapper.CpuRead(pc) + y);
        }

        private int GetABSO()
        {
            return c.mapper.CpuRead(pc + 1) << 8 | c.mapper.CpuRead(pc);
        }

        private int GetABSX()
        {
            int b1 = c.mapper.CpuRead(pc);
            int b2 = c.mapper.CpuRead(pc + 1);
            int b3 = (b1 | b2 << 8) + x;
            pagecrossed = (b3 & 0xff00) != b2 << 8;
            return b3 & 0xffff;
        }

        private int GetABSY()
        {
            int b1 = c.mapper.CpuRead(pc);
            int b2 = c.mapper.CpuRead(pc + 1);
            int b3 = (b1 | b2 << 8) + y;
            pagecrossed = (b3 & 0xff00) != b2 << 8;
            return b3 & 0xffff;
        }

        private int GetINDX()
        {
            int b1 = (byte)(c.mapper.CpuRead(pc) + x);
            return c.mapper.CpuRead(b1) | c.mapper.CpuRead((byte)(b1 + 1)) << 8;
        }

        private int GetINDY()
        {
            int b1 = c.mapper.CpuRead(pc);
            int b2 = c.mapper.CpuRead((byte)(b1 + 1));
            int b3 = (c.mapper.CpuRead(b1) | b2 << 8) + y;
            pagecrossed = (b3 & 0xff00) != b2 << 8;
            return b3 & 0xffff;
        }

        private int GetINDI()
        {
            int addr = c.mapper.CpuRead(pc) | c.mapper.CpuRead(pc + 1) << 8;

            if ((addr & 0xff) == 0xff)
            {
                return ++addr;
            }
            else
            {
                return c.mapper.CpuRead(addr + 1) << 8 | c.mapper.CpuRead(addr);
            }
        }

        private int GetRELA()
        {
            int b1 = c.mapper.CpuRead(pc);
            return pc + (sbyte)(b1) + 1; ;
        }

        public string GetFlags()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("P:");
            sb.Append(fn ? "N" : "n");
            sb.Append(fv ? "V" : "v");
            sb.Append(fu ? "U" : "u");
            sb.Append(fb ? "B" : "b");
            sb.Append(fd ? "D" : "d");
            sb.Append(fi ? "I" : "i");
            sb.Append(fz ? "Z" : "z");
            sb.Append(fc ? "C" : "c");
            sb.Append(" ");
            return sb.ToString();
        }
    }
}
