using System;
using System.Text;
using System.Linq;
using u8 = System.Byte;
using s8 = System.SByte;
using System.Collections.Generic;

namespace NesSharp
{
	public class Cpu
	{
		Core c;

		private u8 a;
		private u8 y;
		private u8 x;
		private u8 ps;
		private u8 sp;
		private int pc;
		private bool breakmode;

		private bool fc;
		private bool fz;
		private bool fi;
		private bool fd;
		private bool fb;
		private bool fu;
		private bool fv;
		private bool fn;

		private bool trace;

		private u8 op;

		private bool pagecrossed;

		public bool running;

		public string opname;

		public Dictionary<int, bool> breakpoints;

		public int cycles;
		public int ppucycles;
		public int totalcycles;
		private int framecycles;

		private const int SCANCYCLES = 341;

		public bool frameready;

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

		public u8 A { get => a; set => a = value; }
		public u8 Y { get => y; set => y = value; }
		public u8 X { get => x; set => x = value; }
		public u8 Ps { get => ps; set => ps = value; }
		public u8 Sp { get => sp; set => sp = value; }
		public int Pc { get => pc; set => pc = value; }
		public bool Breakmode { get => breakmode; set => breakmode = value; }

		public Cpu(Core core, string gamename)
		{
			c = core;
			c.mapper = new Mapper(c, gamename);
			if (c.mapper.ram == null)
				return;
			Pc = (c.mapper.CpuRead(0xfffd) << 8) | c.mapper.CpuRead(0xfffc);

			if (gamename == "Content/instr/nestest.nes")
			{
				//pc = 0xc000;
			}

			Sp = 0xfd;
			Ps = 0x24;
			fi = true;
			cycles = 0;
			totalcycles = 0;
			running = true;
			breakpoints = new Dictionary<int, bool>();
			for (int i = 0; i < 0x10000; i++)
				breakpoints[i] = false;

			//ppucycles = cycles * 3;
			//trace = true;
		}

		public void StepOne()
		{
			Execute();
		}

		public void Step()
		{
			while (ppucycles < SCANCYCLES)
			{
				bool bp = false;
				breakpoints.TryGetValue(pc, out bp);
				if (bp)
				{
					Breakmode = true;
					return;
				}

				Execute();
			}

			ppucycles -= SCANCYCLES;
		}

		private void Execute()
		{
			SetProcessorStatus();

			op = c.mapper.CpuRead(Pc++);

			if (trace)
			{
				int opsize;
				c.tracer.DisassembleFCEUXFormat(op, Pc - 1, A, X, Y, Ps, Sp, out opsize, totalcycles);
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
				case 0xa9: { LDA(); Pc++; } break;
				case 0xa2: { LDX(); Pc++; } break;
				case 0xa0: { LDY(); Pc++; } break;

				case 0x69: { ADC(); Pc++; } break;
				case 0xe9: { SBC(); Pc++; } break;
				case 0x09: { ORA(); Pc++; } break;
				case 0x29: { AND(); Pc++; } break;
				case 0x49: { EOR(); Pc++; } break;

				case 0xc9: { CMP(); Pc++; } break;
				case 0xe0: { CPX(); Pc++; } break;
				case 0xc0: { CPY(); Pc++; } break;

				//Zero Page
				case 0xa5: { LDAM(GetZERP()); Pc++; } break;
				case 0xa6: { LDXM(GetZERP()); Pc++; } break;
				case 0xa4: { LDYM(GetZERP()); Pc++; } break;

				case 0x85: { STA(GetZERP()); Pc++; } break;
				case 0x86: { STX(GetZERP()); Pc++; } break;
				case 0x84: { STY(GetZERP()); Pc++; } break;

				case 0xe6: { INC(GetZERP()); Pc++; } break;
				case 0xc6: { DEC(GetZERP()); Pc++; } break;
				case 0x06: { ASLM(GetZERP()); Pc++; } break;
				case 0x46: { LSRM(GetZERP()); Pc++; } break;
				case 0x26: { ROLM(GetZERP()); Pc++; } break;
				case 0x66: { RORM(GetZERP()); Pc++; } break;

				case 0x65: { ADCM(GetZERP()); Pc++; } break;
				case 0xe5: { SBCM(GetZERP()); Pc++; } break;
				case 0x05: { ORAM(GetZERP()); Pc++; } break;
				case 0x25: { ANDM(GetZERP()); Pc++; } break;
				case 0x45: { EORM(GetZERP()); Pc++; } break;

				case 0x24: { BIT(GetZERP()); Pc++; } break;
				case 0xc5: { CMPM(GetZERP()); Pc++; } break;
				case 0xe4: { CPXM(GetZERP()); Pc++; } break;
				case 0xc4: { CPYM(GetZERP()); Pc++; } break;

				//Zero Page XY
				case 0xb5: { LDAM(GetZERX()); Pc++; } break;
				case 0xb4: { LDYM(GetZERX()); Pc++; } break;
				case 0xb6: { LDXM(GetZERY()); Pc++; } break;

				case 0x95: { STA(GetZERX()); Pc++; } break;
				case 0x94: { STY(GetZERX()); Pc++; } break;
				case 0x96: { STX(GetZERY()); Pc++; } break;

				case 0xf6: { INC(GetZERX()); Pc++; } break;
				case 0xd6: { DEC(GetZERX()); Pc++; } break;
				case 0x16: { ASLM(GetZERX()); Pc++; } break;
				case 0x56: { LSRM(GetZERX()); Pc++; } break;
				case 0x36: { ROLM(GetZERX()); Pc++; } break;
				case 0x76: { RORM(GetZERX()); Pc++; } break;

				case 0x75: { ADCM(GetZERX()); Pc++; } break;
				case 0xf5: { SBCM(GetZERX()); Pc++; } break;
				case 0x15: { ORAM(GetZERX()); Pc++; } break;
				case 0x35: { ANDM(GetZERX()); Pc++; } break;
				case 0x55: { EORM(GetZERX()); Pc++; } break;

				case 0xd5: { CMPM(GetZERX()); Pc++; } break;

				//Absolute
				case 0xad: { LDAM(GetABSO()); Pc += 2; } break;
				case 0xae: { LDXM(GetABSO()); Pc += 2; } break;
				case 0xac: { LDYM(GetABSO()); Pc += 2; } break;

				case 0x8d: { STA(GetABSO()); Pc += 2; } break;
				case 0x8e: { STX(GetABSO()); Pc += 2; } break;
				case 0x8c: { STY(GetABSO()); Pc += 2; } break;

				case 0xee: { INC(GetABSO()); Pc += 2; } break;
				case 0xce: { DEC(GetABSO()); Pc += 2; } break;
				case 0x0e: { ASLM(GetABSO()); Pc += 2; } break;
				case 0x4e: { LSRM(GetABSO()); Pc += 2; } break;
				case 0x2e: { ROLM(GetABSO()); Pc += 2; } break;
				case 0x6e: { RORM(GetABSO()); Pc += 2; } break;

				case 0x6d: { ADCM(GetABSO()); Pc += 2; } break;
				case 0xed: { SBCM(GetABSO()); Pc += 2; } break;
				case 0x0d: { ORAM(GetABSO()); Pc += 2; } break;
				case 0x2d: { ANDM(GetABSO()); Pc += 2; } break;
				case 0x4d: { EORM(GetABSO()); Pc += 2; } break;

				case 0xcd: { CMPM(GetABSO()); Pc += 2; } break;
				case 0x2c: { BIT(GetABSO()); Pc += 2; } break;
				case 0xec: { CPXM(GetABSO()); Pc += 2; } break;
				case 0xcc: { CPYM(GetABSO()); Pc += 2; } break;

				//Absolute XY
				case 0xbd: { LDAM(GetABSX()); Pc += 2; } break;
				case 0xb9: { LDAM(GetABSY()); Pc += 2; } break;
				case 0xbc: { LDYM(GetABSX()); Pc += 2; } break;
				case 0xbe: { LDXM(GetABSY()); Pc += 2; } break;

				case 0x9d: { STA(GetABSX()); Pc += 2; } break;
				case 0x99: { STA(GetABSY()); Pc += 2; } break;

				case 0xfe: { INC(GetABSX()); Pc += 2; } break;
				case 0xde: { DEC(GetABSX()); Pc += 2; } break;
				case 0x1e: { ASLM(GetABSX()); Pc += 2; } break;
				case 0x5e: { LSRM(GetABSX()); Pc += 2; } break;
				case 0x3e: { ROLM(GetABSX()); Pc += 2; } break;
				case 0x7e: { RORM(GetABSX()); Pc += 2; } break;

				case 0x7d: { ADCM(GetABSX()); Pc += 2; } break;
				case 0x79: { ADCM(GetABSY()); Pc += 2; } break;
				case 0xfd: { SBCM(GetABSX()); Pc += 2; } break;
				case 0xf9: { SBCM(GetABSY()); Pc += 2; } break;
				case 0x1d: { ORAM(GetABSX()); Pc += 2; } break;
				case 0x19: { ORAM(GetABSY()); Pc += 2; } break;
				case 0x3d: { ANDM(GetABSX()); Pc += 2; } break;
				case 0x39: { ANDM(GetABSY()); Pc += 2; } break;
				case 0x5d: { EORM(GetABSX()); Pc += 2; } break;
				case 0x59: { EORM(GetABSY()); Pc += 2; } break;

				case 0xdd: { CMPM(GetABSX()); Pc += 2; } break;
				case 0xd9: { CMPM(GetABSY()); Pc += 2; } break;

				//Indirect X
				case 0xa1: { LDAM(GetINDX()); Pc++; } break;

				case 0x81: { STA(GetINDX()); Pc++; } break;

				case 0xc1: { CMPM(GetINDX()); Pc++; } break;

				case 0x61: { ADCM(GetINDX()); Pc++; } break;
				case 0xe1: { SBCM(GetINDX()); Pc++; } break;
				case 0x01: { ORAM(GetINDX()); Pc++; } break;
				case 0x21: { ANDM(GetINDX()); Pc++; } break;
				case 0x41: { EORM(GetINDX()); Pc++; } break;

				//Indirect Y
				case 0xb1: { LDAM(GetINDY()); Pc++; } break;

				case 0x91: { STA(GetINDY()); Pc++; } break;

				case 0xd1: { CMPM(GetINDY()); Pc++; } break;

				case 0x11: { ORAM(GetINDY()); Pc++; } break;
				case 0xf1: { SBCM(GetINDY()); Pc++; } break;
				case 0x71: { ADCM(GetINDY()); Pc++; } break;
				case 0x31: { ANDM(GetINDY()); Pc++; } break;
				case 0x51: { EORM(GetINDY()); Pc++; } break;

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
			framecycles += cyclestable[op] * 3;

			if (c.ppu.ppunmi)
				NMI();
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
			c.mapper.CpuWrite(Sp | 0x100, (u8)(Pc >> 8));
			Sp--;
			c.mapper.CpuWrite(Sp | 0x100, (u8)Pc);
			Sp--;
			c.mapper.CpuWrite(Sp | 0x100, Ps);
			Sp--;
			Pc = (c.mapper.CpuRead(0xfffb) << 8) | c.mapper.CpuRead(0xfffa);
			c.ppu.ppunmi = false;
			cycles += 7;
			totalcycles += 7;
			ppucycles += 7 * 3;
		}

		private void RTI()
		{
			PLP();
			Sp++;
			Pc = c.mapper.CpuRead(Sp | 0x100);
			Sp++;
			Pc |= c.mapper.CpuRead(Sp | 0x100) << 8;
		}

		private void RTS()
		{
			Sp++;
			Pc = c.mapper.CpuRead(Sp | 0x100);
			Sp++;
			Pc |= c.mapper.CpuRead(Sp | 0x100) << 8;
			Pc++;
		}

		private void JSR(int v)
		{
			Pc++;
			c.mapper.CpuWrite(Sp | 0x100, (u8)(Pc >> 8));
			Sp--;
			c.mapper.CpuWrite(Sp | 0x100, (u8)Pc);
			Sp--;
			Pc = v;
		}

		private void JMP(int v)
		{
			Pc = v;
		}

		private void TSX()
		{
			X = Sp;
			SetZero(X);
			SetNegative(X);
		}

		private void TXS()
		{
			Sp = X;
		}

		private void PLP()
		{
			Sp++;
			Ps = c.mapper.CpuRead(Sp | 0x100);
			UpdateFlags();
			//ps = (u8)(ps & 0xef);
		}

		private void PLA()
		{
			Sp++;
			A = c.mapper.CpuRead(Sp | 0x100);
			SetZero(A);
			SetNegative(A);
		}

		private void PHP()
		{
			c.mapper.CpuWrite(Sp | 0x100, (u8)(Ps | 0x30));
			Sp--;
			UpdateFlags();
		}

		private void PHA()
		{
			c.mapper.CpuWrite(Sp | 0x100, A);
			Sp--;
		}

		private void BVS(int v)
		{
			if (fv)
			{
				Pc = v;
				cycles++;
				totalcycles++;
				ppucycles += 3;
			}
			else
			{
				Pc++;
			}
		}

		private void BCS(int v)
		{
			if (fc)
			{
				Pc = v;
				cycles++;
				totalcycles++;
				ppucycles += 3;
			}
			else
			{
				Pc++;
			}
		}

		private void BEQ(int v)
		{
			if (fz)
			{
				Pc = v;
				cycles++;
				totalcycles++;
				ppucycles += 3;
			}
			else
			{
				Pc++;
			}
		}

		private void BPL(int v)
		{
			if (!fn)
			{
				Pc = v;
				cycles++;
				totalcycles++;
				ppucycles += 3;
			}
			else
			{
				Pc++;
			}
		}

		private void BMI(int v)
		{
			if (fn)
			{
				Pc = v;
				cycles++;
				totalcycles++;
				ppucycles += 3;
			}
			else
			{
				Pc++;
			}
		}

		private void BNE(int v)
		{
			if (!fz)
			{
				Pc = v;
				cycles++;
				totalcycles++;
				ppucycles += 3;
			}
			else
			{
				Pc++;
			}
		}

		private void BVC(int v)
		{
			if (!fv)
			{
				Pc = v;
				cycles++;
				totalcycles++;
				ppucycles += 3;
			}
			else
			{
				Pc++;
			}
		}

		private void BCC(int v)
		{
			if (!fc)
			{
				Pc = v;
				cycles++;
				totalcycles++;
				ppucycles += 3;
			}
			else
			{
				Pc++;
			}
		}

		private void BIT(int v)
		{
			int b = c.mapper.CpuRead(v);
			int t = (u8)(A & b);
			SetZero(t);
			SetNegative(b);
			SetOverflow(b);
		}

		private void AND()
		{
			A &= c.mapper.CpuRead(Pc);
			SetZero(A);
			SetNegative(A);
		}

		private void ANDM(int v)
		{
			A &= c.mapper.CpuRead(v);
			SetZero(A);
			SetNegative(A);
		}

		private void DEC(int v)
		{
			int b = c.mapper.CpuRead(v) - 1;
			c.mapper.CpuWrite(v, (u8)b);
			SetZero(b);
			SetNegative(b);
		}

		private void INC(int v)
		{
			int b = c.mapper.CpuRead(v) + 1;
			c.mapper.CpuWrite(v, (u8)b);
			SetZero(b);
			SetNegative(b);
		}

		private void STY(int v1)
		{
			pagecrossed = false;
			c.mapper.CpuWrite(v1, Y);
		}

		private void STX(int v1)
		{
			pagecrossed = false;
			c.mapper.CpuWrite(v1, X);
		}

		private void STA(int v1)
		{
			pagecrossed = false;
			c.mapper.CpuWrite(v1, A);
		}

		private void LDY()
		{
			Y = c.mapper.CpuRead(Pc);
			SetZero(Y);
			SetNegative(Y);
		}

		private void LDYM(int v)
		{
			Y = c.mapper.CpuRead(v);
			SetZero(Y);
			SetNegative(Y);
		}

		private void LDX()
		{
			X = c.mapper.CpuRead(Pc);
			SetZero(X);
			SetNegative(X);
		}

		private void LDXM(int v)
		{
			X = c.mapper.CpuRead(v);
			SetZero(X);
			SetNegative(X);
		}

		private void CPY()
		{
			int v = c.mapper.CpuRead(Pc);
			int t = Y - v;
			fc = Y >= v;
			fz = Y == v;
			SetNegative(t);
		}

		private void CPYM(int v)
		{
			v = c.mapper.CpuRead(v);
			int t = Y - v;
			fc = Y >= v;
			fz = Y == v;
			SetNegative(t);
		}

		private void CPX()
		{
			int v = c.mapper.CpuRead(Pc);
			int t = X - v;
			fc = X >= v;
			fz = X == v;
			SetNegative(t);
		}

		private void CPXM(int v)
		{
			v = c.mapper.CpuRead(v);
			int t = X - v;
			fc = X >= v;
			fz = X == v;
			SetNegative(t);
		}

		private void CMP()
		{
			int v = c.mapper.CpuRead(Pc);
			int t = A - v;
			fc = A >= v;
			fz = A == v;
			SetNegative(t);
		}

		private void CMPM(int v)
		{
			v = c.mapper.CpuRead(v);
			int t = A - v;
			fc = A >= v;
			fz = A == v;
			SetNegative(t);
		}

		private void EOR()
		{
			A = (u8)(A ^ c.mapper.CpuRead(Pc));
			SetZero(A);
			SetNegative(A);
		}

		private void EORM(int v)
		{
			A = (u8)(A ^ c.mapper.CpuRead(v));
			SetZero(A);
			SetNegative(A);
		}

		private void ORA()
		{
			A = (u8)(A | c.mapper.CpuRead(Pc));
			SetZero(A);
			SetNegative(A);
		}

		private void ORAM(int v)
		{
			A = (u8)(A | c.mapper.CpuRead(v));
			SetZero(A);
			SetNegative(A);
		}

		private void SBC()
		{
			int v = c.mapper.CpuRead(Pc);
			int r = A + ~v + (fc ? 1 : 0);
			SetZero(r);
			SetNegative(r);
			fv = ((A ^ v) & (A ^ r) & 0x80) > 0;
			fc = (r & 0xff00) == 0;
			A = (u8)r;
		}

		private void SBCM(int v)
		{
			v = c.mapper.CpuRead(v);
			int r = A + ~v + (fc ? 1 : 0);
			SetZero(r);
			SetNegative(r);
			fv = ((A ^ v) & (A ^ r) & 0x80) > 0;
			fc = (r & 0xff00) == 0;
			A = (u8)r;
		}

		private void ADC()
		{
			int v = c.mapper.CpuRead(Pc);
			int r = A + v + (fc ? 1 : 0);
			SetZero(r);
			SetNegative(r);
			fv = (~(A ^ v) & (A ^ r) & 0x80) > 0;
			fc = r > 255;
			A = (u8)r;
		}

		private void ADCM(int v)
		{
			v = c.mapper.CpuRead(v);
			int r = A + v + (fc ? 1 : 0);
			SetZero(r);
			SetNegative(r);
			fv = (~(A ^ v) & (A ^ r) & 0x80) > 0;
			fc = r > 255;
			A = (u8)r;
		}

		private void LDA()
		{
			A = c.mapper.CpuRead(Pc);
			SetZero(A);
			SetNegative(A);
		}

		private void LDAM(int v)
		{
			A = c.mapper.CpuRead(v);
			SetZero(A);
			SetNegative(A);
		}

		private void UpdateFlags()
		{
			fc = (Ps & 0x01) > 0;
			fz = (Ps & 0x02) > 0;
			fi = (Ps & 0x04) > 0;
			fd = (Ps & 0x08) > 0;
			//fb = (ps & 0x10) > 0 ? true : false;
			fv = (Ps & 0x40) > 0;
			fn = (Ps & 0x80) > 0;
		}

		private void SetProcessorStatus()
		{
			int t = 0;
			if (fc) t |= 0x01;
			if (fz) t |= 0x02;
			if (fi) t |= 0x04;
			if (fd) t |= 0x08;
			if (fb) t |= 0x10;
			if (fv) t |= 0x40;
			if (fv) t |= 0x80;

			Ps = (u8)(t | 0x20);
		}

		private void DEY()
		{
			Y--;
			SetZero(Y);
			SetNegative(Y);
		}

		private void DEX()
		{
			X--;
			SetZero(X);
			SetNegative(X);
		}

		private void INY()
		{
			Y++;
			SetZero(Y);
			SetNegative(Y);
		}

		private void INX()
		{
			X++;
			SetZero(X);
			SetNegative(X);
		}

		private void TAY()
		{
			Y = A;
			SetZero(Y);
			SetNegative(Y);
		}

		private void TAX()
		{
			X = A;
			SetZero(X);
			SetNegative(X);
		}

		private void TYA()
		{
			A = Y;
			SetZero(A);
			SetNegative(A);
		}

		private void TXA()
		{
			A = X;
			SetZero(A);
			SetNegative(A);
		}

		private void ROL()
		{
			bool bit7 = (A & (1 << 7)) > 0;
			A <<= 1 & 0xff;
			if (fc)
				A |= (1 << 0);
			SetZero(A);
			SetNegative(A);
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
			c.mapper.CpuWrite(v, (u8)r);
			SetZero(r);
			SetNegative(r);
		}

		private void ASL()
		{
			fc = (A & (1 << 7)) > 0;
			A = (u8)((A << 1) & 0xfe);
			SetZero(A);
			SetNegative(A);
		}

		private void ASLM(int v)
		{
			int r = c.mapper.CpuRead(v);
			fc = (r & (1 << 7)) > 0;
			r = (r << 1) & 0xfe;
			c.mapper.CpuWrite(v, (u8)r);
			SetZero(r);
			SetNegative(r);
		}

		private void ROR()
		{
			bool bit0 = (A & (1 << 0)) > 0;
			A >>= 1 & 0xff;
			if (fc)
				A |= (1 << 7);
			SetZero(A);
			SetNegative(A);
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
			c.mapper.CpuWrite(v, (u8)r);
			SetZero(r);
			SetNegative(r);
		}

		private void LSR()
		{
			fc = (A & (1 << 0)) > 0;
			A = (u8)((A >> 1) & 0x7f);
			SetZero(A);
			SetNegative(A);
		}

		private void LSRM(int v)
		{
			int r = c.mapper.CpuRead(v);
			fc = (A & (1 << 0)) > 0;
			r = (u8)((r >> 1) & 0x7f);
			c.mapper.CpuWrite(v, (u8)r);
			SetZero(r);
			SetNegative(r);
		}

		private void SetZero(int v)
		{
			fz = (u8)v == 0;
		}

		private void SetNegative(int v)
		{
			fn = ((u8)(v >> 7) & 1) > 0;
		}

		private void SetOverflow(int v)
		{
			fv = ((u8)(v >> 6) & 1) > 0;
		}

		private int GetZERP()
		{
			return c.mapper.ram[Pc];
		}

		private int GetZERX()
		{
			return (u8)(c.mapper.ram[Pc] + X);
		}

		private int GetZERY()
		{
			return (u8)(c.mapper.ram[Pc] + Y);
		}

		private int GetABSO()
		{
			return c.mapper.ram[Pc + 1] << 8 | c.mapper.ram[Pc];
		}

		private int GetABSX()
		{
			int b1 = c.mapper.ram[Pc];
			int b2 = c.mapper.ram[Pc + 1];
			int b3 = (b1 | b2 << 8) + X;
			pagecrossed = (b3 & 0xff00) != b2 << 8;
			return b3 & 0xffff;
		}

		private int GetABSY()
		{
			int b1 = c.mapper.ram[Pc];
			int b2 = c.mapper.ram[Pc + 1];
			int b3 = (b1 | b2 << 8) + Y;
			pagecrossed = (b3 & 0xff00) != b2 << 8;
			return b3 & 0xffff;
		}

		private int GetINDX()
		{
			int b1 = (u8)(c.mapper.ram[Pc] + X);
			return c.mapper.ram[b1] | c.mapper.ram[(u8)(b1 + 1)] << 8;
		}

		private int GetINDY()
		{
			int b1 = c.mapper.ram[Pc];
			int b2 = c.mapper.ram[(u8)(b1 + 1)];
			int b3 = (c.mapper.ram[b1] | b2 << 8) + Y;
			pagecrossed = (b3 & 0xff00) != b2 << 8;
			return b3 & 0xffff;
		}

		private int GetINDI()
		{
			int addr = c.mapper.CpuRead(Pc) | c.mapper.CpuRead(Pc + 1) << 8;

			if ((addr & 0xff) == 0xff)
			{
				return c.mapper.ram[addr] | (c.mapper.ram[addr & 0xff00]) << 8;
			}
			else
			{
				return c.mapper.ram[addr + 1] << 8 | c.mapper.CpuRead(addr);
			}
		}

		private int GetRELA()
		{
			int b1 = c.mapper.CpuRead(Pc);
			return Pc + (s8)(b1) + 1; ;
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
