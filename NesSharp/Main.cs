using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ImGuiNET;
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

		public int state;

		public void Run()
		{
			var window = new RenderWindow(new SFML.Window.VideoMode(256, 240), "Nes Sharp");
			GuiImpl.Init(window);
			window.Closed += (s, e) => window.Close();
			window.Size = new Vector2u(window.Size.X * 6 - 170, window.Size.Y * 4);
			window.Position = new Vector2i(20, 20);

			window.SetFramerateLimit(60);

			state = State.Reset;

			gui = new Gui(this);

			gui.filemanager = true;

			SetAppStyle();

			while (window.IsOpen)
			{
				window.DispatchEvents();

				if (!window.IsOpen)
					break;

				switch (state)
				{
					case State.Running:
						cpu.Step();
						ppu.RenderScanline();

						if (cpu.Breakmode)
							state = State.Debug;

						UpdateScreen(window);
						break;
					case State.Reset:
						GuiImpl.Update(window, clock.Restart());

						gui.MainMenu();
						Initialize(window, gui.LoadFile(window, clock));

						GuiImpl.Render(window);
						window.Display();
						break;
					case State.Debug:
						GuiImpl.Update(window, clock.Restart());

						gui.MainMenu();
						gui.MemoryView(window);
						gui.DebuggerView(window);

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

		private void UpdateScreen(RenderWindow window)
		{
			if (ppu.ppu_scanline == 261)
			{
				window.Clear();
				GuiImpl.Update(window, clock.Restart());

				ppu.emutex.Update(ppu.gfxdata);
				ppu.emusprite.Texture = ppu.emutex;

				float wsize = gui.MainMenu();

				if (gui.resetemu)
					Initialize(window,"");

				if (gui.filemanager)
				{
					Initialize(window, gui.LoadFile(window, clock));
					return;
				}

				gui.BreakpointView(window, clock);
				gui.MemoryView(window);
				gui.DebuggerView(window);

				bool wopen = true;
				uint id = ppu.emusprite.Texture.NativeHandle;
				ImGuiWindowFlags wflags = ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar |
										  ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse;

				if (ImGui.Begin("Nes Sharp", ref wopen, wflags))
				{
					ImGui.SetWindowPos(new ImVec2(0, 25));
					ImGui.SetWindowSize(new ImVec2(512, 480));
					ImGui.Image((IntPtr)id, new ImVec2(512, 464));
				}

				GuiImpl.Render(window);
				window.Display();
			}
		}

		private void SetAppStyle()
		{
			ImGui.StyleColorsLight();
		}

		private void Initialize(RenderWindow window, string gamename)
		{
			ppu = new Ppu(this, window);
			cpu = new Cpu(this, gamename);
			control = new Controls();
			tracer = new Tracer(this);
		}
	}
}
