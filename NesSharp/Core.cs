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
using u8 = System.Byte;
using System.Linq;

namespace NesSharp
{
	public class Core
	{
		public Mapper mapper;
		public Cpu cpu;
		public Ppu ppu;
		public Controls control;
		public Tracer tracer;
		private bool initializedone;
		private bool fileselectopen;
		int romselectindex = 0;

		private enum State
		{
			Reset,
			Running,
			Paused,
			Debug
		}

		State state = State.Reset;
		private bool fileopen;
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

			CircleShape shape = new CircleShape(100);
			shape.FillColor = Color.Green;

			Clock clock = new Clock();

			while (window.IsOpen)
			{
				window.DispatchEvents();

				if (state == State.Reset)
					LoadInitialFile(window, clock);

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

							MainMenu(window);

							if (fileopen)
								OpenFile(window);

							window.Draw(ppu.emusprite);
							GuiImpl.Render(window);
							window.Display();
						}
						break;
					case State.Debug:
						GuiImpl.Update(window, clock.Restart());
						MainMenu(window);
						if (fileopen)
						{
							state = State.Reset;
							OpenFile(window);
							continue;
						}

						Disassembly();

						window.Draw(ppu.emusprite);
						GuiImpl.Render(window);
						window.Display();
						break;
				}
			}

			if (cpu != null && mapper.ram != null)
			{
				if (tracer.tracelines.Count > 0)
					File.WriteAllLines("tracenes.log", tracer.tracelines.ToArray());

				File.WriteAllBytes("ram.bin", mapper.ram);
				File.WriteAllBytes("vram.bin", mapper.vram);
			}

			GuiImpl.Shutdown();
		}

		private void Disassembly()
		{
			if (ImGui.Begin("Debugger"))
			{
				if (ImGui.Button("Run"))
				{
					cpu.Breakmode = false;
					state = State.Running;
					return;
				}

				ImGui.SameLine();

				if (ImGui.Button("Step Into"))
				{
					cpu.StepOne();
					if (cpu.ppucycles >= 341)
					{
						ppu.RenderScanline();
						cpu.ppucycles -= 341;
					}
				}

				ImGui.Separator();

				ImGui.LabelText("", "BPX:");
				//ImGui.InputText("",)

				ImGui.Separator();

				int Pc = cpu.Pc;
				for (int i = 0; i < 20; i++)
				{
					ImGui.PushID(i);
					int opsize;
					bool v = cpu.breakpoints[Pc];
					u8 Op = mapper.ram[Pc];
					string line = tracer.DisassembleFCEUXFormat(Op, Pc, cpu.A, cpu.X, cpu.Y, cpu.Ps, cpu.Sp, out opsize, 0, true);

					if (ImGui.Checkbox("", ref v))
						cpu.breakpoints[Pc] = v;
					ImGui.SameLine();
					ImGui.Text(line);
					Pc += opsize;
					if (Pc > 0xffff)
						break;
					ImGui.PopID();
				}

				ImGui.End();
			}
		}

		private void LoadInitialFile(RenderWindow window, Clock clock)
		{
			while (state == State.Reset && window.IsOpen)
			{
				window.DispatchEvents();
				GuiImpl.Update(window, clock.Restart());
				window.Clear();
				MainMenu(window);

				if (fileopen)
					OpenFile(window);

				GuiImpl.Render(window);
				window.Display();
			}
		}

		private void Initialize(RenderWindow window, string filename)
		{
			ppu = new Ppu(this, window);
			cpu = new Cpu(this, filename);
			control = new Controls();
			tracer = new Tracer(this);
		}

		private void MainMenu(RenderWindow window)
		{
			if (ImGui.BeginMainMenuBar())
			{
				if (ImGui.BeginMenu("File"))
				{
					fileopen = ImGui.MenuItem("Open");
					ImGui.EndMenu();
				}

				if (ImGui.BeginMenu("Debug"))
				{
					if (ImGui.MenuItem("Debugger") && cpu != null)
						state = State.Debug;
					ImGui.EndMenu();
				}

				ImGui.EndMainMenuBar();
			}
		}

		private void OpenFile(RenderWindow window)
		{
			if (ImGui.Begin("Open Rom"))
			{
				string folder = "roms";
				string[] files = Directory.EnumerateFiles("roms", "*.nes").Select(Path.GetFileName).ToArray();

				if (ImGui.ListBox("", ref romselectindex, files, files.Length, 20))
				{
					Initialize(window, folder + "/" + files[romselectindex]);

					state = State.Running;
					fileopen = false;
				}

				ImGui.End();
			}
		}
	}
}
