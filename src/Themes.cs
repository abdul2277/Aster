using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
namespace Aster {
partial class ThemePalette {
 public string Id,Name,Description;public Color Background,Surface,Raised,Sidebar,Border,Accent,Hero;
 public Color Text=Color.FromArgb(231,238,248),Muted=Color.FromArgb(169,187,207);
 static Color C(int hex){return Color.FromArgb((hex>>16)&255,(hex>>8)&255,hex&255);}
 static ThemePalette Make(string id,string name,string description,int bg,int surface,int raised,int side,int border,int accent,int hero){return new ThemePalette{Id=id,Name=name,Description=description,Background=C(bg),Surface=C(surface),Raised=C(raised),Sidebar=C(side),Border=C(border),Accent=C(accent),Hero=C(hero)};}
 static readonly ThemePalette[] Core={
 Make("midnight","Midnight","Cool blue · clear and composed",0x101722,0x182435,0x203148,0x111d2b,0x34485f,0x66d7f3,0x203f61),
 Make("amethyst","Amethyst","Soft violet · a little cosmic",0x181422,0x241e35,0x322a48,0x20192d,0x514463,0xc5a5ff,0x493165),
 Make("lagoon","Lagoon","Deep teal · calm and refreshing",0x0c1c20,0x152b30,0x1c3b42,0x10252a,0x34575c,0x71e3ce,0x17545d),
 Make("rose","Rosewood","Warm rose · quietly expressive",0x23171c,0x34242d,0x48323d,0x2c1c24,0x624450,0xffb2c6,0x663746),
 Make("ember","Ember","Amber glow · warm after dark",0x201a15,0x302820,0x41362a,0x291f18,0x60503d,0xffcc87,0x62462a),
 Make("graphite","Graphite","Neutral charcoal · less distraction",0x17191c,0x25292e,0x32383e,0x1e2226,0x49515a,0xc4d5e4,0x3b4856)};
 public static readonly ThemePalette[] All=CreateCollection();public static ThemePalette Current=All[0];
 public static ThemePalette Find(string id){return All.FirstOrDefault(t=>t.Id==id)??All[0];}
}
class ThemeButton:Button {
 bool hover;
 public ThemeButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
 protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
 protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
 protected override void OnPaint(PaintEventArgs e){if(Width<3||Height<3)return;var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;var t=ThemePalette.Current;using(var path=PlayColors.Round(new RectangleF(1,1,Width-3,Height-3),8)){using(var b=new SolidBrush(hover?t.Raised:BackColor))g.FillPath(b,path);if(hover||Focused||TextAlign!=ContentAlignment.MiddleLeft||BackColor!=t.Sidebar)using(var p=new Pen(Focused?t.Accent:t.Border,1))g.DrawPath(p,path);}TextFormatFlags flags=TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix;if(TextAlign==ContentAlignment.MiddleLeft)flags|=TextFormatFlags.Left;else flags|=TextFormatFlags.HorizontalCenter;TextRenderer.DrawText(g,Text,Font,new Rectangle(10,1,Math.Max(1,Width-20),Height-2),Enabled?ForeColor:t.Muted,flags);}
}
class ThemeCard:Button {
 public ThemePalette Palette;public bool Selected;
 public ThemeCard(){Size=new Size(300,206);Margin=new Padding(0,0,18,18);FlatStyle=FlatStyle.Flat;AccessibleRole=AccessibleRole.PushButton;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;var t=Palette;using(var path=PlayColors.Round(new RectangleF(1,1,Width-3,Height-3),14)){using(var b=new SolidBrush(t.Surface))g.FillPath(b,path);using(var p=new Pen(Selected||Focused?t.Accent:t.Border,Selected?2:1))g.DrawPath(p,path);}using(var b=new SolidBrush(t.Sidebar))g.FillRectangle(b,17,17,35,88);using(var b=new LinearGradientBrush(new Rectangle(60,17,Width-80,36),t.Hero,t.Raised,0f))g.FillRectangle(b,60,17,Width-80,36);for(int i=0;i<3;i++){using(var b=new SolidBrush(i==0?t.Accent:t.Raised))g.FillRectangle(b,23,28+i*22,23,5);using(var b=new SolidBrush(t.Raised))g.FillRectangle(b,60+i*72,63,64,40);}using(var f=new Font("Segoe UI",15,FontStyle.Bold))TextRenderer.DrawText(g,t.Name+(Selected?"  ✓":""),f,new Rectangle(17,118,Width-30,32),t.Text,TextFormatFlags.NoPrefix);using(var f=new Font("Segoe UI",9))TextRenderer.DrawText(g,t.Description,f,new Rectangle(17,154,Width-30,24),t.Muted,TextFormatFlags.NoPrefix|TextFormatFlags.EndEllipsis);using(var f=new Font("Segoe UI",8))TextRenderer.DrawText(g,!Enabled?"PREVIEW ONLY":Selected?"ACTIVE THEME":"CLICK TO APPLY",f,new Rectangle(17,181,Width-30,18),t.Accent,TextFormatFlags.NoPrefix);}
}
partial class MainWindow {
 void BuildThemes(){BuildThemeGallery();}
 void ApplyAppTheme(){var t=ResolveTheme();ThemePalette.Current=t;bg=t.Background;panel=t.Surface;muted=t.Muted;cyan=t.Accent;BackColor=bg;ForeColor=t.Text;ApplyThemeTree(this,t);sidebar.BackColor=t.Sidebar;foreach(Control c in sidebar.Controls)SetSidebar(c,t);foreach(var pair in buttons){pair.Value.BackColor=pair.Key==page?t.Hero:t.Sidebar;pair.Value.ForeColor=pair.Key==page?t.Accent:t.Text;pair.Value.Invalidate();}heading.ForeColor=t.Text;sub.ForeColor=status.ForeColor=t.Muted;if(greetingTitle!=null&&!greetingTitle.IsDisposed){greetingTitle.ForeColor=t.Text;greetingDate.ForeColor=t.Muted;greetingHijri.ForeColor=greetingOccasion.ForeColor=t.Accent;}if(summaryCpu!=null&&!summaryCpu.IsDisposed)summaryCpu.Accent=t.Accent;if(tileCpu!=null&&!tileCpu.IsDisposed)tileCpu.Accent=t.Accent;if(cpu!=null&&!cpu.IsDisposed)cpu.Accent=t.Accent;Invalidate(true);}
 void SetSidebar(Control c,ThemePalette t){if(!(c is TextBox)&&!(c is Button))c.BackColor=t.Sidebar;if(c is Label)c.ForeColor=t.Muted;foreach(Control child in c.Controls)SetSidebar(child,t);}
 void ApplyThemeTree(Control parent,ThemePalette t){foreach(Control c in parent.Controls){if(c is ThemeCard||c is OrbitCanvas)continue;bool transparent=c.BackColor==Color.Transparent;bool cardLabel=c is Label&&c.Dock==DockStyle.Fill&&c.Padding.All>0;bool surface=c is TextBoxBase||c is ComboBox||c is ListBox||c is NumericUpDown||c is Graph||cardLabel; if(!transparent)c.BackColor=surface?t.Surface:t.Background;c.ForeColor=t.Text;
 var button=c as Button;if(button!=null){button.BackColor=t.Raised;button.ForeColor=button.Name=="End"?Color.FromArgb(255,161,161):t.Text;}
 var grid=c as DataGridView;if(grid!=null){grid.EnableHeadersVisualStyles=false;grid.BackgroundColor=t.Surface;grid.GridColor=t.Border;grid.DefaultCellStyle.BackColor=t.Surface;grid.RowsDefaultCellStyle.BackColor=t.Surface;grid.DefaultCellStyle.ForeColor=t.Text;grid.DefaultCellStyle.SelectionBackColor=t.Hero;grid.DefaultCellStyle.SelectionForeColor=t.Text;grid.AlternatingRowsDefaultCellStyle.BackColor=t.Raised;grid.ColumnHeadersDefaultCellStyle.BackColor=t.Raised;grid.ColumnHeadersDefaultCellStyle.ForeColor=t.Muted;}
 var tab=c as TabControl;if(tab!=null){tab.DrawMode=TabDrawMode.OwnerDrawFixed;tab.Padding=new Point(16,9);tab.DrawItem-=PaintThemeTab;tab.DrawItem+=PaintThemeTab;}
 ApplyThemeTree(c,t);c.Invalidate();}}
 void PaintThemeTab(object sender,DrawItemEventArgs e){var tab=(TabControl)sender;var t=ThemePalette.Current;bool selected=e.Index==tab.SelectedIndex;using(var b=new SolidBrush(selected?t.Hero:t.Surface))e.Graphics.FillRectangle(b,e.Bounds);TextRenderer.DrawText(e.Graphics,tab.TabPages[e.Index].Text,Font,e.Bounds,selected?t.Accent:t.Text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix);}
}
}



