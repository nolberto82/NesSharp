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


		public enum Type
		{ Execute, Read, Write };

		public Breakpoint(int offset, int bptype)
		{
			Offset = offset;
			BpType = (Type)bptype;
		}
	}
}
