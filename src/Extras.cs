using System;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace Aster {
partial class MainWindow {
 Form compact;
 void SetupShortcuts(){KeyPreview=true;KeyDown+=(s,e)=>{if(e.KeyCode==Keys.F11){e.Handled=true;e.SuppressKeyPress=true;ToggleFocusMode();}else if(e.Control&&e.KeyCode==Keys.K){e.Handled=true;e.SuppressKeyPress=true;ShowPalette();}else if(e.KeyCode==Keys.F5){e.Handled=true;if(IsInventory())LoadInventory();else StartSample();}};}
 void ShowPalette(){ShowActionLauncher();}
 void BuildCompact(){sub.Text="A small always-on-top display using the existing monitor samples · Ctrl+K opens quick navigation";var box=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,Padding=new Padding(14)};box.Controls.Add(new Label{Text="Keep the essentials in view.",AutoSize=true,Font=new Font("Segoe UI",22)});box.Controls.Add(new Label{Text="CPU, RAM, network and process counts in a compact floating panel.\r\nIt reuses Aster's readings, so it does not add a second system sampler.\r\nClose the panel at any time. Aster must stay open.",AutoSize=true,ForeColor=muted,Margin=new Padding(0,20,0,20)});var open=Button("Open floating monitor");open.Click+=(s,e)=>{if(compact!=null&&!compact.IsDisposed){compact.Activate();return;}compact=new Form{Text="Aster · Mini monitor",Size=new Size(370,250),MinimumSize=new Size(300,220),TopMost=true,BackColor=panel,ForeColor=Color.White,Icon=SuiteArt.AppIcon()};var text=new Label{Dock=DockStyle.Fill,Font=new Font("Segoe UI",15),Padding=new Padding(18)};compact.Controls.Add(text);var clock=new Timer{Interval=1000};clock.Tick+=(o,a)=>{if(current!=null)text.Text="CPU  "+current.Cpu.ToString("F1")+"%\r\nRAM  "+current.MemoryPercent.ToString("F1")+"%\r\n↓ "+Inventory.Bytes(current.Receive)+"/s  ↑ "+Inventory.Bytes(current.Send)+"/s\r\n"+current.Processes.Count+" processes · "+current.Time.ToString("T");};compact.FormClosed+=(o,a)=>clock.Dispose();clock.Start();compact.Show(this);};box.Controls.Add(open);content.Controls.Add(box);}
}
}



