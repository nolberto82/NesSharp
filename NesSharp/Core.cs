using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NesSharp
{
	public class Core
	{
		public Mapper mapper;
		public Cpu cpu;
		public Ppu ppu;
		public Controls control;
		public Tracer tracer;

		private uint mFrame;
		private uint mFps;
		private Clock mClock = new Clock();

		public void Run()
		{
			using (RenderWindow window = new RenderWindow(new VideoMode(256, 240), "Nes Sharp"))
			{
				window.Closed += (s, e) => window.Close();

				Initialize(window);

				window.SetFramerateLimit(60);

				while (window.IsOpen)
				{
					if (mClock.ElapsedTime.AsSeconds() >= 1f)
					{
						mFps = mFrame;
						mFrame = 0;
						mClock.Restart();
					}

					++mFrame;

					//window.Clear();
					window.DispatchEvents();

					cpu.Execute();

					//window.SetTitle(mFps.ToString());
					//Console.WriteLine(mFps);
				}
			}

			if (tracer.tracelines.Count > 0)
			{
				File.WriteAllLines("tracenes.log", tracer.tracelines.ToArray());
			}

			File.WriteAllBytes("ram.bin", mapper.ram);
			File.WriteAllBytes("vram.bin", mapper.vram);
		}

		public void Initialize(RenderWindow window)
		{
			ppu = new Ppu(this, window);
			cpu = new Cpu(this, "roms/smb.nes");
			control = new Controls();
			tracer = new Tracer(this);
		}
	}
}
