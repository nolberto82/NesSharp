using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NesSharp
{
	public class Breakpoint
	{
		public int Offset;
		public Type BpType;
		public RamType RType;
		public bool IsBP;
		public string BpString;

		public enum Type
		{
			Execute,
			Read,
			Write
		}

		public enum RamType
		{
			RAM,
			VRAM
		}

		public Breakpoint(int offset, Type bptype, bool rtype = false)
		{
			Offset = offset;
			BpType = bptype;
			RType = rtype ? RamType.VRAM : RamType.RAM;
			IsBP = true;
			BpString = $"{offset:X4}:{BpType}:{RType}";
		
		}
	}
}
