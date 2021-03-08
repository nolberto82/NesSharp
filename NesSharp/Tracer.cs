using System;
using System.Collections.Generic;
using System.Text;
using u8 = System.Byte;
using s8 = System.SByte;

namespace NesSharp
{
	public class Tracer
	{
		public Dictionary<int, string[]> opnames;
		public List<string> tracelines;
		private Main c;

		public Tracer(Main core)
		{
			c = core;
			tracelines = new List<string>();
			opnames = new Dictionary<int, string[]>();

			tracelines.Add("FCEUX 2.2.3 - Trace Log File");

			//Implied
			opnames[0x2a] = new[] { "ROL A", "ACCU" };
			opnames[0x0a] = new[] { "ASL A", "ACCU" };
			opnames[0x6a] = new[] { "ROR A", "ACCU" };
			opnames[0x4a] = new[] { "LSR A", "ACCU" };

			opnames[0x8a] = new[] { "TXA", "IMPL" };
			opnames[0x98] = new[] { "TYA", "IMPL" };
			opnames[0xaa] = new[] { "TAX", "IMPL" };
			opnames[0xa8] = new[] { "TAY", "IMPL" };

			opnames[0xe8] = new[] { "INX", "IMPL" };
			opnames[0xc8] = new[] { "INY", "IMPL" };
			opnames[0xca] = new[] { "DEX", "IMPL" };
			opnames[0x88] = new[] { "DEY", "IMPL" };

			opnames[0x38] = new[] { "SEC", "IMPL" };
			opnames[0x18] = new[] { "CLC", "IMPL" };
			opnames[0xf8] = new[] { "SED", "IMPL" };
			opnames[0xd8] = new[] { "CLD", "IMPL" };
			opnames[0x78] = new[] { "SEI", "IMPL" };
			opnames[0x58] = new[] { "CLI", "IMPL" };
			opnames[0xb8] = new[] { "CLV", "IMPL" };

			opnames[0xea] = new[] { "NOP", "IMPL" };

			//Immediate
			opnames[0xa9] = new[] { "LDA", "IMME" };
			opnames[0xa2] = new[] { "LDX", "IMME" };
			opnames[0xa0] = new[] { "LDY", "IMME" };

			opnames[0x69] = new[] { "ADC", "IMME" };
			opnames[0xe9] = new[] { "SBC", "IMME" };
			opnames[0x09] = new[] { "ORA", "IMME" };
			opnames[0x29] = new[] { "AND", "IMME" };
			opnames[0x49] = new[] { "EOR", "IMME" };

			opnames[0xc9] = new[] { "CMP", "IMME" };
			opnames[0xe0] = new[] { "CPX", "IMME" };
			opnames[0xc0] = new[] { "CPY", "IMME" };

			//Zero Page
			opnames[0xa5] = new[] { "LDA", "ZERP" };
			opnames[0xa6] = new[] { "LDX", "ZERP" };
			opnames[0xa4] = new[] { "LDY", "ZERP" };

			opnames[0x85] = new[] { "STA", "ZERP" };
			opnames[0x86] = new[] { "STX", "ZERP" };
			opnames[0x84] = new[] { "STY", "ZERP" };

			opnames[0xe6] = new[] { "INC", "ZERP" };
			opnames[0xc6] = new[] { "DEC", "ZERP" };
			opnames[0x06] = new[] { "ASL", "ZERP" };
			opnames[0x46] = new[] { "LSR", "ZERP" };
			opnames[0x26] = new[] { "ROL", "ZERP" };
			opnames[0x66] = new[] { "ROR", "ZERP" };

			opnames[0x65] = new[] { "ADC", "ZERP" };
			opnames[0xe5] = new[] { "SBC", "ZERP" };
			opnames[0x05] = new[] { "ORA", "ZERP" };
			opnames[0x25] = new[] { "AND", "ZERP" };
			opnames[0x45] = new[] { "EOR", "ZERP" };

			opnames[0x24] = new[] { "BIT", "ZERP" };
			opnames[0xc5] = new[] { "CMP", "ZERP" };
			opnames[0xe4] = new[] { "CPX", "ZERP" };
			opnames[0xc4] = new[] { "CPY", "ZERP" };

			//Zero Page XY
			opnames[0xb5] = new[] { "LDA", "ZERX" };
			opnames[0xb4] = new[] { "LDY", "ZERX" };
			opnames[0xb6] = new[] { "LDX", "ZERY" };

			opnames[0x95] = new[] { "STA", "ZERX" };
			opnames[0x94] = new[] { "STY", "ZERX" };
			opnames[0x96] = new[] { "STX", "ZERY" };

			opnames[0xf6] = new[] { "INC", "ZERX" };
			opnames[0xd6] = new[] { "DEC", "ZERX" };
			opnames[0x16] = new[] { "ASL", "ZERX" };
			opnames[0x56] = new[] { "LSR", "ZERX" };
			opnames[0x36] = new[] { "ROL", "ZERX" };
			opnames[0x76] = new[] { "ROR", "ZERX" };

			opnames[0x75] = new[] { "ADC", "ZERX" };
			opnames[0xf5] = new[] { "SBC", "ZERX" };
			opnames[0x15] = new[] { "ORA", "ZERX" };
			opnames[0x35] = new[] { "AND", "ZERX" };
			opnames[0x55] = new[] { "EOR", "ZERX" };

			opnames[0xd5] = new[] { "CMP", "ZERX" };

			//Absolute
			opnames[0xad] = new[] { "LDA", "ABSO" };
			opnames[0xae] = new[] { "LDX", "ABSO" };
			opnames[0xac] = new[] { "LDY", "ABSO" };

			opnames[0x8d] = new[] { "STA", "ABSO" };
			opnames[0x8e] = new[] { "STX", "ABSO" };
			opnames[0x8c] = new[] { "STY", "ABSO" };

			opnames[0xee] = new[] { "INC", "ABSO" };
			opnames[0xce] = new[] { "DEC", "ABSO" };
			opnames[0x0e] = new[] { "ASL", "ABSO" };
			opnames[0x4e] = new[] { "LSR", "ABSO" };
			opnames[0x2e] = new[] { "ROL", "ABSO" };
			opnames[0x6e] = new[] { "ROR", "ABSO" };

			opnames[0x6d] = new[] { "ADC", "ABSO" };
			opnames[0xed] = new[] { "SBC", "ABSO" };
			opnames[0x0d] = new[] { "ORA", "ABSO" };
			opnames[0x2d] = new[] { "AND", "ABSO" };
			opnames[0x4d] = new[] { "EOR", "ABSO" };

			opnames[0xcd] = new[] { "CMP", "ABSO" };
			opnames[0x2c] = new[] { "BIT", "ABSO" };
			opnames[0xec] = new[] { "CPX", "ABSO" };
			opnames[0xcc] = new[] { "CPY", "ABSO" };

			//Absolute XY
			opnames[0xbd] = new[] { "LDA", "ABSX" };
			opnames[0xb9] = new[] { "LDA", "ABSY" };
			opnames[0xbc] = new[] { "LDY", "ABSX" };
			opnames[0xbe] = new[] { "LDX", "ABSY" };

			opnames[0x9d] = new[] { "STA", "ABSX" };
			opnames[0x99] = new[] { "STA", "ABSY" };

			opnames[0xfe] = new[] { "INC", "ABSX" };
			opnames[0xde] = new[] { "DEC", "ABSX" };
			opnames[0x1e] = new[] { "ASL", "ABSX" };
			opnames[0x5e] = new[] { "LSR", "ABSX" };
			opnames[0x3e] = new[] { "ROL", "ABSX" };
			opnames[0x7e] = new[] { "ROR", "ABSX" };

			opnames[0x7d] = new[] { "ADC", "ABSX" };
			opnames[0x79] = new[] { "ADC", "ABSY" };
			opnames[0xfd] = new[] { "SBC", "ABSX" };
			opnames[0xf9] = new[] { "SBC", "ABSY" };
			opnames[0x1d] = new[] { "ORA", "ABSX" };
			opnames[0x19] = new[] { "ORA", "ABSY" };
			opnames[0x3d] = new[] { "AND", "ABSX" };
			opnames[0x39] = new[] { "AND", "ABSY" };
			opnames[0x5d] = new[] { "EOR", "ABSX" };
			opnames[0x59] = new[] { "EOR", "ABSY" };

			opnames[0xdd] = new[] { "CMP", "ABSX" };
			opnames[0xd9] = new[] { "CMP", "ABSY" };

			//Indirect X
			opnames[0xa1] = new[] { "LDA", "INDX" };

			opnames[0x81] = new[] { "STA", "INDX" };

			opnames[0xc1] = new[] { "CMP", "INDX" };

			opnames[0x61] = new[] { "ADC", "INDX" };
			opnames[0xe1] = new[] { "SBC", "INDX" };
			opnames[0x01] = new[] { "ORA", "INDX" };
			opnames[0x21] = new[] { "AND", "INDX" };
			opnames[0x41] = new[] { "EOR", "INDX" };

			//Indirect Y
			opnames[0xb1] = new[] { "LDA", "INDY" };

			opnames[0x91] = new[] { "STA", "INDY" };

			opnames[0xd1] = new[] { "CMP", "INDY" };

			opnames[0x11] = new[] { "ORA", "INDY" };
			opnames[0xf1] = new[] { "SBC", "INDY" };
			opnames[0x71] = new[] { "ADC", "INDY" };
			opnames[0x31] = new[] { "AND", "INDY" };
			opnames[0x51] = new[] { "EOR", "INDY" };

			//Branches
			opnames[0x90] = new[] { "BCC", "RELA" };
			opnames[0x50] = new[] { "BVC", "RELA" };
			opnames[0xd0] = new[] { "BNE", "RELA" };
			opnames[0x30] = new[] { "BMI", "RELA" };
			opnames[0x10] = new[] { "BPL", "RELA" };
			opnames[0xf0] = new[] { "BEQ", "RELA" };
			opnames[0xb0] = new[] { "BCS", "RELA" };
			opnames[0x70] = new[] { "BVS", "RELA" };

			//Stack
			opnames[0x48] = new[] { "PHA", "IMPL" };
			opnames[0x08] = new[] { "PHP", "IMPL" };

			opnames[0x68] = new[] { "PLA", "IMPL" };
			opnames[0x28] = new[] { "PLP", "IMPL" };

			opnames[0x9A] = new[] { "TXS", "IMPL" };
			opnames[0xbA] = new[] { "TSX", "IMPL" };

			//Jumps
			opnames[0x4c] = new[] { "JMP", "ABSO" };
			opnames[0x6c] = new[] { "JMP", "INDI" };
			opnames[0x20] = new[] { "JSR", "ABSO" };

			//Returns
			opnames[0x60] = new[] { "RTS", "IMPL" };
			opnames[0x40] = new[] { "RTI", "IMPL" };
			opnames[0x00] = new[] { "BRK", "IMPL" };

			//Unofficial Implied
			opnames[0x02] = new[] { "", "IMPL" };
			opnames[0x12] = new[] { "", "IMPL" };
			opnames[0x22] = new[] { "", "IMPL" };
			opnames[0x32] = new[] { "", "IMPL" };
			opnames[0x42] = new[] { "", "IMPL" };
			opnames[0x52] = new[] { "", "IMPL" };
			opnames[0x62] = new[] { "", "IMPL" };
			opnames[0x72] = new[] { "", "IMPL" };
			opnames[0x92] = new[] { "", "IMPL" };
			opnames[0xb2] = new[] { "", "IMPL" };
			opnames[0xd2] = new[] { "", "IMPL" };
			opnames[0xf2] = new[] { "", "IMPL" };
			opnames[0x1a] = new[] { "", "IMPL" };
			opnames[0x3a] = new[] { "", "IMPL" };
			opnames[0x5a] = new[] { "", "IMPL" };
			opnames[0x7a] = new[] { "", "IMPL" };
			opnames[0xda] = new[] { "", "IMPL" };
			opnames[0xfa] = new[] { "", "IMPL" };

			//Unofficial Immediate
			opnames[0x0b] = new[] { "", "IMME" };
			opnames[0x2b] = new[] { "", "IMME" };
			opnames[0x6b] = new[] { "", "IMME" };
			opnames[0x4b] = new[] { "", "IMME" }; 
			opnames[0xab] = new[] { "", "IMME" };
			opnames[0xcb] = new[] { "", "IMME" };
			opnames[0x80] = new[] { "", "IMME" };
			opnames[0x82] = new[] { "", "IMME" };
			opnames[0x89] = new[] { "", "IMME" };
			opnames[0xc2] = new[] { "", "IMME" };
			opnames[0xe2] = new[] { "", "IMME" };
			opnames[0xeb] = new[] { "", "IMME" };
			opnames[0x8b] = new[] { "", "IMME" };

			//opnames[0x80] = new[] { "", "NULL" };
			//opnames[0x82] = new[] { "", "NULL" };
			//opnames[0x89] = new[] { "", "NULL" };
			//opnames[0xc2] = new[] { "", "NULL" };
			//opnames[0xe2] = new[] { "", "NULL" };

			//opnames[0x0b] = new[] { "", "NULL" };
			//opnames[0x2b] = new[] { "", "NULL" };
			//opnames[0x4b] = new[] { "", "NULL" };
			//opnames[0x6b] = new[] { "", "NULL" };
			//opnames[0xab] = new[] { "", "NULL" };
			//opnames[0xcb] = new[] { "", "NULL" };

			////Zero Page
			//opnames[0x04] = new[] { "", "NULL" };
			//opnames[0x44] = new[] { "", "NULL" };
			//opnames[0x82] = new[] { "", "NULL" };

			//opnames[0x89] = new[] { "", "NULL" };
			//opnames[0xc2] = new[] { "", "NULL" };
			//opnames[0xe2] = new[] { "", "NULL" };
			//opnames[0x0b] = new[] { "", "NULL" };
			//opnames[0x2b] = new[] { "", "NULL" };
			//opnames[0x4b] = new[] { "", "NULL" };
			//opnames[0x6b] = new[] { "", "NULL" };
			//opnames[0xab] = new[] { "", "NULL" };

			//Zero Page XY
		}

		public string DisassembleFCEUXFormat(u8 op, int pc, u8 a, u8 x, u8 y, u8 ps, u8 sp, out int opsize, int cycles = 0, bool debugger = false)
		{
			string strpc = "";
			string strins = "";
			int b0;
			int b1;
			int b2;
			int b3;
			int addr;
			int v;
			string mode = opnames.ContainsKey(op) ? opnames[op][1] : "NULL";

			StringBuilder sb = new StringBuilder();

			if (!debugger)
				sb.Append($"c{cycles,-12}");

			switch (mode)
			{
				case "IMPL":
				case "ACCU":
					strpc = $"{pc:X4}:{op:X2}";
					strins = $"{opnames[op][0]}";
					break;
				case "IMME":
					b1 = c.mapper.CpuReadDebug(pc + 1);
					strpc = $"{pc:X4}:{op:X2} {b1:X2}";
					strins = $"{opnames[op][0]} #${b1:X2}";
					break;
				case "ZERP":
					b1 = c.mapper.CpuReadDebug(pc + 1);
					v = c.mapper.CpuReadDebug(b1);
					strpc = $"{pc:X4}:{op:X2} {b1:X2}";
					strins = $"{opnames[op][0]} ${b1:X4} = #${v:X2}";
					break;
				case "ZERX":
					b0 = c.mapper.CpuReadDebug(pc + 1);
					b1 = (u8)(b0 + x);
					v = c.mapper.CpuReadDebug(b1);
					strpc = $"{pc:X4}:{op:X2} {b1:X2}";
					strins = $"{opnames[op][0]} ${b0:X4},X @ ${b1:X4} = #${v:X2}";
					break;
				case "ZERY":
					b0 = c.mapper.CpuReadDebug(pc + 1);
					b1 = (u8)(b0 + y);
					v = c.mapper.CpuReadDebug(b1);
					strpc = $"{pc:X4}:{op:X2} {b1:X2}";
					strins = $"{opnames[op][0]} ${b0:X4},Y @ ${b1:X4} = #${v:X2}";
					break;
				case "ABSO":
					b1 = c.mapper.CpuReadDebug(pc + 1);
					b2 = c.mapper.CpuReadDebug(pc + 2);
					addr = (b2 << 8) | b1;
					v = c.mapper.CpuReadDebug(addr);

					if (op != 0x4c && op != 0x20)
					{
						strpc = $"{pc:X4}:{op:X2} {b1:X2} {b2:X2}";
						strins = $"{opnames[op][0]} ${addr:X4} = #${v:X2}";
					}
					else
					{
						strpc = $"{pc:X4}:{op:X2} {b1:X2} {b2:X2}";
						strins = $"{opnames[op][0]} ${addr:X4}";
					}

					break;
				case "ABSX":
					b1 = c.mapper.CpuReadDebug(pc + 1);
					b2 = c.mapper.CpuReadDebug(pc + 2);
					addr = ((b2 << 8) | b1) + x;
					v = c.mapper.CpuReadDebug(addr);
					strpc = $"{pc:X4}:{op:X2} {b1:X2} {b2:X2}";
					strins = $"{opnames[op][0]} ${addr - x:X4},X @ ${addr:X4} = #${v:X2}";
					break;
				case "ABSY":
					b1 = c.mapper.CpuReadDebug(pc + 1);
					b2 = c.mapper.CpuReadDebug(pc + 2);
					addr = (((b2 << 8) | b1) + y) & 0xffff;
					v = c.mapper.CpuReadDebug(addr);
					strpc = $"{pc:X4}:{op:X2} {b1:X2} {b2:X2}";
					strins = $"{opnames[op][0]} ${addr - y:X4},Y @ ${addr:X4} = #${v:X2}";
					break;
				case "INDX":
					b0 = c.mapper.CpuReadDebug(pc + 1);
					b1 = (u8)(b0 + x);
					b2 = c.mapper.CpuReadDebug(b1);
					b3 = c.mapper.CpuReadDebug((u8)(b1 + 1));
					addr = ((b3 << 8) | b2);
					v = c.mapper.CpuReadDebug(addr);
					strpc = $"{pc:X4}:{op:X2} {b0:X2}";
					strins = $"{opnames[op][0]} (${b0:X2},X) @ ${(u8)(b0 + x):X2} = #${v:X2}";
					//str1 += $"${pc:X4}:{op:X2} {b0,-11:X2} {opnames[op][0]} (${b0:X2},X) @ {(u8)(b0 + x):X2} = {addr:X4} = {v,-42:X2}";
					break;
				case "INDY":
					b0 = c.mapper.CpuReadDebug(pc + 1);
					b1 = (u8)(b0);
					b2 = c.mapper.CpuReadDebug(b1);
					b3 = c.mapper.CpuReadDebug((u8)(b1 + 1));
					addr = (((b3 << 8) | b2) + y) & 0xffff;
					v = c.mapper.CpuReadDebug(addr);
					strpc = $"{pc:X4}:{op:X2} {b0:X2}";
					strins = $"{opnames[op][0]} (${b0:X2}),Y @ ${addr:X4} = #${v:X2}";
					break;
				case "INDI":
					b0 = c.mapper.CpuReadDebug(pc + 1);
					b1 = c.mapper.CpuReadDebug(pc + 2);
					addr = ((b1 << 8) | b0);

					if (b0 == 0xff)
					{
						v = addr + 1;
					}
					else
					{
						v = c.mapper.CpuReadDebug(addr) | (u8)c.mapper.CpuReadDebug((addr + 1)) << 8;
					}

					strpc = $"{pc:X4}:{op:X2} {b0:X2} {b1:X2}";
					strins = $"{opnames[op][0]} (${addr:X4}) = #${v:X4}";
					//str1 += $"${pc:X4}:{op:X2} {b0:X2} {b1,-11:X2} {opnames[op][0]} (${addr:X4}) = {v,-42:X4}";
					break;
				case "RELA":
					b1 = c.mapper.CpuReadDebug(pc + 1);
					addr = pc + (s8)(b1) + 2;
					strpc = $"{pc:X4}:{op:X2} {b1:X2}";
					strins = $"{opnames[op][0]} ${addr:X2}";
					break;
				case "NULL":
					strpc = $"{pc:X4}";
					strins = $"UNDEFINED";
					break;
			}

			if (strins == "")
				strins = $"UNDEFINED";

			sb.Append($"${strpc,-14} {strins,-45}");

			if (!debugger)
			{
				sb.Append($"A:{a:X2} X:{x:X2} Y:{y:X2} S:{sp:X2} ");
				sb.Append(c.cpu.GetFlags());
				Console.WriteLine(sb.ToString());
				tracelines.Add(sb.ToString());
			}

			opsize = 1;
			switch (mode)
			{
				case "IMPL":
				case "ACCU":
					opsize = 1;
					break;
				case "IMME":
				case "RELA":
				case "ZERP":
				case "ZERX":
				case "ZERY":
				case "INDX":
				case "INDY":
				case "INDI":
					opsize = 2;
					break;
				case "ABSO":
				case "ABSX":
				case "ABSY":
					opsize = 3;
					break;
			}

			return sb.ToString();
		}
	}
}
