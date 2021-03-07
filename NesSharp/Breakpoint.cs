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
		public bool IsBP;
		public float PositionY;

		public Breakpoint(int offset, bool isBP)
		{
			Offset = offset;
			IsBP = isBP;
		}
	}
}
