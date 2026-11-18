using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Reflection;using System.Windows.Forms;using Aster;
static class T{static int fails,passes;static void Check(bool ok,string n){if(ok)passes++;else{fails++;Console.WriteLine("FAIL: "+n);}}
static void Click(Control c){typeof(Control).GetMethod("OnClick",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{EventArgs.Empty});}
static IEnumerable<Control> All(Control c){foreach(Control x in c.Controls){yield return x;foreach(var y in All(x))yield return y;}}
[STAThread]static int Main(){
 ArtImage.Override=Image.FromFile("/tmp/claude-0/x/AsterSuite-v1.4/Aster-artwork.png");
 var opened=new List<string>();var recent=new List<string>{"Notebook","Space finder","Tools hub","Checklist"};var names=new HashSet<string>(HomeModel.QuickTools.Concat(new[]{"Notebook","Checklist","Processes","Focus timer","Maintenance planner","Session timeline"}));
 var form=new Form{Width=980,Height=700,BackColor=Color.FromArgb(18,24,33)};
 var v=new HomeView(Color.FromArgb(32,44,62),Color.FromArgb(204,215,229),Color.FromArgb(139,150,168),Color.FromArgb(94,196,214),"Abdul",()=>recent,names.Contains,n=>opened.Add(n),null);form.Controls.Add(v);form.Show();Application.DoEvents();
 Check(v.Picture!=null&&v.Picture.Image!=null,"picture shown on Home");Check(v.QuickCount==6,"six quick tools: "+v.QuickCount);Check(v.RecentCount==2,"recent excludes quick tools and the hub: "+v.RecentCount);
 v.SetReadings(20,40,60);Check(v.LastLevel==HomeLevel.Good&&v.VerdictText=="Everything looks healthy.","good");v.SetReadings(95,90,5);Check(v.LastLevel==HomeLevel.Attention&&v.VerdictText.EndsWith("Worth a look."),"attention");v.SetReadings(10,10,-1);Check(v.LastLevel==HomeLevel.Good,"unknown disk is fine");
 var t=All(v).OfType<Label>().First(l=>l.Text=="Ping monitor");Click(t);Check(opened.SequenceEqual(new[]{"Ping monitor"}),"quick tile opens tool");
 v.SetReadings(75,85,12);Application.DoEvents();System.Threading.Thread.Sleep(400);Application.DoEvents();var rt=All(v).OfType<Panel>().Where(p=>p.Width==176).ToList();Console.WriteLine("tile heights: "+string.Join(",",rt.Select(p=>p.Height+"/"+p.Visible).ToArray()));Check(rt.All(p=>p.Height==62&&p.Visible),"all tiles full height");System.Diagnostics.Process.Start("import","-window root /tmp/claude-0/shots/aster-home.png").WaitForExit();
 var fake=new DateTime(2026,3,10,9,0,0);var v2=new HomeView(Color.FromArgb(32,44,62),Color.White,Color.Gray,Color.Cyan,"Abdul",()=>recent,names.Contains,n=>opened.Add(n),null,()=>fake);
 Check(v2.GreetingText.Contains("Good morning, Abdul")&&v2.GreetingText.StartsWith("☀"),"morning greeting: "+v2.GreetingText);Check(v2.SuggestionVisible&&HomeModel.QuickTools.Concat(new[]{"Notebook","Checklist"}).Contains(v2.SuggestionTool),"morning suggestion: "+v2.SuggestionTool);
 fake=new DateTime(2026,3,10,20,30,0);v2.SetReadings(10,10,60);Check(v2.GreetingText.Contains("Good evening")&&v2.SuggestionTool=="Power timer"&&v2.SuggestionText.Contains("Wind down"),"evening changes greeting and idea live: "+v2.SuggestionTool);
 fake=new DateTime(2026,3,10,23,15,0);v2.SetReadings(10,10,60);Check(v2.GreetingText.Contains("Good night")&&v2.GreetingText.StartsWith("🌙"),"night");
 fake=new DateTime(2026,3,10,10,0,0);v2.SetReadings(10,10,60);string calm=v2.SuggestionTool;v2.SetReadings(88,50,60);Check(v2.SuggestionTool=="Processes"&&calm!="Processes","a busy PC changes the suggestion at once");v2.SetReadings(10,10,60);Check(v2.SuggestionTool==calm,"and back when calm");
 var hidden=new HomeView(Color.Black,Color.White,Color.Gray,Color.Cyan,"",()=>recent,n=>false,n=>{},null,()=>fake);Check(!hidden.SuggestionVisible,"no suggestion if the tool does not exist");
 Click(All(v2).OfType<Button>().First(b=>b.Text.StartsWith("Open ")));Check(opened.Last()==v2.SuggestionTool,"suggestion button opens the tool");
 var tour=new WelcomeTour(Color.FromArgb(18,24,33),Color.FromArgb(204,215,229),Color.FromArgb(139,150,168),Color.FromArgb(94,196,214));tour.Show();Application.DoEvents();
 Check(tour.StepCount==5&&tour.Index==0&&tour.ShowsPicture,"tour starts with the picture");System.Diagnostics.Process.Start("import","-window root /tmp/claude-0/shots/aster-tour.png").WaitForExit();
 tour.Advance();Check(tour.Index==1&&!tour.ShowsPicture,"next step");tour.GoBack();Check(tour.Index==0,"back");for(int i=0;i<4;i++)tour.Advance();Check(tour.Index==4&&!tour.Finished,"last step reached");tour.Advance();Check(tour.Finished&&!tour.Visible,"finish closes and marks finished");
 var t2=new WelcomeTour(Color.Black,Color.White,Color.Gray,Color.Cyan);t2.Show();t2.SkipAll();Check(!t2.Finished&&!t2.Visible,"skip closes without finishing");
 ArtImage.Override=null;Console.WriteLine(passes+" passed, "+fails+" failed");return fails==0?0:1;}}
