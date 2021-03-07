using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ImGuiNET;
//using ImGuiSfmlNet;
using Saffron2D.GuiCollection;
using ImVec2 = System.Numerics.Vector2;
using ImVec3 = System.Numerics.Vector3;
using ImVec4 = System.Numerics.Vector4;
using u8 = System.Byte;
using System.Linq;
using NesSharp.UI;

namespace NesSharp
{
	public class Main
	{
		public Mapper mapper;
		public Cpu cpu;
		public Ppu ppu;
		public Controls control;
		public Tracer tracer;
		public Gui gui;
		public Clock clock = new Clock();
		private bool initializedone;
		private bool fileselectopen;

		public int state;

		private bool debugmode;

		public void Run()
		{
			//using (RenderWindow window = new RenderWindow(new VideoMode(256, 240), "Nes Sharp"))
			//{
			var window = new RenderWindow(new SFML.Window.VideoMode(256, 240), "Nes Sharp");
			GuiImpl.Init(window);
			window.Closed += (s, e) => window.Close();
			window.Size = new Vector2u(window.Size.X * 3, window.Size.Y * 3);
			window.Position = new Vector2i((int)(window.Size.X / 2), (int)(window.Size.Y / 6));

			window.SetFramerateLimit(60);

			state = State.Reset;

			Initialize(window);

			while (window.IsOpen)
			{
				window.DispatchEvents();

				if (!window.IsOpen)
					break;

				//if (mapper == null)
				//	break;
				window.Clear();

				switch (state)
				{
					case State.Running:
						cpu.Step();
						ppu.RenderScanline();
						if (cpu.Breakmode)
							state = State.Debug;

						if (ppu.ppu_scanline == 261)
						{
							GuiImpl.Update(window, clock.Restart());
							ppu.emutex.Update(ppu.gfxdata);
							ppu.emusprite.Texture = ppu.emutex;

							gui.MainMenu(window);

							if (gui.filemanager)
							{
								gui.LoadFile(window, clock);
								//Initialize(window);
								break;
							}

							window.Draw(ppu.emusprite);
							GuiImpl.Render(window);
							window.Display();
						}
						break;
					case State.Debug:
						GuiImpl.Update(window, clock.Restart());
						gui.MainMenu(window);

						if (gui.filemanager)
						{
							state = State.Reset;
							gui.LoadFile(window, clock);
							continue;
						}

						gui.DebuggerView();

						window.Draw(ppu.emusprite);
						GuiImpl.Render(window);
						window.Display();
						break;
					case State.Reset:
						GuiImpl.Update(window, clock.Restart());
						gui.MainMenu(window);
						gui.LoadFile(window, clock);
						GuiImpl.Render(window);
						window.Display();
						break;
				}
			}

			if (cpu != null && mapper != null && mapper.ram != null)
			{
				if (tracer.tracelines.Count > 0)
					File.WriteAllLines("tracenes.log", tracer.tracelines.ToArray());

				File.WriteAllBytes("ram.bin", mapper.ram);
				File.WriteAllBytes("vram.bin", mapper.vram);
			}

			GuiImpl.Shutdown();
		}

		private void Initialize(RenderWindow window)
		{
			ppu = new Ppu(this, window);
			cpu = new Cpu(this);
			control = new Controls();
			tracer = new Tracer(this);
			gui = new Gui(this);
		}
	}
}
