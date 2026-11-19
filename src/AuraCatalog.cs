using System;using System.Collections.Generic;using System.Drawing;using System.Linq;
namespace Aura {
class WallpaperStyle {public string Name,Family;public Color Color,Backdrop;public double Tempo,Warp;public int Seed;}
static class WallpaperCatalog {
 public static readonly string[] Families={"Aurora","Starfield","Neon rain","Ocean waves","Orbit","Fireflies","Matrix","Sunset"};
 static readonly string[] PaletteNames={"Arctic","Lagoon","Jade","Emerald","Lime","Citron","Gold","Amber","Peach","Coral","Scarlet","Rose","Blossom","Fuchsia","Magenta","Orchid","Violet","Indigo","Cobalt","Azure","Glacier","Silver","Pearl","Copper","Sage"};
 static readonly string[] PaletteHex={"#68E0EF","#25BFAF","#22C78C","#50D075","#99DA48","#D2DC55","#F5CE62","#FFA83D","#FFB594","#FF8578","#ED5965","#ED729A","#F5A4D0","#F15ED3","#CC55E9","#BB87EE","#986EEB","#7A83E8","#608EEF","#52B2F5","#ADE5F5","#ACBED1","#E4D5C1","#C28B6A","#9FBF91"};
 static readonly string[] Motions={"Drift","Flow","Pulse","Swirl","Surge"};
 static readonly Dictionary<string,WallpaperStyle> Styles=Build();
 public static readonly string[] Names=Families.Concat(Styles.Keys).ToArray();
 static Dictionary<string,WallpaperStyle> Build(){var result=new Dictionary<string,WallpaperStyle>(StringComparer.Ordinal);int seed=0;foreach(string family in Families)for(int palette=0;palette<PaletteNames.Length;palette++)for(int motion=0;motion<Motions.Length;motion++){Color color=ColorTranslator.FromHtml(PaletteHex[palette]);string name=family+" · "+PaletteNames[palette]+" · "+Motions[motion];result.Add(name,new WallpaperStyle{Name=name,Family=family,Color=color,Backdrop=Color.FromArgb(8+color.R/10,10+color.G/9,17+color.B/7),Tempo=new[]{.35,.7,1.0,1.4,2.0}[motion],Warp=.65+motion*.22,Seed=++seed});}return result;}
 public static WallpaperStyle Find(string name){WallpaperStyle style;return name!=null&&Styles.TryGetValue(name,out style)?style:null;}
 public static bool Contains(string name){return name!=null&&(Styles.ContainsKey(name)||Families.Contains(name));}
 public static string Family(string name){var style=Find(name);return style==null?name:style.Family;}
 public static IEnumerable<string> Search(string category,string query){query=(query??"").Trim();return Names.Where(n=>(category=="All animated"||category==Family(n))&&(query.Length==0||n.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0));}
}
}
