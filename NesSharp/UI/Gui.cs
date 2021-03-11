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
		private int bpindex;
		private bool bpwindow;
		private bool br, bw, be;
		private MemoryEditor mem;
		public string gamename;

		public Gui(Main core)
		{
			c = core;
			mem = new MemoryEditor();
			inputtext = "";
		}

		public void DebuggerView(RenderWindow window, Clock clock)
		{
			if (c.cpu == null)
				return;

			bool wopen = true;
			ImGuiWindowFlags wflags = ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar |
						  ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse;

			if (ImGui.Begin("Debugger", ref wopen, wflags))
			{
				ImGui.SetWindowPos(new ImVec2(517, 25));
				ImGui.SetWindowSize(new ImVec2(400, window.Size.Y - 35));

				if (ImGui.BeginChild("Buttons"))
				{
					if (ImGui.Button("Run"))
					{
						jumpto = false;
						c.cpu.Breakmode = false;
						c.emustate = State.Running;
						c.cpu.StepOne();
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

						if (c.emustate == State.Running)
							c.emustate = State.Debug;
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
					ShowRegisters();

					ImGui.Spacing();
					ImGui.Separator();

					ShowFlags();

					ImGui.Spacing();
					ImGui.Separator();

					SetInputText("Cycles:", c.cpu.ppucycles.ToString(), 20, 30, 10);

					ImGui.SameLine(0,1);

					SetInputText("Total Cycles:", c.cpu.totalcycles.ToString(), 20, 70, 10);

					ImGui.SameLine(0, 1);

					SetInputText("Scanline:", c.ppu.ppu_scanline.ToString(), 20, 30, 10);

					ImGui.Spacing();
					ImGui.Separator();

					string[] bpitems = c.cpu.breakpoints.Select(k => k.BpString).ToArray();

					ImGui.Text("Breakpoints");

					ImGui.PushItemWidth(-1);
					if (ImGui.ListBox("##listbp", ref bpindex, bpitems, bpitems.Length, 6))
					{
						if (c.cpu.breakpoints.Count > 0)
							c.cpu.breakpoints.RemoveAt(bpindex);
					}
					ImGui.PopItemWidth();

					int offset = inputtext != "" ? Convert.ToInt32(inputtext, 16) : -1;

					var res = c.cpu.breakpoints.FirstOrDefault(b => b.Offset == offset);

					if (ImGui.Button("Add Breakpoint"))
					{
						if (offset > -1)
						{
							if (br)
								c.cpu.breakpoints.Add(new Breakpoint(offset, Breakpoint.Type.Read, true));
							else if (bw)
								c.cpu.breakpoints.Add(new Breakpoint(offset, Breakpoint.Type.Write, true));
							else if (be)
								c.cpu.breakpoints.Add(new Breakpoint(offset, Breakpoint.Type.Execute, true));
							else
								c.cpu.breakpoints.Remove(res);
						}
					}

					if (res != null && res.BpType == Breakpoint.Type.Read)
						br = res.IsBP;
					else if (res != null && res.BpType == Breakpoint.Type.Write)
						bw = res.IsBP;
					else if (res != null && res.BpType == Breakpoint.Type.Execute)
						be = res.IsBP;

					ImGui.SameLine();

					ImGui.PushItemWidth(36);
					ImGui.InputText("##bpinput", ref inputtext, 4, ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.CharsUppercase);
					ImGui.PopItemWidth();

					ImGui.SameLine();
					ImGui.Checkbox("Read", ref br);
					ImGui.SameLine();
					ImGui.Checkbox("Write", ref bw);
					ImGui.SameLine();
					ImGui.Checkbox("Exec", ref be);

					ImGui.EndChild();
				}

				ImGui.Spacing();
				ImGui.Spacing();
				ImGui.Separator();

				if (ImGui.BeginChild(""))
				{
					int lines = (int)Math.Min(ImGui.GetContentRegionAvail().Y / ImGui.GetTextLineHeightWithSpacing(), 1000);
					int Pc = (jumpto && inputtext.Length > 0 ? Convert.ToInt32(inputtext, 16) : c.cpu.Pc) - 9;// - lines;

					for (int i = 0; i < lines; i++)
					{
						int opsize;
						u8 Op = c.mapper.ram[Pc];
						string line = c.tracer.DisassembleFCEUXFormat(Op, Pc, c.cpu.A, c.cpu.X, c.cpu.Y, c.cpu.Ps, c.cpu.Sp, out opsize, 0, true);

						if (c.emustate == State.Debug)
						{
							var res = c.cpu.breakpoints.FirstOrDefault(b => b.Offset == Pc);
							bool v = res != null ? true : false;

							if (ImGui.Checkbox("##bpchk" + i, ref v))
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
				ImGui.End();
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
						ImGui.PushItemWidth(-1);
						ImGui.SetWindowPos(new ImVec2(0, 25));
						ImGui.SetWindowSize(new ImVec2(256, window.Size.Y));

						if (ImGui.ListBox("##Roms", ref romselectindex, files, files.Length, 20))
						{
							gamename = folder + "/" + files[romselectindex];
							c.emustate = State.Running;
							filemanager = false;
						}

						ImGui.PopItemWidth();
						ImGui.End();
					}
				}

				GuiImpl.Render(window);
				window.Display();
			}
		}

		public float MainMenu()
		{
			float wsize = 0;
			if (ImGui.BeginMainMenuBar())
			{
				wsize = ImGui.GetWindowHeight();
				if (ImGui.BeginMenu("File"))
				{
					filemanager = ImGui.MenuItem("Open");
					//if (ImGui.MenuItem("Reset") && c.cpu != null)
					///	c.showram = true;
					ImGui.EndMenu();
				}

				if (ImGui.BeginMenu("Reset"))
				{
					resetemu = true;
					c.emustate = State.Reset;
					//filemanager = ImGui.MenuItem("Open");
					//if (ImGui.MenuItem("Reset") && c.cpu != null)
					///	c.showram = true;
					ImGui.EndMenu();
				}

				if (c.emustate == State.Running)
				{
					if (ImGui.BeginMenu("Debug"))
					{
						if (ImGui.MenuItem("Debugger"))
							c.emustate = State.Debug;
						if (ImGui.MenuItem("Memory"))
							showram = true;
						ImGui.EndMenu();
					}

					if (ImGui.BeginMenu("Tracer") && c.emustate == State.Running)
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
			{
				tracelog = check;
				if (c.tracer.tracelines.Count > 1)
				{
					File.WriteAllLines("tracenes.log", c.tracer.tracelines.ToArray());
					c.tracer.tracelines.Clear();
					c.tracer.tracelines.Add("FCEUX 2.2.3 - Trace Log File");
				}

			}

			ImGui.PopItemWidth();
		}

		private void SetInputText(string labeltext, string inputtext, uint size, int width, int inputspace)
		{
			ImGui.PushItemWidth(1);
			ImGui.LabelText(labeltext, "");
			ImGui.PopItemWidth();
			ImGui.SameLine(0, 1);

			ImGui.PushItemWidth(width);
			ImGui.InputText("##" + inputspace.ToString(), ref inputtext, size);
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
