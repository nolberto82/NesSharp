using SFML.Window;
using System;
using System.Collections.Generic;
using System.Text;
using u8 = System.Byte;

namespace NesSharp
{
	public class Controls
	{
		private u8 buttonid;
		private u8 strobe;

		public Controls()
		{
			Initialialize();
		}

		public void Initialialize()
		{
			buttonid = 1;
		}

		private bool GetKeys(u8 id)
		{
			switch (id)
			{
				case 0:
					return Keyboard.IsKeyPressed(Keyboard.Key.Z);
				case 1:
					return Keyboard.IsKeyPressed(Keyboard.Key.X);
				case 2:
					return Keyboard.IsKeyPressed(Keyboard.Key.Space);
				case 3:
					return Keyboard.IsKeyPressed(Keyboard.Key.Enter);
				case 4:
					return Keyboard.IsKeyPressed(Keyboard.Key.Up);
				case 5:
					return Keyboard.IsKeyPressed(Keyboard.Key.Down);
				case 6:
					return Keyboard.IsKeyPressed(Keyboard.Key.Left);
				case 7:
					return Keyboard.IsKeyPressed(Keyboard.Key.Right);
			}

			return false;
		}

		public void ControlWrite(u8 v)
		{
			strobe = (u8)(v & 1);

			if (strobe > 0)
			{
				buttonid = 0;
			}
		}

		public u8 ControlRead()
		{
			u8 val = 0x40;

			if (strobe == 0 && buttonid >= 0)
			{
				if (buttonid >= 8)
				{
					buttonid = 0;
				}

				if (GetKeys(buttonid))
				{				
					val = 0x41;
				}

				buttonid++;
			}
			return val;
		}
	}
}
