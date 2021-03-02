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

		public void Run()
		{
			using (RenderWindow window = new RenderWindow(new VideoMode(256, 240), "Nes Sharp"))
			{
				window.Closed += (s, e) => window.Close();
				window.Size = new Vector2u(512, 448);

				Initialize(window);

				window.SetFramerateLimit(60);

				while (window.IsOpen)
				{
					//window.Clear();
					window.DispatchEvents();

					cpu.Execute();


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
			cpu = new Cpu(this, "roms/nestress.nes");
			control = new Controls();
			tracer = new Tracer(this);
		}
	}
}
