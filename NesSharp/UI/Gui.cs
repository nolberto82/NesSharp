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
using ImVec2 = System.Numerics.Vector2;
using u8 = System.Byte;

namespace NesSharp.UI
{
	public class Gui
	{
		public bool filemanager;
		public bool resetemu;
		public bool showram;
		private Main c;
		private string inputtext;
		private bool jumpto;
		public bool tracelog;
		private int index;
		private bool bpwindow;
		private MemoryEditor mem;

		public Gui(Main core)
		{
			c = core;
			mem = new MemoryEditor();
			inputtext = "";
		}

		public void BreakpointView(RenderWindow window, Clock clock)
		{
			ImGuiWindowFlags wflags = ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar |
			  ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse;

			if (ImGui.Begin("Breakpoints", ref bpwindow, wflags))
			{
				ImGui.SetWindowPos(new ImVec2(517, 30 + window.Size.Y / 2));
				ImGui.SetWindowSize(new ImVec2(400, window.Size.Y / 2 - 40));

				for (int i = 0; i < 3; i++)
					SetCheckBoxBP("" + i, 1, 20, i);

				ImGui.End();
			}

		}

		public void DebuggerView(RenderWindow window)
		{
			if (c.cpu == null)
				return;

			bool wopen = true;
			ImGuiWindowFlags wflags = ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar |
						  ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse;

			if (ImGui.Begin("Debugger", ref wopen, wflags))
			{
				ImGui.SetWindowPos(new ImVec2(517, 25));
				ImGui.SetWindowSize(new ImVec2(400, window.Size.Y / 2));

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

						if (c.state == State.Running)
							c.state = State.Debug;
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

					ImGui.SameLine();

					SetCheckBox("Trace", 4, 20, 1);

					ImGui.Separator();
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

				int lines = 500;// (int)Math.Min(ImGui.GetContentRegionAvail().Y / ImGui.GetTextLineHeightWithSpacing(), 1000);
				int Pc = (jumpto && inputtext.Length == 4 ? Convert.ToInt32(inputtext, 16) : c.cpu.Pc);// - lines / 2;

				if (ImGui.BeginChild(""))
				{
					for (int i = 0; i < lines; i++)
					{
						int opsize;
						u8 Op = c.mapper.ram[Pc];
						string line = c.tracer.DisassembleFCEUXFormat(Op, Pc, c.cpu.A, c.cpu.X, c.cpu.Y, c.cpu.Ps, c.cpu.Sp, out opsize, 0, true);

						if (c.state == State.Debug)
						{
							var res = c.cpu.breakpoints.FirstOrDefault(b => b.Offset == Pc);
							bool v = res != null ? true : false;

							if (ImGui.Checkbox("##" + i, ref v))
							{
								if (v)
									c.cpu.breakpoints.Add(new Breakpoint(Pc, 0, true));
								else
									c.cpu.breakpoints.Remove(res);
							}
							ImGui.SameLine();
						}

						ImGui.PushID(i);

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

		public void MemoryView(RenderWindow window)
		{
			bool wopen = true;
			string[] ramsel = { "RAM", "VRAM" };

			ImGuiWindowFlags wflags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse;

			if (ImGui.Begin(ramsel[index], ref wopen, wflags))
			{
				ImGui.Combo("Memory Selection", ref index, ramsel, ramsel.Length);
				ImGui.SetWindowPos(new ImVec2(922, 25));
				ImGui.SetWindowSize(new ImVec2(430, window.Size.Y - 35));

				if (index == 0)
					mem.Draw(ramsel[index], c.mapper.ram, c.mapper.ram.Length);
				else
					mem.Draw(ramsel[index], c.mapper.vram, c.mapper.vram.Length);
			}
		}

		public string LoadFile(RenderWindow window, Clock clock)
		{
			string filename = "";
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
						ImGui.PushItemWidth(-1);
						ImGui.SetWindowPos(new ImVec2(0, 25));
						ImGui.SetWindowSize(new ImVec2(256, window.Size.Y));

						if (ImGui.ListBox("##Roms", ref romselectindex, files, files.Length, 20))
						{
							filename = folder + "/" + files[romselectindex];

							c.state = State.Running;
							filemanager = false;
						}

						ImGui.PopItemWidth();
						ImGui.End();
					}
				}

				GuiImpl.Render(window);
				window.Display();
			}
			return filename;
		}

		public float MainMenu()
		{
			float wsize = 0;
			if (ImGui.BeginMainMenuBar())
			{
				wsize = ImGui.GetWindowHeight(); ;
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
			return wsize;
		}

		private void SetFlags(string flag, bool v)
		{
			ImGui.Checkbox(flag, ref v);
			ImGui.SameLine();
		}

		private void SetCheckBox(string labeltext, uint size, int width, int inputspace)
		{
			bool check = tracelog;
			ImGui.PushItemWidth(1);
			ImGui.LabelText(labeltext, "");
			ImGui.PopItemWidth();
			ImGui.SameLine(0, 1);

			ImGui.PushItemWidth(width);
			if (ImGui.Checkbox("##" + inputspace.ToString(), ref check))
				tracelog = check;
			ImGui.PopItemWidth();
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

		private void SetCheckBoxBP(string labeltext, uint size, int width, int inputspace)
		{
			bool check = tracelog;
			ImGui.PushItemWidth(1);
			ImGui.LabelText(labeltext, "");
			ImGui.PopItemWidth();
			ImGui.SameLine(0, 1);

			ImGui.PushItemWidth(width);
			if (ImGui.Checkbox("##" + inputspace.ToString(), ref check))
				tracelog = check;
			ImGui.PopItemWidth();
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

		private void ShowRegisters()
		{
			SetInputTextSameLine("PC:", c.cpu.Pc.ToString("X4"), 4, 34, 1);
			SetInputTextSameLine("A:", c.cpu.A.ToString("X2"), 2, 22, 2);
			SetInputTextSameLine("X:", c.cpu.X.ToString("X2"), 2, 22, 3);
			SetInputTextSameLine("Y:", c.cpu.Y.ToString("X2"), 2, 22, 4);
			SetInputTextSameLine("SP:", c.cpu.Sp.ToString("X2"), 2, 22, 5);
			SetInputTextSameLine("PS:", c.cpu.Ps.ToString("X2"), 2, 22, 6);
		}
	}
}
