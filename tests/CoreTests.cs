using System;using System.IO;using System.Linq;using System.Threading;using Aster;
static class T{static int fails,passes;
static void Check(bool ok,string name){if(ok)passes++;else{fails++;Console.WriteLine("FAIL: "+name);}}
static int Main(){
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
 Console.WriteLine(passes+" passed, "+fails+" failed");return fails==0?0:1;}}
