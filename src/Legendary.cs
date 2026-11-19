using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Aster {
// Rules use elapsed sample time, not sample count. Long gaps break continuity.
class PressureRule {
 DateTime? since,previous,lastAlert;
 public void Reset(){since=null;previous=null;lastAlert=null;}
 public bool Observe(double value,double threshold,int seconds,int cooldown,DateTime now,double maxGap){
  bool discontinuity=previous.HasValue&&(now<=previous.Value||(now-previous.Value).TotalSeconds>maxGap);
  if(discontinuity)since=null;
  if(previous.HasValue&&now<previous.Value)lastAlert=null;
  previous=now;
  if(double.IsNaN(value)||double.IsInfinity(value)||value<threshold){since=null;return false;}
  if(!since.HasValue)since=now;
  if((now-since.Value).TotalSeconds<seconds)return false;
  if(lastAlert.HasValue&&(now-lastAlert.Value).TotalSeconds<cooldown)return false;
  lastAlert=now;since=now;return true;
 }
}
static class DiagnosticsBundle {
 public static string Csv(IEnumerable<Snapshot> samples){
  var b=new StringBuilder("Time UTC,CPU percent,Memory percent,Available RAM bytes,Total RAM bytes,Process count\r\n");
  foreach(var s in samples)b.AppendLine(string.Join(",",new[]{s.Time.ToUniversalTime().ToString("o"),s.Cpu.ToString("F2",CultureInfo.InvariantCulture),s.MemoryPercent.ToString("F2",CultureInfo.InvariantCulture),s.Available.ToString(CultureInfo.InvariantCulture),s.Total.ToString(CultureInfo.InvariantCulture),s.Processes.Count.ToString(CultureInfo.InvariantCulture)}));
  return b.ToString();
 }
 public static string Report(IEnumerable<Snapshot> samples,string version,bool paused){
  var values=samples.ToArray();var b=new StringBuilder("ASTER DIAGNOSTICS\r\nVersion: "+version+"\r\nCaptured UTC: "+DateTime.UtcNow.ToString("o")+"\r\nMonitoring: "+(paused?"paused":"running")+"\r\nSamples: "+values.Length+" (up to 60 most recent samples)\r\n");
  if(values.Length>0){b.AppendLine("Average CPU: "+values.Average(s=>s.Cpu).ToString("F1",CultureInfo.InvariantCulture)+"%");b.AppendLine("Peak CPU: "+values.Max(s=>s.Cpu).ToString("F1",CultureInfo.InvariantCulture)+"%");b.AppendLine("Peak memory: "+values.Max(s=>s.MemoryPercent).ToString("F1",CultureInfo.InvariantCulture)+"%");}
  b.AppendLine("\r\nThis is a resource snapshot, not a Windows health or malware scan. Paused or delayed collection can leave gaps.");
  b.AppendLine("Contains aggregate measurements and timestamps only. No process names, computer name, serial numbers, location, notes, Telegram credentials or feed messages are included. Nothing is uploaded.");return b.ToString();
 }
 public static void Write(string path,string report,string csv){
  // Assemble in memory before writing so no partially assembled ZIP is saved.
  using(var data=new MemoryStream()){
   using(var zip=new ZipArchive(data,ZipArchiveMode.Create,true)){
    foreach(var pair in new[]{new[]{"report.txt",report},new[]{"performance.csv",csv}}){var entry=zip.CreateEntry(pair[0]);using(var writer=new StreamWriter(entry.Open(),new UTF8Encoding(false)))writer.Write(pair[1]);}
   }
   File.WriteAllBytes(path,data.ToArray());
  }
 }
}
partial class MainWindow {
 readonly PressureRule cpuPressure=new PressureRule(),ramPressure=new PressureRule();
 readonly List<string> pressureEvents=new List<string>();
 int unreadPressure;
 Label commandStatus;ListBox pressureList;
 void TrackPressure(Snapshot s){
  if(preferences.PressureEnabled){
   double gap=Math.Max(10,timer.Interval/1000.0*3);
   if(cpuPressure.Observe(s.Cpu,Math.Max(1,Math.Min(100,preferences.CpuLimit)),Math.Max(5,preferences.PressureSeconds),Math.Max(60,preferences.PressureCooldown),s.Time,gap))AddPressure(s.Time,"CPU",s.Cpu,"Open Processes to review busy apps. Heavy work may explain this.");
   if(ramPressure.Observe(s.MemoryPercent,Math.Max(1,Math.Min(100,preferences.RamLimit)),Math.Max(5,preferences.PressureSeconds),Math.Max(60,preferences.PressureCooldown),s.Time,gap))AddPressure(s.Time,"Memory",s.MemoryPercent,"Save work before closing apps you no longer need.");
  }else{cpuPressure.Reset();ramPressure.Reset();}
  if(commandStatus!=null&&!commandStatus.IsDisposed)commandStatus.Text=CommandStatus();
 }
 void AddPressure(DateTime time,string metric,double value,string advice){AddSessionEvent(metric+" performance alert: "+value.ToString("F1")+"%");
  pressureEvents.Insert(0,time.ToString("HH:mm:ss")+"  ·  "+metric+" "+value.ToString("F1")+"%  ·  "+advice);
  if(pressureEvents.Count>100)pressureEvents.RemoveAt(100);
  if(page!="Performance alerts")unreadPressure++;
  RefreshPressureList();
 }
 void RefreshPressureList(){if(pressureList==null||pressureList.IsDisposed)return;pressureList.Items.Clear();pressureList.Items.AddRange(pressureEvents.Cast<object>().ToArray());}
 string CommandStatus(){return (paused?"Monitoring paused":"Monitoring live")+"  •  "+(current==null?"Waiting for readings":"CPU "+current.Cpu.ToString("F0")+"%  /  RAM "+current.MemoryPercent.ToString("F0")+"%")+"\r\n"+(preferences.PressureEnabled?"Performance alerts on":"Performance alerts off")+"  •  "+unreadPressure+" unread alerts  •  Aster "+AsterUpdate.CurrentVersion+"\r\n"+MaintenanceSummary();}
 FlowLayoutPanel FeatureFlow(string title,string description){
  var flow=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(18)};content.Controls.Add(flow);
  flow.Controls.Add(new Label{Text=title,AutoSize=true,Font=new Font("Segoe UI",26,FontStyle.Bold),Margin=new Padding(0,0,0,12)});
  flow.Controls.Add(new Label{Text=description,AutoSize=true,MaximumSize=new Size(720,0),Margin=new Padding(0,0,0,18)});return flow;
 }
 void BuildCommandCenter(){
  sub.Text="Your daily launchpad · live status, focused actions and troubleshooting";
  var flow=FeatureFlow("Make room for what matters.","Jump into a workflow, check your system, or give your desktop a new mood.");
  commandStatus=new Label{Text=CommandStatus(),AutoSize=true,Font=new Font("Segoe UI",13),Margin=new Padding(0,8,0,22)};flow.Controls.Add(commandStatus);
  foreach(var group in new[]{new[]{"PLAN & REVIEW","Maintenance planner","Performance compare","Session timeline","Diagnostics bundle"},new[]{"KEEP THINGS SMOOTH","Performance alerts","Processes","Diagnostics bundle","Windows advisor"},new[]{"GET INTO YOUR FLOW","Focus timer","Notebook","Checklist","Network lab"},new[]{"MAKE IT YOURS","Themes","Wallpaper studio","Video player","Home layout"}}){
   flow.Controls.Add(new Label{Text=group[0],AutoSize=true,Font=new Font("Segoe UI",10,FontStyle.Bold),Margin=new Padding(0,16,0,8)});
   var row=new FlowLayoutPanel{Width=760,Height=100,WrapContents=true};flow.Controls.Add(row);
   foreach(var targetName in group.Skip(1)){string target=targetName;var button=Button(target+"  →");button.AutoSize=false;button.Width=175;button.Height=70;button.Click+=(s,e)=>Navigate(target);row.Controls.Add(button);}
  }
  var toggle=Button(paused?"Resume live monitoring":"Pause live monitoring");toggle.Click+=(s,e)=>{paused=!paused;pause.Text=paused?"Resume":"Pause";toggle.Text=paused?"Resume live monitoring":"Pause live monitoring";cpuPressure.Reset();ramPressure.Reset();commandStatus.Text=CommandStatus();UpdateView();};flow.Controls.Add(toggle);
  flow.Controls.Add(new Label{Text="Ctrl+K  Find any tool     •     F11  Distraction-free view",AutoSize=true,Margin=new Padding(0,20,0,0)});
 }
 NumericUpDown PressureNumber(FlowLayoutPanel flow,string label,int min,int max,int value){
  var row=new FlowLayoutPanel{Width=710,Height=38};row.Controls.Add(new Label{Text=label,Width=400,Height=30,TextAlign=ContentAlignment.MiddleLeft});
  var number=new NumericUpDown{Minimum=min,Maximum=max,Value=Math.Max(min,Math.Min(max,value)),Width=110,AccessibleName=label};row.Controls.Add(number);flow.Controls.Add(row);return number;
 }
 void BuildPressureAlerts(){
  unreadPressure=0;sub.Text="Optional local alerts · no changes to Windows or running apps";
  var flow=FeatureFlow("Catch resource pressure early.","Alerts appear inside Aster while it is open and monitoring. Choose how long CPU or memory must stay above a limit. A gap in samples resets the waiting period.");
  var enabled=new CheckBox{Text="Enable performance alerts",Checked=preferences.PressureEnabled,AutoSize=true,Margin=new Padding(0,0,0,16)};flow.Controls.Add(enabled);
  var cpuLimit=PressureNumber(flow,"CPU threshold (%)",1,100,preferences.CpuLimit);
  var ramLimit=PressureNumber(flow,"Memory threshold (%)",1,100,preferences.RamLimit);
  var duration=PressureNumber(flow,"Sustained for (seconds)",5,600,preferences.PressureSeconds);
  var quiet=PressureNumber(flow,"Minimum time between alerts per metric (seconds)",60,3600,preferences.PressureCooldown);
  var saved=new Label{Text="Changes take effect when you save.",AutoSize=true};
  var save=Button("Save alert rules");save.Click+=(s,e)=>{
   bool oldEnabled=preferences.PressureEnabled;int oldCpu=preferences.CpuLimit,oldRam=preferences.RamLimit,oldSeconds=preferences.PressureSeconds,oldCooldown=preferences.PressureCooldown;
   preferences.PressureEnabled=enabled.Checked;preferences.CpuLimit=(int)cpuLimit.Value;preferences.RamLimit=(int)ramLimit.Value;preferences.PressureSeconds=(int)duration.Value;preferences.PressureCooldown=(int)quiet.Value;
   try{preferences.Save();cpuPressure.Reset();ramPressure.Reset();saved.Text="Saved · "+(enabled.Checked?"watching new samples":"alerts disabled");}
   catch(Exception ex){preferences.PressureEnabled=oldEnabled;preferences.CpuLimit=oldCpu;preferences.RamLimit=oldRam;preferences.PressureSeconds=oldSeconds;preferences.PressureCooldown=oldCooldown;saved.Text="Could not save: "+ex.Message;}
  };flow.Controls.Add(save);flow.Controls.Add(saved);
  flow.Controls.Add(new Label{Text="THIS SESSION · latest 100 alerts",AutoSize=true,Margin=new Padding(0,20,0,8)});
  pressureList=new ListBox{Width=740,Height=185,HorizontalScrollbar=true};flow.Controls.Add(pressureList);RefreshPressureList();
  var clear=Button("Clear session alerts");clear.Click+=(s,e)=>{pressureEvents.Clear();unreadPressure=0;RefreshPressureList();};flow.Controls.Add(clear);
 }
 void BuildDiagnosticsBundle(){
  sub.Text="Review every exported byte · saved locally · no automatic sharing";
  var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(12)};root.RowStyles.Add(new RowStyle(SizeType.Absolute,62));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,50));content.Controls.Add(root);
  root.Controls.Add(new Label{Text="A small support bundle with aggregate resource readings.\r\nReview both tabs, then save the ZIP wherever you choose.",Dock=DockStyle.Fill,AutoSize=true},0,0);
  var tabs=new TabControl{Dock=DockStyle.Fill};root.Controls.Add(tabs,0,1);
  var report=new TextBox{Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Both,WordWrap=false};var csv=new TextBox{Multiline=true,ReadOnly=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Both,WordWrap=false};
  var reportTab=new TabPage("report.txt");reportTab.Controls.Add(report);tabs.TabPages.Add(reportTab);var csvTab=new TabPage("performance.csv");csvTab.Controls.Add(csv);tabs.TabPages.Add(csvTab);
  Action capture=()=>{var samples=history.ToArray();report.Text=DiagnosticsBundle.Report(samples,AsterUpdate.CurrentVersion,paused);csv.Text=DiagnosticsBundle.Csv(samples);};capture();
  var actions=new FlowLayoutPanel{Dock=DockStyle.Fill};root.Controls.Add(actions,0,2);var refresh=Button("Capture fresh preview");refresh.Click+=(s,e)=>capture();actions.Controls.Add(refresh);
  var export=Button("Save reviewed ZIP…");actions.Controls.Add(export);export.Click+=(s,e)=>{using(var dialog=new SaveFileDialog{Filter="Diagnostics ZIP|*.zip",FileName="Aster-diagnostics-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".zip"})if(dialog.ShowDialog(this)==DialogResult.OK){try{DiagnosticsBundle.Write(dialog.FileName,report.Text,csv.Text);MessageBox.Show(this,"Saved the two files exactly as previewed. Nothing was uploaded.","Diagnostics saved");}catch(Exception ex){MessageBox.Show(this,"Could not save diagnostics: "+ex.Message);}}};
 }
}
}
