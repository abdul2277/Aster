using System;using System.Collections.Generic;using System.IO;using System.Linq;using System.Threading;using Aster;
static class T{static int fails,passes;
static void Check(bool ok,string name){if(ok)passes++;else{fails++;Console.WriteLine("FAIL: "+name);}}
static int Main(){
 // ---- 2.5 additions
 Check(SpaceScanner.CsvCell("=cmd|' /C calc'!A0")=="'=cmd|' /C calc'!A0","formula guard");Check(SpaceScanner.CsvCell("a,b")=="\"a,b\""&&SpaceScanner.CsvCell("say \"hi\"")=="\"say \"\"hi\"\"\""&&SpaceScanner.CsvCell(null)==""&&SpaceScanner.CsvCell("+1")=="'+1","csv escaping");
 // SpaceScanner
 string root=Path.Combine(Path.GetTempPath(),"aster-space-"+Guid.NewGuid().ToString("N"));
 Directory.CreateDirectory(Path.Combine(root,"big","deep"));Directory.CreateDirectory(Path.Combine(root,"small"));
 File.WriteAllBytes(Path.Combine(root,"big","deep","video.mp4"),new byte[300000]);
 File.WriteAllBytes(Path.Combine(root,"big","a.zip"),new byte[100000]);
 File.WriteAllBytes(Path.Combine(root,"small","note.txt"),new byte[1000]);
 File.WriteAllBytes(Path.Combine(root,"root.bin"),new byte[50]);
 // symlink loop must not be followed
 try{System.Diagnostics.Process.Start("ln","-s "+root+" "+Path.Combine(root,"small","loop")).WaitForExit();}catch{}
 for(int i=0;i<150;i++)File.WriteAllBytes(Path.Combine(root,"small","f"+i+".dat"),new byte[i]);
 var r=SpaceScanner.Scan(root,CancellationToken.None);
 long expected=300000+100000+1000+50+Enumerable.Range(0,150).Sum();
 Check(r.TotalBytes==expected,"total bytes "+r.TotalBytes+" vs "+expected);
 Check(r.Files==154,"file count "+r.Files);
 Check(r.LargestFiles.Count==SpaceScanner.MaxLargest,"largest capped at 100: "+r.LargestFiles.Count);
 Check(r.LargestFiles[0].Path.EndsWith("video.mp4"),"largest first");
 Check(r.LargestFiles.Zip(r.LargestFiles.Skip(1),(a,b)=>a.Bytes>=b.Bytes).All(x=>x),"sorted desc");
 Check(r.TopFolders.Count==2&&r.TopFolders[0].Path.EndsWith("big")&&r.TopFolders[0].Bytes==400000&&r.TopFolders[0].Files==2,"top folder rollup");
 Check(r.ByExtension[".mp4"]==300000&&r.ByExtension[".dat"]==Enumerable.Range(0,150).Sum(),"by extension");
 var cts=new CancellationTokenSource();cts.Cancel();Check(SpaceScanner.Scan(root,cts.Token).Canceled,"cancel honored");
 try{SpaceScanner.Scan(Path.Combine(root,"nope"),CancellationToken.None);Check(false,"missing folder throws");}catch(DirectoryNotFoundException){passes++;}
 Directory.Delete(root,true);
 Check(SpaceScanner.Bytes(0)=="0 B"&&SpaceScanner.Bytes(1536)=="1.5 KB"&&SpaceScanner.Bytes(5L*1024*1024*1024)=="5.0 GB"&&SpaceScanner.Bytes(-1)=="Unavailable","bytes format");
 Check(SpaceScanner.Share(1,4)=="25.0%"&&SpaceScanner.Share(1,0)=="0%","share");
 // PingStats
 var p=new PingStats(4);Check(p.Verdict.StartsWith("Waiting"),"verdict empty");
 foreach(long? v in new long?[]{10,20,null,30,40,50})p.Add(v);
 Check(p.Sent==6&&p.Received==5,"sent/received");
 Check(Math.Abs(p.LossPercent-100.0/6)<1e-9,"loss");
 Check(p.Recent.SequenceEqual(new long[]{20,30,40,50}),"window trims to 4");
 Check(p.Min==20&&p.Max==50&&p.Average==35&&p.Jitter==10,"min/max/avg/jitter");
 Check(p.Verdict.StartsWith("Unstable"),"verdict loss: "+p.Verdict);
 var good=new PingStats();for(int i=0;i<30;i++)good.Add(12);Check(good.Verdict.StartsWith("Excellent")&&good.Jitter==0,"verdict good");
 var dead=new PingStats();dead.Add(null);Check(dead.Verdict.StartsWith("No replies"),"verdict dead");
 good.Reset();Check(good.Sent==0&&good.Recent.Count==0&&good.Last==null,"reset");
 // PowerPlan
 var now=new DateTime(2026,11,15,22,30,0);
 Check(PowerPlan.NextAt(23,0,now)==new DateTime(2026,11,15,23,0,0),"next at later today");
 Check(PowerPlan.NextAt(7,0,now)==new DateTime(2026,11,16,7,0,0),"next at tomorrow");
 Check(PowerPlan.NextAt(22,30,now)==new DateTime(2026,11,16,22,30,0),"same minute -> tomorrow");
 Check(PowerPlan.SecondsUntil(now.AddMinutes(30),now)==1800&&PowerPlan.SecondsUntil(now.AddMinutes(-1),now)==0,"seconds until");
 Check(PowerPlan.Arguments(PowerAction.Shutdown,1800).StartsWith("/s /t 1800 /c ")&&PowerPlan.Arguments(PowerAction.Restart,0).StartsWith("/r /t 0 "),"shutdown args");
 try{PowerPlan.Arguments(PowerAction.Shutdown,-1);Check(false,"negative rejected");}catch(ArgumentOutOfRangeException){passes++;}
 Check(PowerPlan.Countdown(TimeSpan.FromSeconds(65))=="01:05"&&PowerPlan.Countdown(TimeSpan.FromSeconds(3725))=="1:02:05"&&PowerPlan.Countdown(TimeSpan.FromSeconds(-3))=="00:00","countdown");
 // ---- 2.5: CSV, summary, ping report, recurring plans
 var rep=new SpaceReport{Root="C:\\Users",TotalBytes=1000,Files=2};rep.TopFolders.Add(new SpaceItem{Path="C:\\Users\\a,b",Bytes=600,Files=1});rep.TopFolders.Add(new SpaceItem{Path="C:\\Users\\c",Bytes=400,Files=1});
 rep.LargestFiles.Add(new SpaceItem{Path="=HYPERLINK(\"http://x\")",Bytes=600,Files=1,Modified=new DateTime(2026,1,2)});
 var csv=SpaceScanner.ToCsv(rep).Split(new[]{"\r\n"},StringSplitOptions.RemoveEmptyEntries);
 Check(csv.Length==4&&csv[0].StartsWith("Kind,Size"),"csv rows");Check(csv[1]=="Folder,600,60.00,1,,\"C:\\Users\\a,b\"","csv folder row: "+csv[1]);Check(csv[3].Contains("'=HYPERLINK(\"\"http://x\"\")"),"csv formula neutralised: "+csv[3]);Check(csv[3].Contains(",2026-01-02,"),"csv date");
 Check(SpaceScanner.ToCsv(new SpaceReport{Root="x"}).Split(new[]{"\r\n"},StringSplitOptions.RemoveEmptyEntries).Length==1,"empty csv has header only");
 var sum=SpaceScanner.Summary(rep,1);Check(sum.Contains("1000 B in 2 files")&&sum.Contains("a,b")&&!sum.Contains("C:\\Users\\c"),"summary respects top N");
 var ps=new PingStats(5);foreach(long? v in new long?[]{20,30,null,40})ps.Add(v);var pr=PingReport.Text("1.1.1.1",ps,new DateTime(2026,5,1,12,0,0),new DateTime(2026,5,1,11,58,30));
 Check(pr.Contains("Host: 1.1.1.1")&&pr.Contains("Sent: 4   Received: 3   Loss: 25.0%")&&pr.Contains("avg 30.0  min 20  max 40")&&pr.Contains("Monitored for: 00:01:30")&&pr.Contains("20 30 40"),"ping report text");
 Check(PingReport.Text("h",new PingStats(),DateTime.Now).Contains("Waiting for the first reply"),"ping report with no data");
 Check(PowerPlan.DaysText(127)=="SUN,MON,TUE,WED,THU,FRI,SAT"&&PowerPlan.DaysText(2|4|8|16|32)=="MON,TUE,WED,THU,FRI"&&PowerPlan.DaysText(1)=="SUN","days text");
 foreach(int bad in new[]{0,-1,128})try{PowerPlan.DaysText(bad);Check(false,"bad days "+bad);}catch(ArgumentOutOfRangeException){passes++;}
 var a1=PowerPlan.CreateTaskArguments(PowerAction.Shutdown,23,5,127);Check(a1.StartsWith("/create /sc DAILY /st 23:05 /tn AsterPowerTimer /tr ")&&a1.Contains("shutdown.exe /s /t 60")&&a1.EndsWith(" /f"),"daily args: "+a1);
 var a2=PowerPlan.CreateTaskArguments(PowerAction.Restart,6,0,2|4|8|16|32);Check(a2.Contains("/sc WEEKLY /d MON,TUE,WED,THU,FRI /st 06:00")&&a2.Contains("shutdown.exe /r"),"weekly args: "+a2);
 Check(!a1.Contains("&")&&!a1.Contains("|")&&!a1.Contains(";"),"no shell metacharacters in the command");
 foreach(var bad in new[]{new[]{24,0},new[]{-1,0},new[]{0,60},new[]{0,-1}})try{PowerPlan.CreateTaskArguments(PowerAction.Shutdown,bad[0],bad[1],127);Check(false,"bad time");}catch(ArgumentOutOfRangeException){passes++;}
 Check(PowerPlan.DeleteTaskArguments()=="/delete /tn AsterPowerTimer /f"&&PowerPlan.QueryTaskArguments()=="/query /tn AsterPowerTimer /fo LIST","delete/query args");
 Check(PowerPlan.Describe(PowerAction.Shutdown,22,30,127)=="Shut down at 22:30 every day"&&PowerPlan.Describe(PowerAction.Restart,7,5,2|4)=="Restart at 07:05 on MON, TUE","describe");
 // ---- 2.6: Tools hub model
 var G=new List<string[]>{new[]{"MONITORING","Summary","Processes","Flight recorder"},new[]{"HARDWARE","CPU specs","Space finder","Services","Summary"},new[]{"EMPTY","Drivers"}};
 Func<string,string> H=n=>n=="Space finder"?"See which folders fill your disk":n=="Processes"?"Running programs and CPU":"";
 var all=HubModel.Sections(G,"","",false,H);Check(all.Count==3&&all[0].Value.Count==3&&all[1].Value.Count==3,"all sections; a tool listed twice shows once: "+all.Count);
 var simple=HubModel.Sections(G,"","",true,H);Check(simple.Count==2&&!simple.SelectMany(x=>x.Value).Any(HubModel.IsAdvanced)&&simple[0].Value.SequenceEqual(new[]{"Summary","Processes"}),"simple mode hides advanced tools and drops empty sections");
 Check(HubModel.Sections(G,"","HARDWARE",false,H).Count==1&&HubModel.Sections(G,"","nope",false,H).Count==0,"category filter");
 var q=HubModel.Sections(G,"disk",null,false,H);Check(q.Count==1&&q[0].Value[0]=="Space finder","hint search finds Space finder");
 var q2=HubModel.Sections(G,"proc",null,false,H);Check(q2.Count==1&&q2[0].Value[0]=="Processes","name search");
 Check(HubModel.Score("Space finder","","space")>HubModel.Score("Disk space","","space")&&HubModel.Score("x","y","zzz")==0&&HubModel.Score("a","b","  ")==1,"ranking: name start beats name contains");
 Check(HubModel.Sections(G,"folders disk",null,false,H)[0].Value[0]=="Space finder"&&HubModel.Sections(G,"folders zzz",null,false,H).Count==0,"every word must match");
 var qa=HubModel.QuickAccess(new[]{"Summary","Services","Ghost"},new[]{"Summary","Tools hub","Processes","CPU specs"},n=>n!="Ghost",true,4);Check(qa.SequenceEqual(new[]{"Summary","Processes","CPU specs"}),"quick access: pins then recent, deduped, no hub/ghost/advanced: "+string.Join(",",qa.ToArray()));
 Check(HubModel.QuickAccess(null,null,n=>true,false,3).Count==0&&HubModel.QuickAccess(new[]{"a","b","c","d"},null,n=>true,false,2).Count==2,"quick access limits and nulls");
 Check(HubModel.Icon("Summary")=="📊"&&HubModel.Icon("Unknown tool")=="▫"&&HubModel.Icon(null)=="▫","icons");
 // ---- 2.7: Home verdicts and crash reports
 Check(HomeModel.Level(20,40,50)==HomeLevel.Good&&HomeModel.Level(75,40,50)==HomeLevel.Busy&&HomeModel.Level(20,85,50)==HomeLevel.Busy&&HomeModel.Level(95,40,50)==HomeLevel.Attention&&HomeModel.Level(10,10,5)==HomeLevel.Attention&&HomeModel.Level(10,10,-1)==HomeLevel.Good,"home levels incl. unknown disk");
 Check(HomeModel.Verdict(10,20,60)=="Everything looks healthy."&&HomeModel.Verdict(80,85,60)=="The processor is working hard and memory is getting full."&&HomeModel.Verdict(95,50,5).EndsWith("Worth a look.")&&HomeModel.Verdict(95,90,5).Contains(", memory is getting full and the system drive"),"home sentences: "+HomeModel.Verdict(95,90,5));
 Check(HomeModel.Greeting(3)=="Working late"&&HomeModel.Greeting(9)=="Good morning"&&HomeModel.Greeting(14)=="Good afternoon"&&HomeModel.Greeting(22)=="Good evening"&&HomeModel.QuickTools.Length==6,"greeting and quick tools");
 Check(CrashReport.Redact("C:\\Users\\alice\\AppData\\x.dll at D:\\users\\Bob Smith\\f")=="C:\\Users\\<user>\\AppData\\x.dll at D:\\users\\<user> Smith\\f"||CrashReport.Redact("C:\\Users\\alice\\AppData\\x.dll").StartsWith("C:\\Users\\<user>\\AppData"),"redacts windows user paths: "+CrashReport.Redact("C:\\Users\\alice\\AppData\\x.dll"));
 Check(CrashReport.Redact("/home/carol/work/a.cs")=="/home/<user>/work/a.cs"&&CrashReport.Redact(null)==""&&CrashReport.Redact("nothing here")=="nothing here","redacts unix paths and nulls");
 Exception boom=null;try{try{throw new InvalidOperationException("inner C:\\Users\\dave\\secret.txt");}catch(Exception i){throw new ApplicationException("outer",i);}}catch(Exception e){boom=e;}
 var cr=CrashReport.Build(boom,"2.7",new DateTime(2026,5,1,9,30,0),"Windows 11","Space finder");
 Check(cr.Contains("Version: 2.7")&&cr.Contains("Page: Space finder")&&cr.Contains("ApplicationException: outer")&&cr.Contains("Caused by: System.InvalidOperationException")&&!cr.Contains("dave")&&cr.Contains("<user>")&&cr.Contains("2026-05-01 09:30:00"),"crash report content and redaction");
 var dir=Path.Combine(Path.GetTempPath(),"aster-crash-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);for(int i=0;i<30;i++)File.WriteAllText(Path.Combine(dir,"crash-2026010"+(i/10)+"-0000"+(i%10).ToString()+i.ToString("00")+".txt"),"x");File.WriteAllText(Path.Combine(dir,"keep.txt"),"x");CrashReport.Trim(dir,20);
 Check(Directory.GetFiles(dir,"crash-*.txt").Length==20&&File.Exists(Path.Combine(dir,"keep.txt")),"trim keeps newest 20 and unrelated files");Directory.Delete(dir,true);
 Console.WriteLine(passes+" passed, "+fails+" failed");return fails==0?0:1;}}
