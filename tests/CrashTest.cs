using System;using System.IO;using System.Linq;using Aster;
namespace Aster{static class AsterUpdate{public const string CurrentVersion="2.7";}}
static class T{static int f,p;static void C(bool ok,string n){if(ok)p++;else{f++;Console.WriteLine("FAIL "+n);}}
static int Main(){var dir=Path.Combine(Path.GetTempPath(),"aster-cl-"+Guid.NewGuid().ToString("N"));CrashLog.Folder=dir;CrashLog.Page="Space finder";
 Exception ex=null;try{throw new IOException("cannot read C:\\Users\\"+Environment.UserName+"\\Documents\\a.txt");}catch(Exception e){ex=e;}
 string file=CrashLog.Save(ex,"2.7");C(file!=null&&File.Exists(file)&&Path.GetFileName(file).StartsWith("crash-"),"saved");var t=File.ReadAllText(file);
 C(t.Contains("Version: 2.7")&&t.Contains("Page: Space finder")&&t.Contains("IOException")&&t.Contains("C:\\Users\\<user>\\Documents"),"content: "+t.Split('\n')[0]);
 for(int i=0;i<25;i++){System.Threading.Thread.Sleep(2);CrashLog.Save(ex,"2.7");}C(Directory.GetFiles(dir,"crash-*.txt").Length<=20,"kept at most 20: "+Directory.GetFiles(dir).Length);
 var blocker=Path.Combine(dir,"afile");File.WriteAllText(blocker,"x");CrashLog.Folder=Path.Combine(blocker,"sub"); // a folder cannot be created under a file
 C(CrashLog.Save(ex,"2.7")==null,"a failing save returns null and never throws");
 Console.WriteLine(p+" passed, "+f+" failed");return f==0?0:1;}}
