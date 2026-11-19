using System;using System.Collections.Generic;using System.Drawing;using System.Globalization;using System.Linq;using System.Text;using System.Windows.Forms;
namespace Aster {
class MaintenanceItem {
 public string Title="";public DateTime Due=DateTime.Today;public int RepeatDays;public bool Done;
 public void Complete(DateTime today){if(RepeatDays>0){Due=today.Date.AddDays(Math.Min(365,RepeatDays));Done=false;}else Done=true;}
 public override string ToString(){return (Done?"DONE":Due.Date<=DateTime.Today?"DUE":"PLANNED")+" · "+Title+" · "+Due.ToString("yyyy-MM-dd")+(RepeatDays>0?" · every "+RepeatDays+" days":"");}
}
class ComparisonReading {public DateTime Time;public double Cpu,Ram;public int Processes;}
class ComparisonWindow {
 public List<ComparisonReading> Readings=new List<ComparisonReading>();
 public string Describe(){if(Readings.Count==0)return "Not captured";return Readings[0].Time.ToString("HH:mm:ss")+" – "+Readings.Last().Time.ToString("HH:mm:ss")+"\r\nCPU average "+Readings.Average(x=>x.Cpu).ToString("F1")+"% · peak "+Readings.Max(x=>x.Cpu).ToString("F1")+"%\r\nRAM average "+Readings.Average(x=>x.Ram).ToString("F1")+"% · peak "+Readings.Max(x=>x.Ram).ToString("F1")+"%\r\nAverage process count "+Readings.Average(x=>x.Processes).ToString("F1");}
}
class ComparisonCapture {
 public ComparisonWindow Before,After;ComparisonWindow pending;bool captureBefore;public string State="Capture a before window to begin.";
 public bool Active{get{return pending!=null;}}
 public void Start(bool before){if(!before&&Before==null)throw new InvalidOperationException("Capture Before first.");if(Active)throw new InvalidOperationException("A capture is already running.");captureBefore=before;pending=new ComparisonWindow();State="Collecting 0 / 20 samples…";}
 public void Cancel(string reason){pending=null;State=reason;}
 public bool Add(Snapshot s,double maxGap){if(pending==null)return false;var values=pending.Readings;if(values.Count>0&&(s.Time<=values.Last().Time||(s.Time-values.Last().Time).TotalSeconds>maxGap)){Cancel("Capture stopped: samples were interrupted. Retry when monitoring is steady.");return false;}if(double.IsNaN(s.Cpu)||double.IsInfinity(s.Cpu)||double.IsNaN(s.MemoryPercent)||double.IsInfinity(s.MemoryPercent)){Cancel("Capture stopped: a resource reading is unavailable.");return false;}values.Add(new ComparisonReading{Time=s.Time,Cpu=s.Cpu,Ram=s.MemoryPercent,Processes=s.Processes.Count});State="Collecting "+values.Count+" / 20 samples…";if(values.Count<20)return false;if(captureBefore){Before=pending;After=null;}else After=pending;pending=null;State=(captureBefore?"Before":"After")+" window captured · 20 samples.";return true;}
 public string Report(){var b=new StringBuilder("ASTER PERFORMANCE COMPARISON\r\n\r\nBEFORE\r\n"+(Before==null?"Not captured":Before.Describe())+"\r\n\r\nAFTER\r\n"+(After==null?"Not captured":After.Describe()));if(Before!=null&&After!=null){b.AppendLine("\r\n\r\nAFTER MINUS BEFORE");b.AppendLine("Average CPU: "+(After.Readings.Average(x=>x.Cpu)-Before.Readings.Average(x=>x.Cpu)).ToString("+0.0;-0.0;0.0")+" percentage points");b.AppendLine("Average RAM: "+(After.Readings.Average(x=>x.Ram)-Before.Readings.Average(x=>x.Ram)).ToString("+0.0;-0.0;0.0")+" percentage points");}b.AppendLine("\r\nCompare similar workloads. Differences do not prove that a change caused an improvement. Aster does not modify Windows during capture.");return b.ToString();}
}
partial class MainWindow {
 readonly ComparisonCapture comparisonCapture=new ComparisonCapture();readonly List<string> sessionTimeline=new List<string>();
 ListBox timelineList;TextBox comparisonText;Label comparisonState,maintenanceState;bool? lastPaused;
 void AddSessionEvent(string message){sessionTimeline.Insert(0,DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+" · "+message);if(sessionTimeline.Count>200)sessionTimeline.RemoveAt(200);if(timelineList!=null&&!timelineList.IsDisposed){timelineList.Items.Clear();timelineList.Items.AddRange(sessionTimeline.Cast<object>().ToArray());}}
 void TickSessionTools(){if(lastPaused!=paused){lastPaused=paused;AddSessionEvent(paused?"Monitoring paused":"Monitoring resumed");}if(paused&&comparisonCapture.Active){comparisonCapture.Cancel("Capture canceled because monitoring was paused.");AddSessionEvent("Performance capture canceled: monitoring paused");RefreshComparison();}if(maintenanceState!=null&&!maintenanceState.IsDisposed)maintenanceState.Text=MaintenanceSummary();}
 void TrackSessionTools(Snapshot s){if(paused&&comparisonCapture.Active)comparisonCapture.Cancel("Capture canceled because monitoring was paused.");else if(comparisonCapture.Add(s,Math.Max(10,timer.Interval/1000.0*3)))AddSessionEvent(comparisonCapture.State);RefreshComparison();}
 string MaintenanceSummary(){int due=preferences.Maintenance.Count(x=>!x.Done&&x.Due.Date<=DateTime.Today);return due==0?"No maintenance reminders due today.":due+" maintenance reminder(s) due · open Maintenance planner";}
 void RefreshComparison(){if(comparisonText!=null&&!comparisonText.IsDisposed)comparisonText.Text=comparisonCapture.Report();if(comparisonState!=null&&!comparisonState.IsDisposed)comparisonState.Text=comparisonCapture.State;}
 void BuildPerformanceCompare(){
  sub.Text="Compare two 20-sample windows · local measurements · no automatic tuning";
  var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=4,Padding=new Padding(16)};foreach(int h in new[]{80,50,35})root.RowStyles.Add(new RowStyle(SizeType.Absolute,h));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));content.Controls.Add(root);
  root.Controls.Add(new Label{Dock=DockStyle.Fill,Text="1. Capture Before during your usual workload.\r\n2. Make one change yourself, then capture After under a similar workload.\r\nCaptures use the selected refresh interval and can continue while you visit another page."},0,0);
  var actions=new FlowLayoutPanel{Dock=DockStyle.Fill};root.Controls.Add(actions,0,1);
  foreach(bool before in new[]{true,false}){bool which=before;var button=Button(before?"Capture Before":"Capture After");button.Click+=(s,e)=>{try{if(paused)throw new InvalidOperationException("Resume monitoring before starting a capture.");comparisonCapture.Start(which);AddSessionEvent("Started "+(which?"Before":"After")+" performance capture");RefreshComparison();}catch(Exception ex){MessageBox.Show(this,ex.Message);}};actions.Controls.Add(button);}
  var cancel=Button("Cancel capture");cancel.Click+=(s,e)=>{if(comparisonCapture.Active){comparisonCapture.Cancel("Capture canceled. Completed windows kept.");AddSessionEvent("Performance capture canceled");RefreshComparison();}};actions.Controls.Add(cancel);
  var export=Button("Export comparison");export.Click+=(s,e)=>ExportPersonalText("performance-comparison",comparisonCapture.Report());actions.Controls.Add(export);
  comparisonState=new Label{Dock=DockStyle.Fill};root.Controls.Add(comparisonState,0,2);comparisonText=new TextBox{Dock=DockStyle.Fill,ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical};root.Controls.Add(comparisonText,0,3);RefreshComparison();
 }
 void BuildSessionTimeline(){
  sub.Text="This Aster session · latest 200 events · stored in memory until you export";
  var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(16)};root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));content.Controls.Add(root);
  var row=new FlowLayoutPanel{Dock=DockStyle.Fill};var note=new TextBox{Width=410,MaxLength=120,AccessibleName="Timeline marker"};row.Controls.Add(note);var add=Button("Add marker");add.Click+=(s,e)=>{if(note.Text.Trim().Length>0){AddSessionEvent("Marker: "+note.Text.Trim());note.Clear();}};row.Controls.Add(add);root.Controls.Add(row,0,0);
  var actions=new FlowLayoutPanel{Dock=DockStyle.Fill};var export=Button("Export timeline");export.Click+=(s,e)=>ExportPersonalText("session-timeline",string.Join(Environment.NewLine,sessionTimeline));actions.Controls.Add(export);var clear=Button("Clear timeline");clear.Click+=(s,e)=>{sessionTimeline.Clear();timelineList.Items.Clear();};actions.Controls.Add(clear);actions.Controls.Add(new Label{Text="Tracks page visits, monitoring, captures and performance alerts.",AutoSize=true,Margin=new Padding(12,8,0,0)});root.Controls.Add(actions,0,1);
  timelineList=new ListBox{Dock=DockStyle.Fill,HorizontalScrollbar=true};timelineList.Items.AddRange(sessionTimeline.Cast<object>().ToArray());root.Controls.Add(timelineList,0,2);
 }
 void BuildMaintenancePlanner(){
  sub.Text="Local reminders while Aster is open · you choose when to take action";
  var flow=FeatureFlow("A little upkeep. On your terms.","Plan a backup, update review or storage check. Reminders appear here and in Command Center; Aster never runs maintenance automatically.");
  maintenanceState=new Label{Text=MaintenanceSummary(),AutoSize=true,Margin=new Padding(0,0,0,14)};flow.Controls.Add(maintenanceState);
  var presets=new FlowLayoutPanel{Width=760,Height=42};flow.Controls.Add(presets);
  var title=new TextBox{Width=470,MaxLength=80,AccessibleName="Maintenance title"};flow.Controls.Add(new Label{Text="Reminder title",AutoSize=true});flow.Controls.Add(title);
  foreach(string text in new[]{"Review backups","Check Windows updates","Review storage space"}){string preset=text;var b=Button(text);b.Click+=(s,e)=>title.Text=preset;presets.Controls.Add(b);}
  var row=new FlowLayoutPanel{Width=760,Height=42};flow.Controls.Add(row);row.Controls.Add(new Label{Text="Due",Width=40,Margin=new Padding(0,7,0,0)});var date=new DateTimePicker{Format=DateTimePickerFormat.Short,Width=155};row.Controls.Add(date);row.Controls.Add(new Label{Text="Repeat days (0 = once)",Width=190,Margin=new Padding(12,7,0,0)});var repeat=new NumericUpDown{Minimum=0,Maximum=365,Width=75};row.Controls.Add(repeat);
  var list=new ListBox{Width=740,Height=220,HorizontalScrollbar=true};var actions=new FlowLayoutPanel{Width=760,Height=48};flow.Controls.Add(actions);flow.Controls.Add(list);
  Action render=()=>{list.Items.Clear();list.Items.AddRange(preferences.Maintenance.OrderBy(x=>x.Done).ThenBy(x=>x.Due).Cast<object>().ToArray());maintenanceState.Text=MaintenanceSummary();};
  Action<Action> persist=change=>{var backup=preferences.Maintenance.Select(x=>new MaintenanceItem{Title=x.Title,Due=x.Due,RepeatDays=x.RepeatDays,Done=x.Done}).ToList();try{change();preferences.Save();render();}catch(Exception ex){preferences.Maintenance=backup;render();MessageBox.Show(this,"Could not save reminder: "+ex.Message);}};
  var add=Button("Add reminder");add.Click+=(s,e)=>{if(title.Text.Trim().Length==0)return;if(preferences.Maintenance.Count>=24){MessageBox.Show(this,"Keep up to 24 reminders. Remove old reminders to add more.");return;}persist(()=>preferences.Maintenance.Add(new MaintenanceItem{Title=title.Text.Trim(),Due=date.Value.Date,RepeatDays=(int)repeat.Value}));};actions.Controls.Add(add);
  var done=Button("Mark done");done.Click+=(s,e)=>{var selected=list.SelectedItem as MaintenanceItem;if(selected==null)return;persist(()=>selected.Complete(DateTime.Today));};actions.Controls.Add(done);
  var snooze=Button("Snooze 1 day");snooze.Click+=(s,e)=>{var selected=list.SelectedItem as MaintenanceItem;if(selected==null)return;persist(()=>{selected.Due=DateTime.Today.AddDays(1);selected.Done=false;});};actions.Controls.Add(snooze);
  var remove=Button("Remove selected");remove.Click+=(s,e)=>{var selected=list.SelectedItem as MaintenanceItem;if(selected!=null)persist(()=>preferences.Maintenance.Remove(selected));};actions.Controls.Add(remove);
  var tools=new FlowLayoutPanel{Width=760,Height=48};flow.Controls.Add(tools);foreach(string name in new[]{"Backup center","Update Aster","Disk space","Recovery toolbox"}){string target=name;var b=Button(name+" →");b.Click+=(s,e)=>Navigate(target);tools.Controls.Add(b);}render();
 }
}
}
