using ImGuiNET;
using Saffron2D.GuiCollection;
using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using u8 = System.Byte;

namespace NesSharp.UI
{
	public class Gui
	{
		private Main c;
		public bool filemanager;
		private string inputtext;
		private bool jumpto;
		public bool resetemu;
		public bool showram;
		private MemoryEditor mem;

		public Gui(Main core)
		{
			c = core;
			mem = new MemoryEditor();
			inputtext = "";
		}

		public void MainMenu()
		{
			if (ImGui.BeginMainMenuBar())
			{
				if (ImGui.BeginMenu("File"))
				{
					filemanager = ImGui.MenuItem("Open");
					//if (ImGui.MenuItem("Reset") && c.cpu != null)
					///	c.showram = true;
					ImGui.EndMenu();
				}

				if (c.state == State.Running)
				{
					if (ImGui.BeginMenu("Debug"))
					{
						if (ImGui.MenuItem("Debugger"))
							c.state = State.Debug;
						if (ImGui.MenuItem("Memory"))
							showram = true;
						ImGui.EndMenu();
					}

					if (ImGui.BeginMenu("Tracer") && c.state == State.Running)
					{
						if (ImGui.MenuItem("Trace") && c.cpu != null)
							c.cpu.trace = !c.cpu.trace;
						ImGui.EndMenu();
					}
				}

				ImGui.EndMainMenuBar();
			}
		}

		public void LoadFile(RenderWindow window, Clock clock)
		{
			while (filemanager && window.IsOpen)
			{
				window.DispatchEvents();
				GuiImpl.Update(window, clock.Restart());
				window.Clear();

				MainMenu();

				if (filemanager)
				{
					int romselectindex = 0;
					if (ImGui.Begin("Open Rom"))
					{
						string folder = "roms";
						string[] files = Directory.EnumerateFiles("roms", "*.nes").Select(Path.GetFileName).ToArray();

						if (ImGui.ListBox("", ref romselectindex, files, files.Length, 20))
						{
							c.cpu.LoadRom(folder + "/" + files[romselectindex]);
							c.state = State.Running;
							//c.state = State.Debug;
							filemanager = false;
						}

						ImGui.End();
					}
				}

				GuiImpl.Render(window);
				window.Display();
			}
		}

		public void DebuggerView()
		{
			if (c.cpu == null)
				return;

			if (ImGui.Begin("Debugger"))
			{
				if (ImGui.BeginChild("Buttons"))
				{
					if (ImGui.Button("Run"))
					{
						jumpto = false;
						c.cpu.Breakmode = false;
						c.state = State.Running;
						return;
					}

					ImGui.SameLine();

					if (ImGui.Button("Step Into"))
					{
						c.cpu.StepOne();
						if (c.cpu.ppucycles >= 341)
						{
							c.ppu.RenderScanline();
							c.cpu.ppucycles -= 341;
						}
					}

					ImGui.SameLine();

					if (ImGui.Button("Add BP"))
					{
						if (ImGui.Begin("Breakpoints"))
						{
							ImGui.Text("123");
							ImGui.End();
						}
					}

					ImGui.SameLine();

					if (ImGui.Button("Goto"))
						jumpto = true;

					if (inputtext.Length < 4)
						jumpto = false;

					ImGui.SameLine();

					ImGui.PushItemWidth(36);
					ImGui.InputText(" ", ref inputtext, 4, ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.CharsUppercase);
					ImGui.PopItemWidth();
				}

				if (ImGui.BeginChild(""))
				{
					ImGui.Separator();
					ShowRegisters();
					ImGui.Spacing();
					ImGui.Separator();
					ShowFlags();
					ImGui.Spacing();
					ImGui.Separator();
				}

				int lines = (int)Math.Min(ImGui.GetContentRegionAvail().Y / ImGui.GetTextLineHeightWithSpacing(), 512);
				int Pc = (jumpto ? Convert.ToInt32(inputtext, 16) : c.cpu.Pc);// - lines / 2;
				int dpc = 0;
				if (c.cpu.breakpoints.Count > 0)
					dpc = c.cpu.breakpoints.FirstOrDefault(p => p.BpType == Breakpoint.Type.Execute).Offset;

				if (ImGui.BeginChild(""))
				{
					for (int i = 0; i < lines; i++)
					{
						ImGui.PushID(i);
						int opsize;

						var res = c.cpu.breakpoints.FirstOrDefault(b => b.Offset == Pc);
						bool v = res != null ? true : false;
						u8 Op = c.mapper.ram[Pc];
						string line = c.tracer.DisassembleFCEUXFormat(Op, Pc, c.cpu.A, c.cpu.X, c.cpu.Y, c.cpu.Ps, c.cpu.Sp, out opsize, 0, true);

						if (Pc == c.cpu.Pc)
							ImGui.Selectable(line, true);
						else
							ImGui.Text(line);

						Pc += opsize;
						if (Pc > 0xffff)
							break;

						ImGui.PopID();
					}

					ImGui.EndChild();
				}
				ImGui.End();
			}
		}

		public void MemoryView()
		{
			mem.Draw("Memory", c.mapper.ram, c.mapper.ram.Length);
		}

		private void ShowRegisters()
		{
			SetInputTextSameLine("PC:", c.cpu.Pc.ToString("X4"), 4, 34, 1);
			SetInputTextSameLine("A:", c.cpu.A.ToString("X2"), 2, 22, 2);
			SetInputTextSameLine("X:", c.cpu.X.ToString("X2"), 2, 22, 3);
			SetInputTextSameLine("Y:", c.cpu.Y.ToString("X2"), 2, 22, 4);
			SetInputTextSameLine("SP:", c.cpu.Sp.ToString("X2"), 2, 22, 5);
			SetInputTextSameLine("PS:", c.cpu.Ps.ToString("X2"), 2, 22, 6);
		}

		private void ShowFlags()
		{
			SetFlags("N", c.cpu.Fn);
			SetFlags("V", c.cpu.Fv);
			SetFlags("U", c.cpu.Fu);
			SetFlags("B", c.cpu.Fb);
			SetFlags("D", c.cpu.Fd);
			SetFlags("I", c.cpu.Fi);
			SetFlags("Z", c.cpu.Fz);
			SetFlags("C", c.cpu.Fc);
		}

		private void SetFlags(string flag, bool v)
		{
			ImGui.Checkbox(flag, ref v);
			ImGui.SameLine();
		}

		private void SetInputTextSameLine(string labeltext, string inputtext, uint size, int width, int inputspace)
		{
			ImGui.PushItemWidth(1);
			ImGui.LabelText(labeltext, "");
			ImGui.PopItemWidth();
			ImGui.SameLine(0, 1);

			ImGui.PushItemWidth(width);
			ImGui.InputText("##" + inputspace.ToString(), ref inputtext, size, ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.CharsUppercase);
			ImGui.PopItemWidth();
			ImGui.SameLine(0, 2);
		}
	}
}
