using System;using System.Drawing;using System.Windows.Forms;
namespace Aster {partial class MainWindow {
 AsterAlerts.AlertPanel airDefensePanel;
 TabControl airDefenseTabs;
 EmbeddedVideo airDefenseVideo;
 void StopAirDefenseVideo(){if(airDefenseVideo!=null){airDefenseVideo.Dispose();airDefenseVideo=null;}}
 void DetachAirDefense(){
  StopAirDefenseVideo();
  // Preserve the monitor across navigation, but stop video playback.
  if(airDefensePanel!=null&&airDefensePanel.Parent!=null)airDefensePanel.Parent.Controls.Remove(airDefensePanel);
  airDefenseTabs=null;
 }
 void BuildAirDefense(){
  sub.Text="Nearby public advisories and your embedded YouTube player";
  if(airDefensePanel==null){
   airDefensePanel=new AsterAlerts.AlertPanel{Dock=DockStyle.Fill};
   airDefensePanel.WatchVideoRequested+=(s,e)=>{if(airDefenseTabs!=null&&!airDefenseTabs.IsDisposed)airDefenseTabs.SelectedIndex=1;};
   FormClosing+=(s,e)=>{if(!e.Cancel&&airDefensePanel.IsMonitoring&&MessageBox.Show(this,"Close Aster and stop the alert monitor?","Stop monitoring",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)e.Cancel=true;};
   FormClosed+=(s,e)=>{StopAirDefenseVideo();airDefensePanel.Dispose();};
  }
  airDefenseTabs=new TabControl{Dock=DockStyle.Fill,AccessibleName="Air Defense feed sections",Padding=new Point(18,9)};
  var feedTab=new TabPage("Alerts & settings");var videoTab=new TabPage("YouTube player");
  feedTab.Controls.Add(airDefensePanel);airDefenseTabs.TabPages.Add(feedTab);airDefenseTabs.TabPages.Add(videoTab);content.Controls.Add(airDefenseTabs);
  airDefenseTabs.SelectedIndexChanged+=(s,e)=>{
   if(airDefenseTabs==null||airDefenseTabs.IsDisposed||airDefenseTabs.Disposing)return;
   StopAirDefenseVideo();
   if(airDefenseTabs.SelectedIndex!=1)return;
   while(videoTab.Controls.Count>0)videoTab.Controls[0].Dispose();
   var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2};
   layout.RowStyles.Add(new RowStyle(SizeType.Absolute,58));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));videoTab.Controls.Add(layout);
   layout.Controls.Add(new Label{Text="Your linked video plays here. Started alert monitoring continues while you watch.\r\nVideo is not processed for proximity alerts. YouTube may show ads or restrict playback.",Dock=DockStyle.Fill,Padding=new Padding(12,8,12,0)},0,0);
   airDefenseVideo=new EmbeddedVideo(()=>{if(airDefenseTabs!=null)airDefenseTabs.SelectedIndex=0;});layout.Controls.Add(airDefenseVideo,0,1);ApplyAppTheme();
  };
 }
}}
