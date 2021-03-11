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

		public int emustate;

		public void Run()
		{
			var window = new RenderWindow(new SFML.Window.VideoMode(256, 240), "Nes Sharp");
			GuiImpl.Init(window);
			window.Closed += (s, e) => window.Close();
			window.Size = new Vector2u(window.Size.X * 6 - 170, window.Size.Y * 4);
			window.Position = new Vector2i(20, 20);

			window.SetFramerateLimit(60);

			emustate = State.Reset;

			gui = new Gui(this);

			gui.filemanager = true;

			SetAppStyle();

			while (window.IsOpen)
			{
				window.DispatchEvents();

				if (!window.IsOpen)
					break;

				switch (emustate)
				{
					case State.Running:
						cpu.Step();
						if (!cpu.Breakmode)
							ppu.RenderScanline();

						if (cpu.Breakmode)
							emustate = State.Debug;

						UpdateScreen(window);
						break;
					case State.Reset:
						UpdateReset(window);
						break;
					case State.Debug:
						GuiImpl.Update(window, clock.Restart());

						RenderFrame(window);

						GuiImpl.Render(window);
						window.Display();
						break;
				}
			}

			if (cpu != null && mapper != null && mapper.ram != null)
			{
				File.WriteAllBytes("ram.bin", mapper.ram);
				File.WriteAllBytes("vram.bin", mapper.vram);
			}

			GuiImpl.Shutdown();
		}

		private void UpdateReset(RenderWindow window)
		{
			GuiImpl.Update(window, clock.Restart());

			if (gui.resetemu)
			{
				Reset();
				emustate = State.Running;
				return;
			}

			gui.MainMenu();
			gui.LoadFile(window, clock);
			if (emustate == State.Running)
				Initialize(window);

			GuiImpl.Render(window);
			window.Display();
		}

		private void UpdateScreen(RenderWindow window)
		{
			if (ppu.ppu_scanline == 262)
			{
				window.Clear();
				GuiImpl.Update(window, clock.Restart());

				if (gui.filemanager)
				{
					gui.LoadFile(window, clock);
					Initialize(window);
					return;
				}

				RenderFrame(window);

				GuiImpl.Render(window);
				window.Display();
			}
		}

		private void RenderFrame(RenderWindow window)
		{
			bool wopen = true;


			ImGuiWindowFlags wflags = ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar |
									  ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse;

			gui.MainMenu();
			gui.DebuggerView(window, clock);
			gui.MemoryView(window);

			ppu.emutex.Update(ppu.gfxdata);
			ppu.emusprite.Texture = ppu.emutex;

			uint id = ppu.emusprite.Texture.NativeHandle;

			if (ImGui.Begin("Nes Sharp", ref wopen, wflags))
			{
				ImGui.SetWindowPos(new ImVec2(0, 25));
				ImGui.SetWindowSize(new ImVec2(512, 480));
				ImGui.Image((IntPtr)id, new ImVec2(512, 464));
			}
		}

		private void SetAppStyle()
		{
			//ImGui.StyleColorsLight();
		}

		private void Initialize(RenderWindow window)
		{
			ppu = new Ppu(this, window);
			cpu = new Cpu(this);
			mapper = new Mapper(this);
			control = new Controls();
			tracer = new Tracer(this);
			Reset();
		}

		private void Reset()
		{
			cpu.Reset();
			ppu.Reset();
			gui.resetemu = false;
		}
	}
}
