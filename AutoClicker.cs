using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("AutoClicker by BOZYILDIZ")]
[assembly: AssemblyProduct("AutoClicker by BOZYILDIZ")]
[assembly: AssemblyCompany("BOZYILDIZ")]

namespace AutoClicker
{
    // =====================================================================
    //  Thème "macOS sombre"
    // =====================================================================
    static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(28, 28, 30);
        public static readonly Color Card = Color.FromArgb(44, 44, 46);
        public static readonly Color Field = Color.FromArgb(62, 62, 66);
        public static readonly Color FieldHi = Color.FromArgb(96, 96, 101);
        public static readonly Color Sep = Color.FromArgb(60, 60, 64);
        public static readonly Color Text = Color.FromArgb(245, 245, 247);
        public static readonly Color Sub = Color.FromArgb(152, 152, 157);
        public static readonly Color Accent = Color.FromArgb(10, 132, 255);
        public static readonly Color Green = Color.FromArgb(48, 209, 88);
        public static readonly Color Red = Color.FromArgb(255, 69, 58);
        public static readonly Color Menu = Color.FromArgb(40, 40, 42);

        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        public static Font F(float size) { return new Font("Segoe UI", size); }
        public static Font FS(float size) { return new Font("Segoe UI Semibold", size); }

        public static void Dwm(IntPtr h, int attr, int value)
        {
            try { DwmSetWindowAttribute(h, attr, ref value, 4); } catch { }
        }

        // Coins arrondis Windows 11 + bordure sombre
        public static void MacWindow(IntPtr h)
        {
            Dwm(h, 33, 2);            // DWMWA_WINDOW_CORNER_PREFERENCE = ROUND
            Dwm(h, 20, 1);            // DWMWA_USE_IMMERSIVE_DARK_MODE
            Dwm(h, 34, 0x00403C3C);   // DWMWA_BORDER_COLOR (BGR)
        }

        public static GraphicsPath Round(RectangleF r, float rad)
        {
            var p = new GraphicsPath();
            float d = Math.Min(rad * 2, Math.Min(r.Width, r.Height));
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, Color c, RectangleF r, float rad)
        {
            using (var p = Round(r, rad)) using (var b = new SolidBrush(c)) g.FillPath(b, p);
        }

        public static Color A(Color c, bool enabled) { return enabled ? c : Color.FromArgb(90, c); }

        public static Color Mix(Color a, Color b, float t)
        {
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        public static void Hq(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        }

        public static void DrawText(Graphics g, string s, Font f, Color c, RectangleF r, StringAlignment h)
        {
            using (var sf = new StringFormat { Alignment = h, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            using (var b = new SolidBrush(c))
                g.DrawString(s, f, b, r, sf);
        }
    }

    // =====================================================================
    //  Contrôles personnalisés
    // =====================================================================
    public class MacControl : Control
    {
        protected readonly System.Windows.Forms.Timer anim = new System.Windows.Forms.Timer { Interval = 15 };
        bool locked;

        public MacControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.StandardDoubleClick, false);
            Cursor = Cursors.Hand;
            anim.Tick += delegate { if (!Animate()) anim.Stop(); Invalidate(); };
        }

        // Verrouillé = grisé et inactif (sans le rendu "désactivé" moche de Windows)
        public bool Locked
        {
            get { return locked; }
            set
            {
                if (locked == value) return;
                locked = value;
                Cursor = value ? Cursors.Default : Cursors.Hand;
                OnLockedChanged();
                Invalidate();
            }
        }

        protected bool En { get { return Enabled && !locked; } }
        protected virtual void OnLockedChanged() { }
        protected virtual bool Animate() { return false; }

        protected static float Approach(float cur, float target)
        {
            float d = target - cur;
            return Math.Abs(d) < 0.01f ? target : cur + d * 0.25f;
        }

        protected override void OnPaintBackground(PaintEventArgs e) { e.Graphics.Clear(BackColor); }
    }

    // Carte arrondie façon Réglages macOS, avec séparateurs entre les lignes
    public class Card : Control
    {
        public readonly List<int> Separators = new List<int>();

        public Card()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
            ForeColor = Theme.Text;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Bg);
            Theme.Hq(g);
            var r = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (var p = Theme.Round(r, 12))
            {
                using (var b = new SolidBrush(BackColor)) g.FillPath(b, p);
                using (var pen = new Pen(Color.FromArgb(18, 255, 255, 255))) g.DrawPath(pen, p);
            }
            g.SmoothingMode = SmoothingMode.None;
            using (var pen = new Pen(Theme.Sep))
                foreach (int y in Separators) g.DrawLine(pen, 14, y, Width - 14, y);
        }
    }

    // Contrôle segmenté (onglets / choix exclusifs)
    public class Segmented : MacControl
    {
        readonly string[] items;
        readonly Font font = Theme.F(9.5f);
        int sel;
        float pos;
        public event EventHandler SelectedIndexChanged;

        public Segmented(params string[] items) { this.items = items; Height = 28; }

        public int SelectedIndex
        {
            get { return sel; }
            set
            {
                if (value == sel) return;
                sel = value;
                anim.Start();
                Invalidate();
                if (SelectedIndexChanged != null) SelectedIndexChanged(this, EventArgs.Empty);
            }
        }

        protected override bool Animate() { pos = Approach(pos, sel); return pos != sel; }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!En) return;
            int i = (int)(e.X / ((float)Width / items.Length));
            if (i >= 0 && i < items.Length) SelectedIndex = i;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Hq(g);
            float w = (float)Width / items.Length;
            Theme.FillRound(g, Theme.A(Theme.Field, En), new RectangleF(0, 0, Width - 1, Height - 1), 8);
            var pill = new RectangleF(pos * w + 2, 2, w - 4, Height - 5);
            Theme.FillRound(g, Color.FromArgb(En ? 50 : 20, 0, 0, 0), new RectangleF(pill.X, pill.Y + 1, pill.Width, pill.Height), 6);
            Theme.FillRound(g, Theme.A(Theme.FieldHi, En), pill, 6);
            for (int i = 0; i < items.Length; i++)
                Theme.DrawText(g, items[i], font, Theme.A(i == sel ? Theme.Text : Theme.Sub, En), new RectangleF(i * w, 0, w, Height), StringAlignment.Center);
        }
    }

    // Interrupteur style iOS
    public class Toggle : MacControl
    {
        bool on;
        float pos;
        public event EventHandler CheckedChanged;

        public Toggle(bool initial) { on = initial; pos = initial ? 1 : 0; Size = new Size(44, 26); }

        public bool Checked
        {
            get { return on; }
            set
            {
                if (on == value) return;
                on = value;
                anim.Start();
                if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
            }
        }

        protected override bool Animate() { float t = on ? 1 : 0; pos = Approach(pos, t); return pos != t; }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (En) Checked = !Checked;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Hq(g);
            Color track = Theme.Mix(Color.FromArgb(78, 78, 82), Theme.Green, pos);
            Theme.FillRound(g, Theme.A(track, En), new RectangleF(0, 0, Width - 1, Height - 1), Height / 2f);
            float d = Height - 5;
            float x = 2 + pos * (Width - 5 - d);
            using (var b = new SolidBrush(Color.FromArgb(60, 0, 0, 0))) g.FillEllipse(b, x, 3.5f, d, d);
            using (var b = new SolidBrush(Theme.A(Color.White, En))) g.FillEllipse(b, x, 2, d, d);
        }
    }

    // Menu déroulant façon "pop-up button" macOS
    public class MacSelect : MacControl
    {
        readonly List<string> items = new List<string>();
        readonly Font font = Theme.F(9.5f);
        int sel;
        bool hover;
        public event EventHandler SelectedIndexChanged;

        public MacSelect(params string[] items) { this.items.AddRange(items); Size = new Size(170, 28); }

        public int SelectedIndex
        {
            get { return sel; }
            set
            {
                if (value == sel) return;
                sel = value;
                Invalidate();
                if (SelectedIndexChanged != null) SelectedIndexChanged(this, EventArgs.Empty);
            }
        }

        public void SetItem(int i, string text) { items[i] = text; Invalidate(); }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (!En) return;
            var menu = new ContextMenuStrip
            {
                Renderer = new MacMenuRenderer(),
                ShowImageMargin = false,
                ShowCheckMargin = true,
                Font = font,
                BackColor = Theme.Menu,
                ForeColor = Theme.Text,
                Padding = new Padding(2, 4, 2, 4)
            };
            for (int i = 0; i < items.Count; i++)
            {
                int idx = i;
                var it = new ToolStripMenuItem(items[i]) { Checked = i == sel, ForeColor = Theme.Text, AutoSize = false };
                it.Size = new Size(Math.Max(Width, TextRenderer.MeasureText(items[i], font).Width + 50), 26);
                it.Click += delegate { SelectedIndex = idx; };
                menu.Items.Add(it);
            }
            menu.Opened += delegate { Theme.MacWindow(menu.Handle); };
            menu.Closed += delegate { BeginInvoke((MethodInvoker)delegate { menu.Dispose(); }); };
            menu.Show(this, new Point(0, Height + 4));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Hq(g);
            Color bg = hover && En ? Theme.Mix(Theme.Field, Color.White, 0.08f) : Theme.Field;
            Theme.FillRound(g, Theme.A(bg, En), new RectangleF(0, 0, Width - 1, Height - 1), 7);
            string s = sel >= 0 && sel < items.Count ? items[sel] : "";
            Theme.DrawText(g, s, font, Theme.A(Theme.Text, En), new RectangleF(10, 0, Width - 34, Height), StringAlignment.Near);

            // double chevron à droite, comme sur macOS
            float cx = Width - 15, cy = Height / 2f;
            using (var pen = new Pen(Theme.A(Theme.Sub, En), 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                g.DrawLines(pen, new[] { new PointF(cx - 3.5f, cy - 2), new PointF(cx, cy - 5.5f), new PointF(cx + 3.5f, cy - 2) });
                g.DrawLines(pen, new[] { new PointF(cx - 3.5f, cy + 2), new PointF(cx, cy + 5.5f), new PointF(cx + 3.5f, cy + 2) });
            }
        }
    }

    class MacMenuRenderer : ToolStripRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e) { e.Graphics.Clear(Theme.Menu); }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (var pen = new Pen(Color.FromArgb(70, 70, 74)))
                e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected) return;
            Theme.Hq(e.Graphics);
            Theme.FillRound(e.Graphics, Theme.Accent, new RectangleF(4, 1, e.Item.Width - 8, e.Item.Height - 2), 5);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Theme.Text;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            var g = e.Graphics;
            Theme.Hq(g);
            var r = e.ImageRectangle;
            float cx = r.X + r.Width / 2f, cy = r.Y + r.Height / 2f;
            using (var pen = new Pen(Theme.Text, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                g.DrawLines(pen, new[] { new PointF(cx - 4.5f, cy), new PointF(cx - 1.5f, cy + 3.5f), new PointF(cx + 4.5f, cy - 4) });
        }
    }

    // Champ numérique arrondi avec petites flèches
    public class MacNumber : MacControl
    {
        readonly TextBox box;
        readonly decimal min, max;
        decimal val;

        public MacNumber(decimal min, decimal max, decimal value)
        {
            this.min = min; this.max = max; val = value;
            Size = new Size(80, 28);
            box = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Theme.Field,
                ForeColor = Theme.Text,
                Font = Theme.F(10f),
                TextAlign = HorizontalAlignment.Center,
                Text = value.ToString()
            };
            box.KeyPress += (s, e) => { if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar)) e.Handled = true; };
            box.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Up) { Step(1); e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Down) { Step(-1); e.SuppressKeyPress = true; }
                else if (e.KeyCode == Keys.Enter) { Commit(); e.SuppressKeyPress = true; }
            };
            box.Leave += delegate { Commit(); };
            box.MouseWheel += (s, e) => { if (!Locked) Step(e.Delta > 0 ? 1 : -1); };
            Controls.Add(box);
        }

        public decimal Value
        {
            get { Commit(); return val; }
            set { val = Clamp(value); box.Text = val.ToString(); }
        }

        decimal Clamp(decimal v) { return Math.Max(min, Math.Min(max, v)); }

        void Commit()
        {
            decimal v;
            if (decimal.TryParse(box.Text, out v)) val = Clamp(v);
            box.Text = val.ToString();
        }

        void Step(int d) { Commit(); val = Clamp(val + d); box.Text = val.ToString(); }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (box != null) box.SetBounds(6, (Height - box.Height) / 2, Width - 26, box.Height);
        }

        protected override void OnLockedChanged()
        {
            box.ReadOnly = Locked;
            box.TabStop = !Locked;
            box.ForeColor = Locked ? Color.FromArgb(110, 110, 114) : Theme.Text;
            box.BackColor = Locked ? Color.FromArgb(50, 50, 53) : Theme.Field;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!En) return;
            if (e.X > Width - 20) Step(e.Y < Height / 2 ? 1 : -1);
            else box.Focus();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (En) Step(e.Delta > 0 ? 1 : -1);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Hq(g);
            Theme.FillRound(g, En ? Theme.Field : Color.FromArgb(50, 50, 53), new RectangleF(0, 0, Width - 1, Height - 1), 7);
            float cx = Width - 12, cy = Height / 2f;
            using (var pen = new Pen(Theme.A(Theme.Sub, En), 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                g.DrawLines(pen, new[] { new PointF(cx - 3, cy - 2.5f), new PointF(cx, cy - 5.5f), new PointF(cx + 3, cy - 2.5f) });
                g.DrawLines(pen, new[] { new PointF(cx - 3, cy + 2.5f), new PointF(cx, cy + 5.5f), new PointF(cx + 3, cy + 2.5f) });
            }
        }
    }

    // Gros bouton arrondi avec léger dégradé
    public class MacButton : MacControl
    {
        Color fill;
        bool hover, down;
        readonly Font font = Theme.FS(11f);

        public MacButton(string text, Color fill) { Text = text; this.fill = fill; Height = 44; }

        public Color Fill { get { return fill; } set { fill = value; Invalidate(); } }

        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; down = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); down = true; Invalidate(); }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); down = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Hq(g);
            Color c = down ? Theme.Mix(fill, Color.Black, 0.18f) : hover ? Theme.Mix(fill, Color.White, 0.10f) : fill;
            var r = new RectangleF(0, 0, Width - 1, Height - 1);
            using (var p = Theme.Round(r, 11))
            using (var b = new LinearGradientBrush(new RectangleF(0, -1, Width, Height + 2), Theme.Mix(c, Color.White, 0.12f), c, 90f))
            {
                g.FillPath(b, p);
                using (var pen = new Pen(Color.FromArgb(40, 255, 255, 255))) g.DrawPath(pen, p);
            }
            Theme.DrawText(g, Text, font, Color.White, r, StringAlignment.Center);
        }
    }

    // Les trois boutons rouge / jaune / vert de macOS
    public class TrafficLights : MacControl
    {
        bool hover;
        public bool Pinned = true;
        public event EventHandler CloseClick, MinClick, PinClick;

        static readonly Color[] Colors = { Color.FromArgb(255, 95, 87), Color.FromArgb(254, 188, 46), Color.FromArgb(40, 200, 64) };

        public TrafficLights() { Size = new Size(58, 14); Cursor = Cursors.Default; }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            int i = e.X / 20;
            EventHandler h = i == 0 ? CloseClick : i == 1 ? MinClick : PinClick;
            if (h != null) h(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Hq(g);
            for (int i = 0; i < 3; i++)
            {
                float x = i * 20 + 0.5f;
                using (var b = new SolidBrush(Colors[i])) g.FillEllipse(b, x, 0.5f, 12.5f, 12.5f);
                using (var pen = new Pen(Color.FromArgb(40, 0, 0, 0))) g.DrawEllipse(pen, x, 0.5f, 12.5f, 12.5f);
                if (!hover) continue;

                float cx = x + 6.25f, cy = 6.75f;
                using (var pen = new Pen(Color.FromArgb(150, 60, 0, 0), 1.3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    if (i == 0) { g.DrawLine(pen, cx - 2.5f, cy - 2.5f, cx + 2.5f, cy + 2.5f); g.DrawLine(pen, cx - 2.5f, cy + 2.5f, cx + 2.5f, cy - 2.5f); }
                    else if (i == 1) g.DrawLine(pen, cx - 3, cy, cx + 3, cy);
                    else if (Pinned) { using (var b = new SolidBrush(pen.Color)) g.FillEllipse(b, cx - 2, cy - 2, 4, 4); }
                    else { g.DrawLine(pen, cx - 3, cy, cx + 3, cy); g.DrawLine(pen, cx, cy - 3, cx, cy + 3); }
                }
            }
        }
    }

    // Touche du clavier visuel : position et taille en unités (1 = une touche normale).
    // Code = scan code, + 0xE000 pour les touches étendues ; 0 = touche décorative non cliquable.
    public class KbKey
    {
        public RectangleF R;
        public int Code;
        public string Label, Name;

        public KbKey(float x, float y, float w, float h, int code, string label, string name)
        {
            R = new RectangleF(x, y, w, h);
            Code = code;
            Label = label;
            Name = name ?? label;
        }
    }

    // Clavier complet dessiné (Windows ou Mac, AZERTY / QWERTY / QWERTZ) : on clique sur les touches à utiliser.
    // Les touches sont repérées par leur position physique, seule l'étiquette change selon la langue.
    public class KeyboardView : MacControl
    {
        public const int U = 26;   // taille d'une touche en pixels

        readonly List<KbKey> keys = new List<KbKey>();
        readonly List<int> sel = new List<int>();
        readonly Font fBig = Theme.F(9.5f), fSmall = Theme.F(7.25f), fTiny = Theme.F(6f), fNum = Theme.FS(6.5f);
        readonly StringFormat keyFmt = new StringFormat(StringFormat.GenericTypographic)
        {
            Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap | StringFormatFlags.NoClip
        };
        int lang, hover = -1;
        bool mac, multi;
        public event EventHandler SelectionChanged;

        public KeyboardView() { Size = new Size(23 * U, (int)(6.5f * U)); Rebuild(); }

        public int Lang { get { return lang; } set { lang = value; Rebuild(); } }
        public bool Mac { get { return mac; } set { mac = value; Rebuild(); } }

        // Une seule touche ou une suite de touches (appuyées dans l'ordre des clics)
        public bool Multi
        {
            get { return multi; }
            set
            {
                multi = value;
                if (!multi && sel.Count > 1) { sel.RemoveRange(1, sel.Count - 1); Changed(); }
                Invalidate();
            }
        }

        public List<int> Selected { get { return new List<int>(sel); } }

        public string NameOf(int code)
        {
            foreach (var k in keys) if (k.Code == code) return k.Name;
            return "?";
        }

        void Changed() { Invalidate(); if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty); }

        void Add(float x, float y, float w, float h, int code, string label, string name = null)
        {
            keys.Add(new KbKey(x, y, w, h, code, label, name));
        }

        void Rebuild()
        {
            keys.Clear();
            bool iso = lang != 1;   // AZERTY et QWERTZ : clavier ISO (touche < et grande touche Entrée)
            string r1, r2, r3, r4, k2b;
            if (lang == 0)
            {
                r1 = mac ? "@ & é \" ' ( § è ! ç à ) -" : "² & é \" ' ( - è _ ç à ) =";
                r2 = "A Z E R T Y U I O P ^ $";
                r3 = "Q S D F G H J K L M ù";
                r4 = mac ? "W X C V B N , ; : =" : "W X C V B N , ; : !";
                k2b = mac ? "`" : "*";
            }
            else if (lang == 2)
            {
                r1 = "^ 1 2 3 4 5 6 7 8 9 0 ß ´";
                r2 = "Q W E R T Z U I O P Ü +";
                r3 = "A S D F G H J K L Ö Ä";
                r4 = "Y X C V B N M , . -";
                k2b = "#";
            }
            else
            {
                r1 = "` 1 2 3 4 5 6 7 8 9 0 - =";
                r2 = "Q W E R T Y U I O P [ ]";
                r3 = "A S D F G H J K L ; '";
                r4 = "Z X C V B N M , . /";
                k2b = "\\";
            }
            string[] n1 = r1.Split(' '), n2 = r2.Split(' '), n3 = r3.Split(' '), n4 = r4.Split(' ');
            string enter = mac ? "return" : "Entrée", shift = mac ? "shift" : "Maj";

            // ---- rangée des touches de fonction ----
            Add(0, 0, 1, 1, 0x01, mac ? "esc" : "Échap");
            for (int i = 0; i < 12; i++)
                Add(2 + i + (i / 4) * 0.5f, 0, 1, 1, i < 10 ? 0x3B + i : 0x57 + i - 10, "F" + (i + 1));
            if (mac) { Add(15.5f, 0, 1, 1, 0x64, "F13"); Add(16.5f, 0, 1, 1, 0x65, "F14"); Add(17.5f, 0, 1, 1, 0x66, "F15"); }
            else { Add(15.5f, 0, 1, 1, 0xE037, "Impr"); Add(16.5f, 0, 1, 1, 0x46, "Défil"); Add(17.5f, 0, 1, 1, 0, "Pause"); }

            // ---- bloc principal ----
            float y = 1.5f;
            Add(0, y, 1, 1, 0x29, n1[0]);
            for (int i = 1; i <= 12; i++) Add(i, y, 1, 1, 0x01 + i, n1[i]);
            Add(13, y, 2, 1, 0x0E, mac ? "delete" : "Retour");

            y++;
            Add(0, y, 1.5f, 1, 0x0F, mac ? "tab" : "Tab");
            for (int i = 0; i < 12; i++) Add(1.5f + i, y, 1, 1, 0x10 + i, n2[i]);
            if (iso) Add(13.75f, y, 1.25f, 2, 0x1C, enter);
            else Add(13.5f, y, 1.5f, 1, 0x2B, k2b);

            y++;
            Add(0, y, 1.75f, 1, 0x3A, mac ? "caps" : "Verr. maj");
            for (int i = 0; i < 11; i++) Add(1.75f + i, y, 1, 1, 0x1E + i, n3[i]);
            if (iso) Add(12.75f, y, 1, 1, 0x2B, k2b);
            else Add(12.75f, y, 2.25f, 1, 0x1C, enter);

            y++;
            if (iso) { Add(0, y, 1.25f, 1, 0x2A, shift, "Maj gauche"); Add(1.25f, y, 1, 1, 0x56, "<"); }
            else Add(0, y, 2.25f, 1, 0x2A, shift, "Maj gauche");
            for (int i = 0; i < 10; i++) Add(2.25f + i, y, 1, 1, 0x2C + i, n4[i]);
            Add(12.25f, y, 2.75f, 1, 0x36, shift, "Maj droite");

            y++;
            if (mac)
            {
                Add(0, y, 1.5f, 1, 0x1D, "control");
                Add(1.5f, y, 1.25f, 1, 0x38, "option");
                Add(2.75f, y, 1.5f, 1, 0xE05B, "cmd");
                Add(4.25f, y, 6.5f, 1, 0x39, "", "Espace");
                Add(10.75f, y, 1.5f, 1, 0xE05C, "cmd", "cmd droite");
                Add(12.25f, y, 1.25f, 1, 0xE038, "option", "option droite");
                Add(13.5f, y, 1.5f, 1, 0xE01D, "control", "control droite");
            }
            else
            {
                Add(0, y, 1.25f, 1, 0x1D, "Ctrl");
                Add(1.25f, y, 1.25f, 1, 0xE05B, "Win");
                Add(2.5f, y, 1.25f, 1, 0x38, "Alt");
                Add(3.75f, y, 6.25f, 1, 0x39, "Espace");
                Add(10f, y, 1.25f, 1, 0xE038, lang == 1 ? "Alt" : "Alt Gr", lang == 1 ? "Alt droite" : "Alt Gr");
                Add(11.25f, y, 1.25f, 1, 0xE05C, "Win", "Win droite");
                Add(12.5f, y, 1.25f, 1, 0xE05D, "Menu");
                Add(13.75f, y, 1.25f, 1, 0xE01D, "Ctrl", "Ctrl droite");
            }

            // ---- bloc navigation + flèches ----
            y = 1.5f;
            if (mac)
            {
                Add(15.5f, y, 1, 1, 0, "fn");
                Add(16.5f, y, 1, 1, 0xE047, "home");
                Add(17.5f, y, 1, 1, 0xE049, "pg up");
                Add(15.5f, y + 1, 1, 1, 0xE053, "del");
                Add(16.5f, y + 1, 1, 1, 0xE04F, "end");
                Add(17.5f, y + 1, 1, 1, 0xE051, "pg dn");
            }
            else
            {
                Add(15.5f, y, 1, 1, 0xE052, "Inser");
                Add(16.5f, y, 1, 1, 0xE047, "Début");
                Add(17.5f, y, 1, 1, 0xE049, "Pg ↑");
                Add(15.5f, y + 1, 1, 1, 0xE053, "Suppr");
                Add(16.5f, y + 1, 1, 1, 0xE04F, "Fin");
                Add(17.5f, y + 1, 1, 1, 0xE051, "Pg ↓");
            }
            Add(16.5f, y + 3, 1, 1, 0xE048, "↑", "Flèche haut");
            Add(15.5f, y + 4, 1, 1, 0xE04B, "←", "Flèche gauche");
            Add(16.5f, y + 4, 1, 1, 0xE050, "↓", "Flèche bas");
            Add(17.5f, y + 4, 1, 1, 0xE04D, "→", "Flèche droite");

            // ---- pavé numérique ----
            float x = 19;
            if (mac)
            {
                Add(x, y, 1, 1, 0x45, "clear");
                Add(x + 1, y, 1, 1, 0x59, "=", "Pavé =");
                Add(x + 2, y, 1, 1, 0xE035, "/", "Pavé /");
                Add(x + 3, y, 1, 1, 0x37, "*", "Pavé *");
                Add(x + 3, y + 1, 1, 1, 0x4A, "-", "Pavé -");
                Add(x + 3, y + 2, 1, 1, 0x4E, "+", "Pavé +");
            }
            else
            {
                Add(x, y, 1, 1, 0x45, "Num", "Verr num");
                Add(x + 1, y, 1, 1, 0xE035, "/", "Pavé /");
                Add(x + 2, y, 1, 1, 0x37, "*", "Pavé *");
                Add(x + 3, y, 1, 1, 0x4A, "-", "Pavé -");
                Add(x + 3, y + 1, 1, 2, 0x4E, "+", "Pavé +");
            }
            int[] pad = { 0x47, 0x48, 0x49, 0x4B, 0x4C, 0x4D, 0x4F, 0x50, 0x51 };
            for (int i = 0; i < 9; i++)
            {
                string d = "" + "789456123"[i];
                Add(x + i % 3, y + 1 + i / 3, 1, 1, pad[i], d, "Pavé " + d);
            }
            Add(x + 3, y + 3, 1, 2, 0xE01C, mac ? "enter" : "Entrée", "Pavé Entrée");
            Add(x, y + 4, 2, 1, 0x52, "0", "Pavé 0");
            Add(x + 2, y + 4, 1, 1, 0x53, ".", "Pavé .");

            // une touche qui n'existe plus sur ce clavier (ex : < en QWERTY) est retirée de la sélection
            if (sel.RemoveAll(c => !keys.Exists(k => k.Code == c)) > 0) Changed();
            hover = -1;
            Invalidate();
        }

        static RectangleF Px(RectangleF u) { return new RectangleF(u.X * U + 1.5f, u.Y * U + 1, u.Width * U - 3, u.Height * U - 4); }

        int HitTest(Point p)
        {
            for (int i = 0; i < keys.Count; i++) if (Px(keys[i].R).Contains(p)) return i;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int h = HitTest(e.Location);
            if (h != hover) { hover = h; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = -1; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (!En) return;
            int h = HitTest(e.Location);
            if (h < 0 || keys[h].Code == 0) return;
            int code = keys[h].Code;
            if (multi) { if (!sel.Remove(code)) sel.Add(code); }
            else if (sel.Count == 1 && sel[0] == code) return;
            else { sel.Clear(); sel.Add(code); }
            Changed();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Hq(g);
            for (int i = 0; i < keys.Count; i++)
            {
                var k = keys[i];
                var r = Px(k.R);
                int order = sel.IndexOf(k.Code);
                bool on = order >= 0 && k.Code != 0;
                Color c = k.Code == 0 ? Color.FromArgb(52, 52, 56)
                        : on ? Theme.Accent
                        : i == hover && En ? Theme.Mix(Theme.Field, Color.White, 0.12f) : Theme.Field;

                Theme.FillRound(g, Color.FromArgb(En ? 70 : 30, 0, 0, 0), new RectangleF(r.X, r.Y + 1.5f, r.Width, r.Height), 5);
                Theme.FillRound(g, Theme.A(c, En), r, 5);

                // texte centré sans "…" : sur les petites touches il peut déborder d'un pixel dans l'espace entre touches
                Font f = k.Label.Length <= 2 ? fBig : g.MeasureString(k.Label, fSmall, PointF.Empty, keyFmt).Width > r.Width - 2 ? fTiny : fSmall;
                using (var b = new SolidBrush(Theme.A(k.Code == 0 ? Theme.Sub : Theme.Text, En)))
                    g.DrawString(k.Label, f, b, new PointF(r.X + r.Width / 2, r.Y + r.Height / 2 + 0.5f), keyFmt);
                if (on && multi)
                    Theme.DrawText(g, (order + 1).ToString(), fNum, Theme.A(Color.White, En), new RectangleF(r.Right - 12, r.Y, 12, 11), StringAlignment.Center);
            }
        }
    }

    // =====================================================================
    //  Fenêtre principale
    // =====================================================================
    public class MainForm : Form
    {
        // ---- Win32 ----
        [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint mods, uint vk);
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")] static extern uint SendInput(uint n, INPUT[] inputs, int size);
        [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")] static extern bool GetLastInputInfo(ref LASTINPUTINFO info);
        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
        [DllImport("winmm.dll")] static extern uint timeBeginPeriod(uint ms);
        [DllImport("winmm.dll")] static extern uint timeEndPeriod(uint ms);

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public IntPtr dwExtraInfo; }
        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; }
        [StructLayout(LayoutKind.Sequential)]
        struct INPUT { public uint type; public InputUnion u; }
        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO { public uint cbSize, dwTime; }

        public const string AppName = "AutoClicker by BOZYILDIZ";

        const int WM_HOTKEY = 0x0312;
        const int HK_TOGGLE = 1, HK_POS = 2;
        const uint VK_F6 = 0x75, VK_F7 = 0x76;

        // ---- mise en page ----
        const int W = 420, M = 16, CW = W - 2 * M, RowH = 42;
        const int KW = 23 * KeyboardView.U + 28;   // largeur du panneau clavier
        const int ACT_KEYBOARD = 5;                // action anti-AFK "Clavier"

        // ---- UI ----
        TrafficLights lights;
        Segmented segTabs, segMode, segRepeat, segPos, segKbOs, segKbMulti;
        Panel pnlClick, pnlAfk, pnlKb;
        MacNumber numInterval, numCount, numAfk, numAfkStop, numKeyHold, numKeyGap;
        MacSelect cbUnit, cbButton, cbType, cbAfkUnit, cbAfkAction, cbLayout, cbAfkStop, cbHoldUnit, cbGapUnit;
        KeyboardView kb;
        Toggle chkAfkIdle, chkToast, chkBadge;
        Label lblPos, lblStatus, lblKeys;
        MacButton btnStart;
        Overlay toast, badge;
        readonly Font fRow = Theme.F(10f), fSmall = Theme.F(8.75f), fTitle = Theme.FS(10.5f);
        bool uiRunning;

        // ---- état ----
        Thread worker;
        ManualResetEvent stopEvent = new ManualResetEvent(false);
        volatile bool running;
        volatile bool afkMode;
        long clicks;
        long nextActionMs;   // anti-AFK : ms restantes avant la prochaine action
        long stopLeftMs;     // anti-AFK : ms restantes avant l'arrêt automatique (-1 = jamais)
        Point fixedPos = Point.Empty;
        System.Windows.Forms.Timer uiTimer;

        public MainForm()
        {
            Text = AppName;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }   // logo dans la barre des tâches
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = fRow;
            ClientSize = new Size(W, 668);
            DoubleBuffered = true;
            TopMost = true;

            // ===== Barre de titre =====
            lights = new TrafficLights { Location = new Point(18, 16) };
            lights.CloseClick += delegate { Close(); };
            lights.MinClick += delegate { WindowState = FormWindowState.Minimized; };
            lights.PinClick += delegate { TopMost = !TopMost; lights.Pinned = TopMost; lights.Invalidate(); };

            segTabs = new Segmented("AutoClicker", "Anti-AFK") { Size = new Size(260, 30), Location = new Point((W - 260) / 2, 50) };
            segTabs.SelectedIndexChanged += delegate
            {
                pnlClick.Visible = segTabs.SelectedIndex == 0;
                pnlAfk.Visible = segTabs.SelectedIndex == 1;
                UpdateKbPanel();
            };

            pnlClick = new Panel { Location = new Point(0, 94), Size = new Size(W, 372), BackColor = Theme.Bg };
            pnlAfk = new Panel { Location = new Point(0, 94), Size = new Size(W, 372), BackColor = Theme.Bg, Visible = false };

            // ===== Onglet AutoClicker =====
            var c1 = MakeCard(pnlClick, 0, 2);
            segMode = new Segmented("Intervalle", "Hard Spam") { Width = 200 };
            segMode.SelectedIndexChanged += delegate { UpdateLocks(); };
            AddRow(c1, 0, "Mode", segMode);
            numInterval = new MacNumber(1, 100000, 1) { Width = 76 };
            cbUnit = new MacSelect("millisecondes", "secondes", "minutes", "heures") { Width = 130 };
            cbUnit.SelectedIndex = 1;
            AddRow(c1, 1, "1 clic toutes les", numInterval, cbUnit);

            var c2 = MakeCard(pnlClick, 96, 2);
            cbButton = new MacSelect("Bouton gauche", "Bouton droit", "Bouton milieu") { Width = 160 };
            AddRow(c2, 0, "Bouton", cbButton);
            cbType = new MacSelect("Simple clic", "Double clic") { Width = 160 };
            AddRow(c2, 1, "Type de clic", cbType);

            var c3 = MakeCard(pnlClick, 192, 2);
            segRepeat = new Segmented("Infini", "Limité") { Width = 160 };
            segRepeat.SelectedIndexChanged += delegate { UpdateLocks(); };
            AddRow(c3, 0, "Répétition", segRepeat);
            numCount = new MacNumber(1, 10000000, 100) { Width = 110 };
            AddRow(c3, 1, "Nombre de clics", numCount);

            var c4 = MakeCard(pnlClick, 288, 2);
            segPos = new Segmented("Souris", "Point fixe") { Width = 180 };
            AddRow(c4, 0, "Position", segPos);
            lblPos = new Label { AutoSize = false, Size = new Size(200, 28), TextAlign = ContentAlignment.MiddleRight, ForeColor = Theme.Sub, Font = fRow, Text = "F7 pour enregistrer" };
            AddRow(c4, 1, "Point enregistré", lblPos);

            // ===== Onglet Anti-AFK =====
            var a1 = MakeCard(pnlAfk, 0, 4);
            numAfk = new MacNumber(1, 100000, 1) { Width = 70 };
            cbAfkUnit = new MacSelect("secondes", "minutes") { Width = 110 };
            cbAfkUnit.SelectedIndex = 1;
            AddRow(a1, 0, "Agir toutes les", numAfk, cbAfkUnit);
            cbAfkAction = new MacSelect("Mouvement souris (conseillé)", "Clic gauche", "Espace (sauter)", "Z (avancer)", "Souris + Espace", "Clavier (choisir les touches)") { Width = 220 };
            cbAfkAction.SelectedIndexChanged += delegate { UpdateKbPanel(); };
            AddRow(a1, 1, "Action", cbAfkAction);
            cbLayout = new MacSelect("AZERTY (France, Belgique)", "QWERTY (US, UK, ES, IT…)", "QWERTZ (Allemagne, Suisse)") { Width = 220 };
            cbLayout.SelectedIndexChanged += delegate { UpdateForwardLabel(); if (kb != null) kb.Lang = cbLayout.SelectedIndex; };
            cbLayout.SelectedIndex = DetectLayout();
            UpdateForwardLabel();
            AddRow(a1, 2, "Disposition", cbLayout);
            numAfkStop = new MacNumber(1, 100000, 30) { Width = 70 };
            cbAfkStop = new MacSelect("jamais", "minutes", "heures") { Width = 110 };
            cbAfkStop.SelectedIndexChanged += delegate { UpdateLocks(); };
            AddRow(a1, 3, "Arrêt auto après", numAfkStop, cbAfkStop);

            var a2 = new Card { Location = new Point(M, 180), Size = new Size(CW, 60) };
            a2.Controls.Add(new Label { Text = "Seulement si je suis inactif", AutoSize = true, Font = fRow, Location = new Point(14, 10) });
            a2.Controls.Add(new Label { Text = "Se met en pause tout seul quand tu joues", AutoSize = true, Font = fSmall, ForeColor = Theme.Sub, Location = new Point(14, 32) });
            chkAfkIdle = new Toggle(true);
            chkAfkIdle.Location = new Point(CW - 14 - chkAfkIdle.Width, (60 - chkAfkIdle.Height) / 2);
            a2.Controls.Add(chkAfkIdle);
            pnlAfk.Controls.Add(a2);

            pnlAfk.Controls.Add(new Label
            {
                Text = "Choisis un temps plus court que le kick du jeu (ex : kick à 20 min → agir toutes les 1 à 5 min).\n\n"
                     + "La fenêtre du jeu doit être au premier plan pour que l'action soit prise en compte.",
                Location = new Point(M + 6, 254), MaximumSize = new Size(CW - 12, 0), AutoSize = true,
                Font = fSmall, ForeColor = Theme.Sub
            });

            // ===== Clavier visuel : s'ouvre à droite quand l'action "Clavier" est choisie =====
            pnlKb = new Panel { Location = new Point(W, 94), Size = new Size(KW, 560), BackColor = Theme.Bg, Visible = false };
            var k1 = MakeCard(pnlKb, 0, 2, 0, KW);
            segKbOs = new Segmented("Windows", "Mac") { Width = 180 };
            AddRow(k1, 0, "Type de clavier", segKbOs);
            segKbMulti = new Segmented("Une touche", "Plusieurs touches") { Width = 240 };
            AddRow(k1, 1, "Sélection", segKbMulti);

            kb = new KeyboardView { Location = new Point(14, 14), Lang = cbLayout.SelectedIndex };
            var k2 = new Card { Location = new Point(0, 96), Size = new Size(KW, kb.Height + 28) };
            k2.Controls.Add(kb);
            pnlKb.Controls.Add(k2);

            var k3 = MakeCard(pnlKb, k2.Bottom + 12, 3, 0, KW);
            numKeyHold = new MacNumber(1, 100000, 100) { Width = 80 };
            cbHoldUnit = new MacSelect("millisecondes", "secondes", "minutes") { Width = 130 };
            AddRow(k3, 0, "Garder la touche enfoncée pendant", numKeyHold, cbHoldUnit);
            numKeyGap = new MacNumber(0, 100000, 1) { Width = 80 };
            cbGapUnit = new MacSelect("millisecondes", "secondes", "minutes") { Width = 130 };
            cbGapUnit.SelectedIndex = 1;
            AddRow(k3, 1, "Attendre entre chaque touche", numKeyGap, cbGapUnit);
            lblKeys = new Label { AutoSize = false, AutoEllipsis = true, Size = new Size(KW - 180, 28), TextAlign = ContentAlignment.MiddleRight, Font = fRow };
            AddRow(k3, 2, "Touches choisies", lblKeys);

            pnlKb.Controls.Add(new Label
            {
                Text = "Clique sur une touche pour la choisir (clique encore pour la retirer en mode « Plusieurs touches »). "
                     + "Les touches sont appuyées dans l'ordre des numéros, à chaque action.\n\n"
                     + "Garder la touche enfoncée : combien de temps le doigt reste appuyé sur la touche. "
                     + "100 ms = un appui rapide ; 2 secondes sur Z = avancer pendant 2 secondes.\n"
                     + "Attendre entre chaque touche : la pause avant d'appuyer sur la touche suivante (mode « Plusieurs touches »).",
                Location = new Point(6, k3.Bottom + 12), MaximumSize = new Size(KW - 12, 0), AutoSize = true,
                Font = fSmall, ForeColor = Theme.Sub
            });

            segKbOs.SelectedIndexChanged += delegate { kb.Mac = segKbOs.SelectedIndex == 1; };
            segKbMulti.SelectedIndexChanged += delegate { kb.Multi = segKbMulti.SelectedIndex == 1; UpdateLocks(); };
            kb.SelectionChanged += delegate { UpdateKeysLabel(); };
            UpdateKeysLabel();

            // ===== Réglages communs =====
            var s1 = MakeCard(this, 478, 2);
            chkToast = new Toggle(true);
            AddRow(s1, 0, "Notification à l'écran", chkToast);
            chkBadge = new Toggle(true);
            chkBadge.CheckedChanged += delegate { if (running && chkBadge.Checked) badge.ShowBadge(); else badge.Hide(); };
            AddRow(s1, 1, "Indicateur permanent si actif", chkBadge);

            btnStart = new MacButton("Démarrer   (F6)", Theme.Accent) { Location = new Point(M, 574), Width = CW };
            btnStart.Click += delegate { if (running) StopClicking(); else StartSelected(2000); };

            lblStatus = new Label
            {
                Text = "Prêt — F6 pour démarrer / arrêter",
                Location = new Point(M, 626), Size = new Size(CW, 36),
                TextAlign = ContentAlignment.TopCenter, Font = fSmall, ForeColor = Theme.Sub
            };

            Controls.AddRange(new Control[] { lights, segTabs, pnlClick, pnlAfk, pnlKb, btnStart, lblStatus });

            toast = new Overlay(false);
            badge = new Overlay(true);

            UpdateLocks();

            uiTimer = new System.Windows.Forms.Timer { Interval = 200 };
            uiTimer.Tick += delegate
            {
                if (running) lblStatus.Text = StatusText();
                else if (uiRunning) ResetUi();
            };
            uiTimer.Start();
        }

        // ---------- helpers de mise en page ----------
        Card MakeCard(Control parent, int y, int rows, int x = M, int w = CW)
        {
            var c = new Card { Location = new Point(x, y), Size = new Size(w, rows * RowH) };
            for (int r = 1; r < rows; r++) c.Separators.Add(r * RowH);
            parent.Controls.Add(c);
            return c;
        }

        void AddRow(Card c, int row, string label, params Control[] right)
        {
            var l = new Label { Text = label, AutoSize = true, Font = fRow, ForeColor = Theme.Text };
            l.Location = new Point(14, row * RowH + (RowH - l.PreferredHeight) / 2);
            c.Controls.Add(l);
            int x = c.Width - 12;
            for (int i = right.Length - 1; i >= 0; i--)
            {
                Control ctl = right[i];
                x -= ctl.Width;
                ctl.Location = new Point(x, row * RowH + (RowH - ctl.Height) / 2);
                c.Controls.Add(ctl);
                x -= 8;
            }
        }

        Label UnitLabel(string text)
        {
            return new Label { Text = text, AutoSize = false, Size = new Size(24, 28), TextAlign = ContentAlignment.MiddleLeft, Font = fRow, ForeColor = Theme.Sub };
        }

        // Le clavier visuel s'affiche à droite (la fenêtre s'élargit) quand l'action "Clavier" est choisie
        void UpdateKbPanel()
        {
            bool show = segTabs.SelectedIndex == 1 && cbAfkAction.SelectedIndex == ACT_KEYBOARD;
            pnlKb.Visible = show;
            ClientSize = new Size(show ? W + KW + M : W, ClientSize.Height);
            Rectangle wa = Screen.FromControl(this).WorkingArea;
            if (Right > wa.Right) Left = Math.Max(wa.Left, wa.Right - Width);
        }

        void UpdateKeysLabel()
        {
            var keys = kb.Selected;
            if (keys.Count == 0)
            {
                lblKeys.Text = "aucune — clique sur le clavier";
                lblKeys.ForeColor = Theme.Sub;
                return;
            }
            lblKeys.Text = string.Join("  →  ", keys.ConvertAll(c => kb.NameOf(c)).ToArray());
            lblKeys.ForeColor = Theme.Text;
        }

        // ---------- fenêtre sans bordure ----------
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ClassStyle |= 0x20000;   // CS_DROPSHADOW
                cp.Style |= 0x20000;        // WS_MINIMIZEBOX (réduire depuis la barre des tâches)
                return cp;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Theme.Hq(e.Graphics);
            Theme.DrawText(e.Graphics, AppName, fTitle, Theme.Text, new RectangleF(0, 2, W, 42), StringAlignment.Center);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            ReleaseCapture();
            SendMessage(Handle, 0xA1, new IntPtr(2), IntPtr.Zero); // déplacer la fenêtre
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ActiveControl = btnStart;
        }

        // La touche "avancer" est toujours à la même place physique (scan code 0x11),
        // seule la lettre affichée change selon le clavier.
        void UpdateForwardLabel()
        {
            cbAfkAction.SetItem(3, (cbLayout.SelectedIndex == 0 ? "Z" : "W") + " (avancer)");
        }

        static int DetectLayout()
        {
            string c = InputLanguage.CurrentInputLanguage.Culture.Name;
            if (c == "fr-FR" || c == "fr-BE" || c == "nl-BE" || c == "fr-MC" || c == "fr-LU") return 0;
            if (c.StartsWith("de-") || c.EndsWith("-CH") || c.StartsWith("cs-") || c.StartsWith("sk-")
                || c.StartsWith("hu-") || c.StartsWith("sl-") || c.StartsWith("hr-")) return 2;
            return 1;
        }

        string StatusText()
        {
            long n = Interlocked.Read(ref clicks);
            if (!afkMode) return "En cours — " + n + " clics  ·  F6 pour arrêter";
            long left = Interlocked.Read(ref stopLeftMs);
            return "Anti-AFK actif — " + n + " actions  ·  prochaine dans " + Clock(Interlocked.Read(ref nextActionMs))
                 + (left >= 0 ? "\nArrêt automatique dans " + Clock(left)
                    : chkAfkIdle.Checked ? "\nLe compte à rebours repart quand tu bouges" : "");
        }

        static string Clock(long ms)
        {
            long s = Math.Max(0, (ms + 999) / 1000);
            if (s >= 3600) return (s / 3600) + ":" + (s / 60 % 60).ToString("00") + ":" + (s % 60).ToString("00");
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.MacWindow(Handle);
            RegisterHotKey(Handle, HK_TOGGLE, 0, VK_F6);
            RegisterHotKey(Handle, HK_POS, 0, VK_F7);
            timeBeginPeriod(1);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopClicking();
            UnregisterHotKey(Handle, HK_TOGGLE);
            UnregisterHotKey(Handle, HK_POS);
            timeEndPeriod(1);
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HK_TOGGLE) { if (running) StopClicking(); else StartSelected(0); }
                else if (id == HK_POS)
                {
                    fixedPos = Cursor.Position;
                    segPos.SelectedIndex = 1;
                    lblPos.Text = "X " + fixedPos.X + "   Y " + fixedPos.Y;
                    lblPos.ForeColor = Theme.Text;
                }
            }
            base.WndProc(ref m);
        }

        void StartSelected(int startDelayMs)
        {
            if (running) return;
            if (segTabs.SelectedIndex == 1) StartAfk(startDelayMs);
            else StartClicking(startDelayMs);
        }

        void StartClicking(int startDelayMs)
        {
            if (segPos.SelectedIndex == 1 && fixedPos == Point.Empty)
            {
                MessageBox.Show(this, "Place la souris puis appuie sur F7 pour enregistrer le point fixe.", AppName);
                return;
            }

            long ms;
            if (segMode.SelectedIndex == 1) ms = 0;
            else
            {
                long mult = new long[] { 1, 1000, 60000, 3600000 }[cbUnit.SelectedIndex];
                ms = (long)numInterval.Value * mult;
            }

            int btn = cbButton.SelectedIndex;
            bool dbl = cbType.SelectedIndex == 1;
            long max = segRepeat.SelectedIndex == 1 ? (long)numCount.Value : long.MaxValue;
            bool useFixed = segPos.SelectedIndex == 1;
            Point pos = fixedPos;

            BeginRun(false, startDelayMs);
            worker = new Thread(delegate () { ClickLoop(ms, btn, dbl, max, useFixed, pos, startDelayMs); });
            worker.IsBackground = true;
            worker.Priority = ThreadPriority.AboveNormal;
            worker.Start();
        }

        void StartAfk(int startDelayMs)
        {
            long ms = (long)numAfk.Value * (cbAfkUnit.SelectedIndex == 1 ? 60000 : 1000);
            int action = cbAfkAction.SelectedIndex;
            List<int> keys = kb.Selected;
            if (action == ACT_KEYBOARD && keys.Count == 0)
            {
                MessageBox.Show(this, "Choisis au moins une touche sur le clavier.", AppName);
                return;
            }
            long[] mult = { 1, 1000, 60000 };   // millisecondes, secondes, minutes
            long holdMs = (long)numKeyHold.Value * mult[cbHoldUnit.SelectedIndex];
            long gapMs = (long)numKeyGap.Value * mult[cbGapUnit.SelectedIndex];
            bool onlyIdle = chkAfkIdle.Checked;
            long stopAfterMs = cbAfkStop.SelectedIndex == 0 ? 0 : (long)numAfkStop.Value * (cbAfkStop.SelectedIndex == 1 ? 60000 : 3600000);

            BeginRun(true, startDelayMs);
            Interlocked.Exchange(ref nextActionMs, ms);
            Interlocked.Exchange(ref stopLeftMs, stopAfterMs > 0 ? stopAfterMs : -1);
            worker = new Thread(delegate () { AfkLoop(ms, action, keys, holdMs, gapMs, onlyIdle, stopAfterMs, startDelayMs); });
            worker.IsBackground = true;
            worker.Start();
        }

        void BeginRun(bool afk, int startDelayMs)
        {
            afkMode = afk;
            clicks = 0;
            stopEvent.Reset();
            running = true;
            uiRunning = true;
            btnStart.Text = "Arrêter   (F6)";
            btnStart.Fill = Theme.Red;
            UpdateLocks();
            if (startDelayMs > 0) lblStatus.Text = "Démarrage dans " + (startDelayMs / 1000) + " s…";

            string mode = afk ? "Anti-AFK" : "AutoClicker";
            if (chkToast.Checked) toast.ShowToast(mode + " activé", Theme.Green);
            badge.SetBadgeText(mode + " actif  ·  F6");
            if (chkBadge.Checked) badge.ShowBadge();
        }

        void StopClicking()
        {
            if (!running) return;
            running = false;
            stopEvent.Set();
            ResetUi();
        }

        void ResetUi()
        {
            uiRunning = false;
            btnStart.Text = "Démarrer   (F6)";
            btnStart.Fill = Theme.Accent;
            UpdateLocks();
            lblStatus.Text = "Arrêté — " + Interlocked.Read(ref clicks) + (afkMode ? " actions effectuées" : " clics effectués");

            badge.Hide();
            if (chkToast.Checked) toast.ShowToast((afkMode ? "Anti-AFK" : "AutoClicker") + " désactivé", Theme.Red);
        }

        // Verrouille les réglages pendant que ça tourne, et grise les champs inutiles
        void UpdateLocks()
        {
            bool run = uiRunning;
            segTabs.Locked = run;
            LockAll(pnlClick, run);
            LockAll(pnlAfk, run);
            numInterval.Locked = run || segMode.SelectedIndex == 1;
            cbUnit.Locked = run || segMode.SelectedIndex == 1;
            numCount.Locked = run || segRepeat.SelectedIndex == 0;
            LockAll(pnlKb, run);
            numAfkStop.Locked = run || cbAfkStop.SelectedIndex == 0;
            numKeyGap.Locked = run || segKbMulti.SelectedIndex == 0;
            cbGapUnit.Locked = run || segKbMulti.SelectedIndex == 0;
        }

        static void LockAll(Control root, bool locked)
        {
            foreach (Control c in root.Controls)
            {
                var m = c as MacControl;
                if (m != null) m.Locked = locked;
                LockAll(c, locked);
            }
        }

        void ClickLoop(long intervalMs, int btn, bool dbl, long max, bool useFixed, Point pos, int startDelayMs)
        {
            if (startDelayMs > 0 && stopEvent.WaitOne(startDelayMs)) return;

            while (running && Interlocked.Read(ref clicks) < max)
            {
                if (useFixed) SetCursorPos(pos.X, pos.Y);
                SendClick(btn);
                if (dbl) SendClick(btn);
                Interlocked.Increment(ref clicks);

                if (intervalMs == 0)
                {
                    // Hard spam : ~1 ms entre les clics (≈ 500-1000 CPS)
                    if (stopEvent.WaitOne(1)) break;
                }
                else if (Wait(intervalMs)) break;
            }
            running = false;
        }

        void AfkLoop(long intervalMs, int action, List<int> keys, long holdMs, long gapMs, bool onlyIdle, long stopAfterMs, int startDelayMs)
        {
            if (startDelayMs > 0 && stopEvent.WaitOne(startDelayMs)) return;

            int start = Environment.TickCount, lastAction = start;
            while (running)
            {
                // Arrêt automatique après la durée totale choisie
                if (stopAfterMs > 0)
                {
                    long left = stopAfterMs - (uint)(Environment.TickCount - start);
                    Interlocked.Exchange(ref stopLeftMs, left);
                    if (left <= 0) break;
                }

                // Mode "seulement si inactif" : on compte depuis la dernière entrée clavier/souris
                // (la tienne ou celle du programme). Sinon : depuis la dernière action du programme.
                long elapsed = onlyIdle ? IdleMs() : (uint)(Environment.TickCount - lastAction);
                long remaining = intervalMs - elapsed;
                Interlocked.Exchange(ref nextActionMs, remaining);

                if (remaining <= 0)
                {
                    DoAfkAction(action, keys, holdMs, gapMs);
                    lastAction = Environment.TickCount;
                    Interlocked.Increment(ref clicks);
                    Interlocked.Exchange(ref nextActionMs, intervalMs);
                }

                if (stopEvent.WaitOne(250)) break;
            }
            running = false;
        }

        // Attend intervalMs ; renvoie true si on a demandé l'arrêt
        bool Wait(long intervalMs)
        {
            long remaining = intervalMs;
            while (remaining > 0)
            {
                int chunk = (int)Math.Min(remaining, int.MaxValue);
                if (stopEvent.WaitOne(chunk)) return true;
                remaining -= chunk;
            }
            return false;
        }

        static long IdleMs()
        {
            var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO)) };
            if (!GetLastInputInfo(ref info)) return 0;
            return (uint)Environment.TickCount - info.dwTime;
        }

        void DoAfkAction(int action, List<int> keys, long holdMs, long gapMs)
        {
            switch (action)
            {
                case 0: Jiggle(); break;
                case 1: SendClick(0); break;
                case 2: PressKey(0x39); break;           // Espace
                case 3: PressKey(0x11, 300); break;      // touche "avancer" : Z en AZERTY, W en QWERTY/QWERTZ
                case 4: Jiggle(); Thread.Sleep(150); PressKey(0x39); break;
                case ACT_KEYBOARD:                       // touches choisies sur le clavier, dans l'ordre
                    for (int i = 0; i < keys.Count; i++)
                    {
                        if (i > 0 && Wait(gapMs)) return;
                        // touche gardée enfoncée holdMs ; relâchée tout de suite si on arrête (F6)
                        SendKey(keys[i], false);
                        bool stop = Wait(holdMs);
                        SendKey(keys[i], true);
                        if (stop) return;
                    }
                    break;
            }
        }

        static void SendClick(int btn)
        {
            uint down, up;
            if (btn == 1) { down = 0x0008; up = 0x0010; }        // droit
            else if (btn == 2) { down = 0x0020; up = 0x0040; }   // milieu
            else { down = 0x0002; up = 0x0004; }                 // gauche

            var inputs = new INPUT[2];
            inputs[0].type = 0; inputs[0].u.mi.dwFlags = down;
            inputs[1].type = 0; inputs[1].u.mi.dwFlags = up;
            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        // Petit aller-retour de la souris (mouvement relatif, vu par les jeux)
        static void Jiggle()
        {
            MoveRel(15, 0);
            Thread.Sleep(80);
            MoveRel(-15, 0);
        }

        static void MoveRel(int dx, int dy)
        {
            var inputs = new INPUT[1];
            inputs[0].type = 0;
            inputs[0].u.mi.dx = dx;
            inputs[0].u.mi.dy = dy;
            inputs[0].u.mi.dwFlags = 0x0001; // MOUSEEVENTF_MOVE
            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        // Appui touche par scan code (fonctionne mieux dans les jeux que les codes virtuels).
        // code = scan code, + 0xE000 pour les touches étendues (flèches, Win, Alt Gr, pavé /, Entrée du pavé…)
        static void PressKey(int code, int holdMs = 60)
        {
            SendKey(code, false);
            Thread.Sleep(holdMs);
            SendKey(code, true);
        }

        static void SendKey(int code, bool up)
        {
            const uint KEYEVENTF_EXTENDEDKEY = 0x0001, KEYEVENTF_SCANCODE = 0x0008, KEYEVENTF_KEYUP = 0x0002;
            var input = new INPUT[1];
            input[0].type = 1;
            input[0].u.ki.wScan = (ushort)(code & 0xFF);
            input[0].u.ki.dwFlags = KEYEVENTF_SCANCODE
                | ((code & 0xE000) == 0xE000 ? KEYEVENTF_EXTENDEDKEY : 0)
                | (up ? KEYEVENTF_KEYUP : 0);
            SendInput(1, input, Marshal.SizeOf(typeof(INPUT)));
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    // =====================================================================
    //  Overlay par-dessus le jeu (style HUD macOS) :
    //  ne prend jamais le focus et laisse passer les clics.
    // =====================================================================
    public class Overlay : Form
    {
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

        readonly bool isBadge;
        readonly Font font;
        readonly System.Windows.Forms.Timer timer;
        string text = "";
        Color dot = Theme.Green;
        int ticks;

        public Overlay(bool isBadge)
        {
            this.isBadge = isBadge;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(32, 32, 34);
            Opacity = isBadge ? 0.9 : 0.95;
            font = isBadge ? Theme.FS(9.5f) : Theme.FS(13f);

            timer = new System.Windows.Forms.Timer { Interval = isBadge ? 1000 : 30 };
            timer.Tick += delegate { OnTick(); };
            VisibleChanged += delegate { if (!Visible) timer.Stop(); };
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                // TOPMOST | TOOLWINDOW | TRANSPARENT (clics traversants) | LAYERED | NOACTIVATE
                cp.ExStyle |= 0x8 | 0x80 | 0x20 | 0x80000 | 0x08000000;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Theme.MacWindow(Handle);
        }

        int DotSize { get { return isBadge ? 8 : 12; } }
        int PadX { get { return isBadge ? 14 : 22; } }
        int Gap { get { return isBadge ? 8 : 12; } }

        void SetContent(string t, Color c)
        {
            text = t;
            dot = c;
            Size sz = TextRenderer.MeasureText(t, font);
            Size = new Size(PadX + DotSize + Gap + sz.Width + PadX - 4, isBadge ? 30 : 54);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Hq(g);
            float d = DotSize, y = (Height - d) / 2f;
            using (var glow = new SolidBrush(Color.FromArgb(70, dot))) g.FillEllipse(glow, PadX - 3, y - 3, d + 6, d + 6);
            using (var b = new SolidBrush(dot)) g.FillEllipse(b, PadX, y, d, d);
            Size sz = TextRenderer.MeasureText(text, font);
            TextRenderer.DrawText(g, text, font, new Point(PadX + DotSize + Gap, (Height - sz.Height) / 2), Theme.Text);
        }

        public void ShowToast(string t, Color c)
        {
            timer.Stop();
            SetContent(t, c);
            Rectangle b = Screen.FromPoint(Cursor.Position).Bounds;
            Location = new Point(b.Left + (b.Width - Width) / 2, b.Top + b.Height / 6);
            Opacity = 0.95;
            ticks = 0;
            Show();
            BringTop();
            timer.Start();
        }

        public void SetBadgeText(string t) { SetContent(t, Theme.Green); }

        public void ShowBadge()
        {
            Rectangle b = Screen.FromPoint(Cursor.Position).Bounds;
            Location = new Point(b.Right - Width - 14, b.Top + 14);
            Show();
            BringTop();
            timer.Start();
        }

        void OnTick()
        {
            if (isBadge) { BringTop(); return; }  // reste au-dessus du jeu
            // Toast : visible ~1,5 s puis fondu
            if (++ticks < 50) return;
            Opacity -= 0.08;
            if (Opacity <= 0.05) Hide();
        }

        void BringTop()
        {
            // HWND_TOPMOST, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW
            SetWindowPos(Handle, new IntPtr(-1), 0, 0, 0, 0, 0x2 | 0x1 | 0x10 | 0x40);
        }
    }
}
