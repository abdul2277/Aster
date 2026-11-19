using System;using System.Collections.Generic;using System.Drawing;using System.Linq;using System.Reflection;using System.Windows.Forms;using Aster;
static class T{static int fails,passes;static void Check(bool ok,string n){if(ok)passes++;else{fails++;Console.WriteLine("FAIL: "+n);}}
static void Click(Control c){typeof(Control).GetMethod("OnClick",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new object[]{EventArgs.Empty});}
static IEnumerable<Control> All(Control c){foreach(Control x in c.Controls){yield return x;foreach(var y in All(x))yield return y;}}
[STAThread]static int Main(){
 var pins=new HashSet<string>{"Summary","Windows advisor"};var recent=new List<string>{"Space finder","Tools hub","Services"};var opened=new List<string>();bool simpleState=false;int pinCalls=0;
 var names=new HashSet<string>(G.All.SelectMany(g=>g.Skip(1)));
 var form=new Form{Width=1000,Height=700,BackColor=Color.FromArgb(18,24,33)};
 ToolsHubView v=null;v=new ToolsHubView(G.All,n=>n=="Space finder"?"See which folders and files fill your disk":n=="Summary"?"Live CPU, memory and disk":"Open "+n.ToLowerInvariant(),pins,()=>recent,false,names.Contains,n=>opened.Add(n),n=>{pinCalls++;if(!pins.Remove(n))pins.Add(n);},b=>simpleState=b,Color.FromArgb(32,44,62),Color.FromArgb(204,215,229),Color.FromArgb(139,150,168),Color.FromArgb(94,196,214));
 form.Controls.Add(v);form.Show();Application.DoEvents();
 int all=v.TileCount;Check(all>=45,"all tiles: "+all);Check(v.SectionCount==5,"five sections");
 var quick=All(v).OfType<Button>().Where(b=>b.Text.Contains("  ")).Select(b=>b.Text.Trim()).ToList();Check(quick.Any(q=>q.EndsWith("Summary"))&&quick.Any(q=>q.EndsWith("Space finder"))&&!quick.Any(q=>q.Contains("Tools hub")),"quick access: pins and recents, no hub: "+string.Join("|",quick.ToArray()));
 Check(all>0&&All(v).OfType<Panel>().Count(p=>p.Width==176)==all,"one tile per tool");
 v.SetSimple(true);Application.DoEvents();int simple=v.TileCount;Check(simple<all&&simple>=25&&simpleState,"simple mode hides tools: "+simple+" of "+all+" and tells the app");
 Check(!All(v).OfType<Label>().Any(l=>l.Text=="Air Defense feed"||l.Text=="Services"),"advanced tools not shown in simple mode");
 v.SearchFor("disk");Application.DoEvents();Check(v.TileCount>=2&&All(v).OfType<Label>().Any(l=>l.Text=="Space finder"),"search by description: "+v.TileCount);
 v.SearchFor("zzzzqq");Application.DoEvents();Check(v.TileCount==0&&All(v).OfType<Label>().Any(l=>l.Text.StartsWith("No tools match")),"empty search message");
 v.SearchFor("");v.SetSimple(false);v.ChooseCategory("PRODUCTIVITY");Application.DoEvents();Check(v.SectionCount==1&&v.TileCount>=7,"category: "+v.TileCount);
 var tile=All(v).OfType<Label>().First(l=>l.Text=="Power timer");Click(tile);Check(opened.SequenceEqual(new[]{"Power timer"}),"clicking a tile opens it");
 var star=All(v).OfType<Label>().First(l=>l.Text=="☆");Click(star);Application.DoEvents();Check(pinCalls==1,"star toggles pin");
 var panel=All(v).OfType<Panel>().First(p=>p.Width==176);Check(panel.AccessibleName.Length>0&&panel.AccessibleRole==AccessibleRole.PushButton&&panel.TabStop,"tiles are accessible");
 Check(G.All.SelectMany(g=>g.Skip(1)).All(n=>HubModel.Icon(n)!="▫"),"every tool has an icon: "+string.Join(",",G.All.SelectMany(g=>g.Skip(1)).Where(n=>HubModel.Icon(n)=="▫").ToArray()));
 v.ChooseCategory("");Application.DoEvents();System.Diagnostics.Process.Start("import","-window root /tmp/claude-0/shots/aster-hub.png").WaitForExit();
 v.Dispose();form.Close();Console.WriteLine(passes+" passed, "+fails+" failed");return fails==0?0:1;}}
