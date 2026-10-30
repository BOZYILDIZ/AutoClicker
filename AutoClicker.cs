using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
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
                Theme.DrawText(g, Lang.T(items[i]), font, Theme.A(i == sel ? Theme.Text : Theme.Sub, En), new RectangleF(i * w, 0, w, Height), StringAlignment.Center);
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
                string text = Lang.T(items[i]);
                var it = new ToolStripMenuItem(text) { Checked = i == sel, ForeColor = Theme.Text, AutoSize = false };
                it.Size = new Size(Math.Max(Width, TextRenderer.MeasureText(text, font).Width + 50), 26);
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
            string s = sel >= 0 && sel < items.Count ? Lang.T(items[sel]) : "";
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
            foreach (var k in keys)
                if (k.Code == code)
                    return k.Name.StartsWith("Pavé ") ? AutoClicker.Lang.T("Pavé") + " " + AutoClicker.Lang.T(k.Name.Substring(5)) : AutoClicker.Lang.T(k.Name);
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
                string label = AutoClicker.Lang.T(k.Label);
                Font f = label.Length <= 2 ? fBig : g.MeasureString(label, fSmall, PointF.Empty, keyFmt).Width > r.Width - 2 ? fTiny : fSmall;
                using (var b = new SolidBrush(Theme.A(k.Code == 0 ? Theme.Sub : Theme.Text, En)))
                    g.DrawString(label, f, b, new PointF(r.X + r.Width / 2, r.Y + r.Height / 2 + 0.5f), keyFmt);
                if (on && multi)
                    Theme.DrawText(g, (order + 1).ToString(), fNum, Theme.A(Color.White, En), new RectangleF(r.Right - 12, r.Y, 12, 11), StringAlignment.Center);
            }
        }
    }

    // Repère le message de récolte du serveur (« E | Mantarı Topla », cadre vert menthe) au centre de l'écran.
    // On compte les pixels de ce vert : ~490 en 1080p quand le message est affiché, 0 sinon.
    public class PromptDetector : IDisposable
    {
        readonly Rectangle zone;
        readonly Bitmap bmp;
        readonly Graphics g;
        readonly double scale;

        // Zone de l'écran à ignorer (la fenêtre du programme : ses interrupteurs sont verts eux aussi)
        public Func<Rectangle> Exclude;

        public PromptDetector()
        {
            // centre de l'écran : on évite la mini-carte, la barre d'objets et le HUD du serveur (eux aussi en vert)
            Rectangle s = Screen.PrimaryScreen.Bounds;
            zone = new Rectangle(s.X + s.Width * 20 / 100, s.Y + s.Height * 25 / 100, s.Width * 60 / 100, s.Height * 57 / 100);
            bmp = new Bitmap(zone.Width, zone.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            g = Graphics.FromImage(bmp);
            scale = 1080.0 * 1080.0 / ((double)s.Height * s.Height);   // le texte grandit avec la hauteur de l'écran
        }

        public Rectangle Zone { get { return zone; } }

        // Nombre de pixels vert menthe dans la zone, ramené à un écran 1080p
        public int Count()
        {
            try { g.CopyFromScreen(zone.Location, Point.Empty, zone.Size); }
            catch { return 0; }

            // rectangle à ignorer, ramené aux coordonnées de la zone capturée
            Rectangle ex = Rectangle.Intersect(Exclude != null ? Exclude() : Rectangle.Empty, zone);
            ex.Offset(-zone.X, -zone.Y);

            var rect = new Rectangle(Point.Empty, bmp.Size);
            var data = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            int n = 0;
            try
            {
                var row = new byte[data.Stride];
                for (int y = 0; y < data.Height; y++)
                {
                    Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, data.Stride);
                    bool exRow = y >= ex.Top && y < ex.Bottom;
                    for (int i = 0; i < data.Width * 4; i += 4)
                    {
                        if (exRow && i / 4 >= ex.Left && i / 4 < ex.Right) continue;
                        int b = row[i], gr = row[i + 1], r = row[i + 2];
                        if (gr > 170 && gr - r > 90 && gr - b > 30 && b > 80) n++;
                    }
                }
            }
            finally { bmp.UnlockBits(data); }
            return (int)(n * scale);
        }

        public void Dispose() { g.Dispose(); bmp.Dispose(); }
    }

    // Trajet enregistré pour la récolte : touches et mouvements de souris horodatés (ms depuis le début),
    // rejoués en boucle. Sauvegardé dans %APPDATA%\AutoClicker by BOZYILDIZ\trajet.txt
    public class Route
    {
        public const byte KeyDown = 0, KeyUp = 1, Move = 2;

        // touche : A = code (scan code, + 0xE000 si étendue) ; souris : A = dx, B = dy
        public struct Ev
        {
            public int T, A, B;
            public byte Kind;
            public Ev(int t, byte kind, int a, int b) { T = t; Kind = kind; A = a; B = b; }
        }

        public readonly List<Ev> Events = new List<Ev>();
        public int Duration;

        // dossier des données du programme (trajet, journal de récolte)
        public static string DataDir
        {
            get { return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), MainForm.AppName); }
        }

        static string FilePath { get { return System.IO.Path.Combine(DataDir, "trajet.txt"); } }

        public void Save()
        {
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(FilePath));
                var sb = new System.Text.StringBuilder();
                sb.Append(Duration).Append('\n');
                foreach (var e in Events) sb.Append(e.T).Append(' ').Append(e.Kind).Append(' ').Append(e.A).Append(' ').Append(e.B).Append('\n');
                System.IO.File.WriteAllText(FilePath, sb.ToString());
            }
            catch { }
        }

        public static Route Load()
        {
            try
            {
                if (!System.IO.File.Exists(FilePath)) return null;
                string[] lines = System.IO.File.ReadAllLines(FilePath);
                var r = new Route { Duration = int.Parse(lines[0]) };
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] p = lines[i].Split(' ');
                    if (p.Length == 4) r.Events.Add(new Ev(int.Parse(p[0]), byte.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3])));
                }
                return r.Duration > 0 && r.Events.Count > 0 ? r : null;
            }
            catch { return null; }
        }
    }

    // =====================================================================
    //  Traductions : le texte français sert de clé, Tr donne { anglais, turc }.
    //  Langue choisie mémorisée dans %APPDATA%\AutoClicker by BOZYILDIZ\langue.txt
    // =====================================================================
    public static class Lang
    {
        public const int FR = 0, EN = 1, TR = 2;
        static int current = Load();
        public static event EventHandler Changed;

        public static int Current
        {
            get { return current; }
            set
            {
                if (value == current) return;
                current = value;
                try { System.IO.Directory.CreateDirectory(Route.DataDir); System.IO.File.WriteAllText(LangFile, value.ToString()); } catch { }
                if (Changed != null) Changed(null, EventArgs.Empty);
            }
        }

        static string LangFile { get { return System.IO.Path.Combine(Route.DataDir, "langue.txt"); } }

        static int Load()
        {
            try { int v; if (int.TryParse(System.IO.File.ReadAllText(LangFile).Trim(), out v) && v >= 0 && v <= 2) return v; }
            catch { }
            return FR;
        }

        public static string T(string fr)
        {
            string[] t;
            return current == FR || fr == null || !Tr.TryGetValue(fr, out t) ? fr : t[current - 1];
        }

        public static string T(string fr, params object[] args) { return string.Format(T(fr), args); }

        static readonly Dictionary<string, string[]> Tr = new Dictionary<string, string[]>
        {
            // ---- onglets, modes ----
            { "Récolte", new[] { "Harvest", "Hasat" } },
            { "Intervalle", new[] { "Interval", "Aralık" } },
            { "Mode", new[] { "Mode", "Mod" } },
            { "millisecondes", new[] { "milliseconds", "milisaniye" } },
            { "secondes", new[] { "seconds", "saniye" } },
            { "minutes", new[] { "minutes", "dakika" } },
            { "heures", new[] { "hours", "saat" } },
            { "jamais", new[] { "never", "kapalı" } },

            // ---- onglet AutoClicker ----
            { "1 clic toutes les", new[] { "1 click every", "Tıklama aralığı" } },
            { "Bouton gauche", new[] { "Left button", "Sol tuş" } },
            { "Bouton droit", new[] { "Right button", "Sağ tuş" } },
            { "Bouton milieu", new[] { "Middle button", "Orta tuş" } },
            { "Bouton", new[] { "Button", "Fare tuşu" } },
            { "Simple clic", new[] { "Single click", "Tek tık" } },
            { "Double clic", new[] { "Double click", "Çift tık" } },
            { "Type de clic", new[] { "Click type", "Tıklama türü" } },
            { "Infini", new[] { "Unlimited", "Sonsuz" } },
            { "Limité", new[] { "Limited", "Sınırlı" } },
            { "Répétition", new[] { "Repeat", "Tekrar" } },
            { "Nombre de clics", new[] { "Number of clicks", "Tıklama sayısı" } },
            { "Souris", new[] { "Cursor", "Fare" } },
            { "Point fixe", new[] { "Fixed point", "Sabit nokta" } },
            { "Position", new[] { "Position", "Konum" } },
            { "F7 pour enregistrer", new[] { "F7 to save", "Kaydetmek için F7" } },
            { "Point enregistré", new[] { "Saved point", "Kayıtlı nokta" } },

            // ---- onglet Anti-AFK ----
            { "Agir toutes les", new[] { "Act every", "Eylem aralığı" } },
            { "Mouvement souris (conseillé)", new[] { "Mouse movement (recommended)", "Fare hareketi (önerilen)" } },
            { "Clic gauche", new[] { "Left click", "Sol tık" } },
            { "Espace (sauter)", new[] { "Space (jump)", "Space (zıpla)" } },
            { "(avancer)", new[] { "(walk forward)", "(ileri yürü)" } },
            { "Souris + Espace", new[] { "Mouse + Space", "Fare + Space" } },
            { "Clavier (choisir les touches)", new[] { "Keyboard (pick keys)", "Klavye (tuşları seç)" } },
            { "Action", new[] { "Action", "Eylem" } },
            { "AZERTY (France, Belgique)", new[] { "AZERTY (France, Belgium)", "AZERTY (Fransa, Belçika)" } },
            { "QWERTY (US, UK, ES, IT…)", new[] { "QWERTY (US, UK, ES, IT…)", "QWERTY (ABD, İngiltere, İspanya…)" } },
            { "QWERTZ (Allemagne, Suisse)", new[] { "QWERTZ (Germany, Switzerland)", "QWERTZ (Almanya, İsviçre)" } },
            { "Disposition", new[] { "Layout", "Klavye düzeni" } },
            { "Arrêt auto après", new[] { "Auto-stop after", "Otomatik durdurma" } },
            { "Seulement si je suis inactif", new[] { "Only when I'm idle", "Sadece hareketsizken" } },
            { "Se met en pause tout seul quand tu joues", new[] { "Pauses automatically while you play", "Sen oynarken kendiliğinden duraklar" } },
            { "Choisis un temps plus court que le kick du jeu (ex : kick à 20 min → agir toutes les 1 à 5 min).\n\n"
              + "La fenêtre du jeu doit être au premier plan pour que l'action soit prise en compte.",
              new[] { "Pick an interval shorter than the game's AFK kick (e.g. kick at 20 min → act every 1 to 5 min).\n\n"
                      + "The game window must be in the foreground for the action to register.",
                      "Oyunun AFK'dan atma süresinden daha kısa bir süre seç (örn. 20 dk'da atıyorsa → 1 ila 5 dk'da bir).\n\n"
                      + "Eylemin algılanması için oyun penceresi ön planda olmalı." } },

            // ---- clavier visuel ----
            { "Type de clavier", new[] { "Keyboard type", "Klavye türü" } },
            { "Une touche", new[] { "One key", "Tek tuş" } },
            { "Plusieurs touches", new[] { "Multiple keys", "Birden fazla tuş" } },
            { "Sélection", new[] { "Selection", "Seçim" } },
            { "Garder la touche enfoncée pendant", new[] { "Hold each key for", "Tuşu basılı tutma süresi" } },
            { "Attendre entre chaque touche", new[] { "Wait between keys", "Tuşlar arası bekleme" } },
            { "Touches choisies", new[] { "Selected keys", "Seçilen tuşlar" } },
            { "aucune — clique sur le clavier", new[] { "none — click the keyboard", "yok — klavyeye tıkla" } },
            { "Clique sur une touche pour la choisir (clique encore pour la retirer en mode « Plusieurs touches »). "
              + "Les touches sont appuyées dans l'ordre des numéros, à chaque action.\n\n"
              + "Garder la touche enfoncée : combien de temps le doigt reste appuyé sur la touche. "
              + "100 ms = un appui rapide ; 2 secondes sur Z = avancer pendant 2 secondes.\n"
              + "Attendre entre chaque touche : la pause avant d'appuyer sur la touche suivante (mode « Plusieurs touches »).",
              new[] { "Click a key to select it (click it again to remove it in “Multiple keys” mode). "
                      + "On each action, keys are pressed in numbered order.\n\n"
                      + "Hold each key for: how long each key stays pressed. "
                      + "100 ms = a quick tap; 2 seconds on W = walk forward for 2 seconds.\n"
                      + "Wait between keys: the pause before pressing the next key (“Multiple keys” mode).",
                      "Seçmek için bir tuşa tıkla (“Birden fazla tuş” modunda kaldırmak için tekrar tıkla). "
                      + "Her eylemde tuşlara numara sırasıyla basılır.\n\n"
                      + "Tuşu basılı tutma süresi: tuşun ne kadar süre basılı kalacağı. "
                      + "100 ms = hızlı bir basış; W'ye 2 saniye basmak = 2 saniye ileri yürümek.\n"
                      + "Tuşlar arası bekleme: bir sonraki tuşa basmadan önceki bekleme (“Birden fazla tuş” modu)." } },

            // ---- touches du clavier visuel ----
            { "Échap", new[] { "Esc", "Esc" } },
            { "Impr", new[] { "PrtSc", "PrtSc" } },
            { "Défil", new[] { "ScrLk", "ScrLk" } },
            { "Retour", new[] { "Backspace", "Backspace" } },
            { "Verr. maj", new[] { "Caps Lock", "Caps Lock" } },
            { "Entrée", new[] { "Enter", "Enter" } },
            { "Maj", new[] { "Shift", "Shift" } },
            { "Espace", new[] { "Space", "Space" } },
            { "Inser", new[] { "Ins", "Ins" } },
            { "Début", new[] { "Home", "Home" } },
            { "Suppr", new[] { "Del", "Del" } },
            { "Fin", new[] { "End", "End" } },
            { "Pg ↑", new[] { "PgUp", "PgUp" } },
            { "Pg ↓", new[] { "PgDn", "PgDn" } },
            { "Maj gauche", new[] { "Left Shift", "Sol Shift" } },
            { "Maj droite", new[] { "Right Shift", "Sağ Shift" } },
            { "Alt droite", new[] { "Right Alt", "Sağ Alt" } },
            { "Win droite", new[] { "Right Win", "Sağ Win" } },
            { "Ctrl droite", new[] { "Right Ctrl", "Sağ Ctrl" } },
            { "cmd droite", new[] { "right cmd", "sağ cmd" } },
            { "option droite", new[] { "right option", "sağ option" } },
            { "control droite", new[] { "right control", "sağ control" } },
            { "Flèche haut", new[] { "Up arrow", "Yukarı ok" } },
            { "Flèche gauche", new[] { "Left arrow", "Sol ok" } },
            { "Flèche bas", new[] { "Down arrow", "Aşağı ok" } },
            { "Flèche droite", new[] { "Right arrow", "Sağ ok" } },
            { "Verr num", new[] { "Num Lock", "Num Lock" } },
            { "Pavé", new[] { "Numpad", "Numpad" } },

            // ---- onglet Récolte ----
            { "Touche à appuyer", new[] { "Key to press", "Basılacak tuş" } },
            { "Pause après récolte", new[] { "Pause after harvest", "Hasat sonrası bekleme" } },
            { "Basse", new[] { "Low", "Düşük" } },
            { "Normale", new[] { "Normal", "Normal" } },
            { "Haute", new[] { "High", "Yüksek" } },
            { "Sensibilité", new[] { "Sensitivity", "Hassasiyet" } },
            { "pas visible", new[] { "not visible", "görünmüyor" } },
            { "détecté", new[] { "detected", "algılandı" } },
            { "Message de récolte", new[] { "Harvest prompt", "Hasat mesajı" } },
            { "Suivre un trajet enregistré", new[] { "Follow a recorded route", "Kayıtlı rotayı takip et" } },
            { "Trajet", new[] { "Route", "Rota" } },
            { "Appuie sur la touche dès que le cadre vert du message de récolte apparaît au centre de l'écran.\n"
              + "Trajet : en jeu, appuie sur F9, fais le tour du champ en revenant au point de départ, puis F9. "
              + "Avec F6, le perso refait ce tour en boucle et s'arrête pour ramasser.",
              new[] { "Presses the key as soon as the green frame of the harvest prompt shows up in the middle of the screen.\n"
                      + "Route: in game, press F9, walk around the field and back to your starting point, then press F9 again. "
                      + "With F6, your character loops this route and stops to harvest.",
                      "Hasat mesajının yeşil çerçevesi ekranın ortasında belirince tuşa basar.\n"
                      + "Rota: oyunda F9'a bas, tarlanın etrafını dolaşıp başlangıç noktasına dön, sonra tekrar F9'a bas. "
                      + "F6 ile karakterin bu turu döngü halinde tekrarlar ve toplamak için durur." } },

            // ---- réglages communs, bouton, statut ----
            { "Notification à l'écran", new[] { "On-screen notification", "Ekran bildirimi" } },
            { "Indicateur permanent si actif", new[] { "Indicator while running", "Aktifken kalıcı gösterge" } },
            { "Démarrer   (F6)", new[] { "Start   (F6)", "Başlat   (F6)" } },
            { "Arrêter   (F6)", new[] { "Stop   (F6)", "Durdur   (F6)" } },
            { "Prêt — F6 pour démarrer / arrêter", new[] { "Ready — F6 to start / stop", "Hazır — başlatmak / durdurmak için F6" } },
            { "Démarrage dans {0} s…", new[] { "Starting in {0} s…", "{0} sn içinde başlıyor…" } },
            { "Arrêt en cours… réessaie dans un instant", new[] { "Stopping… try again in a moment", "Durduruluyor… birazdan tekrar dene" } },
            { "En cours — {0} clics  ·  F6 pour arrêter", new[] { "Running — {0} clicks  ·  F6 to stop", "Çalışıyor — {0} tık  ·  durdurmak için F6" } },
            { "Récolte active — {0} récoltes  ·  F6 pour arrêter", new[] { "Harvest on — {0} harvested  ·  F6 to stop", "Hasat açık — {0} hasat  ·  durdurmak için F6" } },
            { "Message détecté, récolte…", new[] { "Prompt detected, harvesting…", "Mesaj algılandı, hasat ediliyor…" } },
            { "Trajet en cours — tour {0}", new[] { "Route running — lap {0}", "Rota sürüyor — {0}. tur" } },
            { "Déplace-toi vers un champignon", new[] { "Move towards a mushroom", "Bir mantara doğru git" } },
            { "Anti-AFK actif — {0} actions  ·  prochaine dans {1}", new[] { "Anti-AFK on — {0} actions  ·  next in {1}", "Anti-AFK açık — {0} eylem  ·  sonraki: {1}" } },
            { "Arrêt automatique dans {0}", new[] { "Auto-stop in {0}", "{0} içinde otomatik duracak" } },
            { "Le compte à rebours repart quand tu bouges", new[] { "The countdown restarts when you move", "Hareket edince geri sayım yeniden başlar" } },
            { "Arrêté — {0} clics", new[] { "Stopped — {0} clicks", "Durduruldu — {0} tık" } },
            { "Arrêté — {0} actions", new[] { "Stopped — {0} actions", "Durduruldu — {0} eylem" } },
            { "Arrêté — {0} récoltes", new[] { "Stopped — {0} harvested", "Durduruldu — {0} hasat" } },
            { "AutoClicker activé", new[] { "AutoClicker on", "AutoClicker açıldı" } },
            { "Anti-AFK activé", new[] { "Anti-AFK on", "Anti-AFK açıldı" } },
            { "Récolte activée", new[] { "Harvest on", "Hasat açıldı" } },
            { "AutoClicker désactivé", new[] { "AutoClicker off", "AutoClicker kapandı" } },
            { "Anti-AFK désactivé", new[] { "Anti-AFK off", "Anti-AFK kapandı" } },
            { "Récolte désactivée", new[] { "Harvest off", "Hasat kapandı" } },
            { "AutoClicker actif  ·  F6", new[] { "AutoClicker on  ·  F6", "AutoClicker açık  ·  F6" } },
            { "Anti-AFK actif  ·  F6", new[] { "Anti-AFK on  ·  F6", "Anti-AFK açık  ·  F6" } },
            { "Récolte active  ·  F6", new[] { "Harvest on  ·  F6", "Hasat açık  ·  F6" } },

            // ---- confiance du certificat (1er lancement) ----
            { "Rendre AutoClicker reconnu sur ce PC ?", new[] { "Make AutoClicker trusted on this PC?", "AutoClicker bu PC'de tanınsın mı?" } },
            { "Windows ne connaît pas encore cette application et peut l'afficher comme « éditeur inconnu ». "
                    + "Cette étape ajoute le certificat de BOZYILDIZ pour qu'AutoClicker soit reconnu sur ce PC.\n\n"
                    + "Windows affichera un avertissement de sécurité : choisis « Oui » pour confirmer. "
                    + "Tu peux refuser, l'application fonctionnera quand même.",
              new[] { "Windows doesn't know this app yet and may show it as \"unknown publisher\". "
                    + "This step adds BOZYILDIZ's certificate so AutoClicker is trusted on this PC.\n\n"
                    + "Windows will show a security warning: choose \"Yes\" to confirm. "
                    + "You can decline, the app will still work.",
                      "Windows bu uygulamayı henüz tanımıyor ve \"bilinmeyen yayımcı\" olarak gösterebilir. "
                    + "Bu adım, AutoClicker'ın bu PC'de tanınması için BOZYILDIZ sertifikasını ekler.\n\n"
                    + "Windows bir güvenlik uyarısı gösterecek: onaylamak için \"Evet\" seç. "
                    + "Reddedebilirsin, uygulama yine de çalışır." } },
            { "Ne plus me demander", new[] { "Don't ask again", "Bir daha sorma" } },
            { "Rendre reconnu", new[] { "Make trusted", "Tanıt" } },
            { "Plus tard", new[] { "Later", "Sonra" } },

            // ---- trajet ----
            { "Enregistrement du trajet  ·  F9 pour finir", new[] { "Recording route  ·  F9 to finish", "Rota kaydediliyor  ·  bitirmek için F9" } },
            { "Enregistrement du trajet  ·  F9", new[] { "Recording route  ·  F9", "Rota kaydediliyor  ·  F9" } },
            { "Aucune touche n'a été enregistrée, seulement la souris : le perso tournera sur place sans avancer.",
              new[] { "No key was recorded, only the mouse: the character will turn on the spot without moving.",
                      "Hiçbir tuş kaydedilmedi, sadece fare: karakter ilerlemeden yerinde dönecek." } },
            { "Trajet trop court, ancien trajet conservé", new[] { "Route too short, previous route kept", "Rota çok kısa, eski rota korundu" } },
            { "Trajet enregistré  ·  {0}", new[] { "Route saved  ·  {0}", "Rota kaydedildi  ·  {0}" } },
            { "enregistrement… {0}  ·  F9", new[] { "recording… {0}  ·  F9", "kaydediliyor… {0}  ·  F9" } },
            { "aucun  ·  F9 pour enregistrer", new[] { "none  ·  F9 to record", "yok  ·  kaydetmek için F9" } },
            { "{0}  ·  F9 pour refaire", new[] { "{0}  ·  F9 to re-record", "{0}  ·  yeniden kaydetmek için F9" } },

            // ---- messages ----
            { "Impossible d'écouter le clavier et la souris pour enregistrer le trajet.",
              new[] { "Can't capture keyboard and mouse input to record the route.", "Rotayı kaydetmek için klavye ve fare dinlenemiyor." } },
            { "Enregistre d'abord un trajet : en jeu, appuie sur F9, fais le tour du champ, puis F9 à nouveau.",
              new[] { "Record a route first: in game, press F9, walk around the field, then press F9 again.",
                      "Önce bir rota kaydet: oyunda F9'a bas, tarlanın etrafını dolaş, sonra tekrar F9'a bas." } },
            { "Place la souris puis appuie sur F7 pour enregistrer le point fixe.",
              new[] { "Move the cursor to the spot, then press F7 to save the fixed point.", "Fareyi yerleştir, sonra sabit noktayı kaydetmek için F7'ye bas." } },
            { "Choisis au moins une touche sur le clavier.", new[] { "Pick at least one key on the keyboard.", "Klavyeden en az bir tuş seç." } },
        };
    }

    // Drapeaux de langue (France, Royaume-Uni, Turquie) dessinés à la main, en haut à droite
    public class LangFlags : MacControl
    {
        const int FW = 22, FH = 15, Gap = 8;
        int hover = -1;

        public LangFlags() { Size = new Size(3 * FW + 2 * Gap + 2, FH + 2); }

        int HitTest(int x) { int i = x / (FW + Gap); return i >= 0 && i < 3 && x - i * (FW + Gap) <= FW ? i : -1; }

        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); int h = HitTest(e.X); if (h != hover) { hover = h; Invalidate(); } }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = -1; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int h = HitTest(e.X);
            if (h >= 0 && En) { Lang.Current = h; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Hq(g);
            for (int i = 0; i < 3; i++)
            {
                var r = new RectangleF(1 + i * (FW + Gap), 1, FW, FH);
                using (var clip = Theme.Round(r, 3))
                {
                    var state = g.Save();
                    g.SetClip(clip);
                    if (i == 0) DrawFrance(g, r); else if (i == 1) DrawUk(g, r); else DrawTurkey(g, r);
                    g.Restore(state);

                    // langue non choisie : drapeau assombri (moins si survolé)
                    if (i != Lang.Current)
                        using (var b = new SolidBrush(Color.FromArgb(i == hover ? 70 : 140, Theme.Bg))) g.FillPath(b, clip);
                    using (var pen = new Pen(i == Lang.Current ? Color.FromArgb(220, 255, 255, 255) : Color.FromArgb(40, 255, 255, 255), i == Lang.Current ? 1.4f : 1f))
                        g.DrawPath(pen, clip);
                }
            }
        }

        static void DrawFrance(Graphics g, RectangleF r)
        {
            float w = r.Width / 3;
            using (var b = new SolidBrush(Color.FromArgb(0, 85, 164))) g.FillRectangle(b, r.X, r.Y, w + 0.5f, r.Height);
            using (var b = new SolidBrush(Color.White)) g.FillRectangle(b, r.X + w, r.Y, w + 0.5f, r.Height);
            using (var b = new SolidBrush(Color.FromArgb(239, 65, 53))) g.FillRectangle(b, r.X + 2 * w, r.Y, w, r.Height);
        }

        static void DrawUk(Graphics g, RectangleF r)
        {
            Color blue = Color.FromArgb(1, 33, 105), red = Color.FromArgb(200, 16, 46);
            using (var b = new SolidBrush(blue)) g.FillRectangle(b, r);
            float h = r.Height;
            using (var p = new Pen(Color.White, h * 0.3f)) { g.DrawLine(p, r.Left, r.Top, r.Right, r.Bottom); g.DrawLine(p, r.Left, r.Bottom, r.Right, r.Top); }
            using (var p = new Pen(red, h * 0.1f)) { g.DrawLine(p, r.Left, r.Top, r.Right, r.Bottom); g.DrawLine(p, r.Left, r.Bottom, r.Right, r.Top); }
            float cx = r.X + r.Width / 2, cy = r.Y + h / 2;
            using (var b = new SolidBrush(Color.White))
            {
                g.FillRectangle(b, cx - h * 0.17f, r.Top, h * 0.34f, h);
                g.FillRectangle(b, r.Left, cy - h * 0.17f, r.Width, h * 0.34f);
            }
            using (var b = new SolidBrush(red))
            {
                g.FillRectangle(b, cx - h * 0.1f, r.Top, h * 0.2f, h);
                g.FillRectangle(b, r.Left, cy - h * 0.1f, r.Width, h * 0.2f);
            }
        }

        static void DrawTurkey(Graphics g, RectangleF r)
        {
            Color red = Color.FromArgb(227, 10, 23);
            float h = r.Height, cy = r.Y + h / 2;
            using (var b = new SolidBrush(red)) g.FillRectangle(b, r);
            // croissant : grand disque blanc moins un disque rouge décalé vers la droite
            float cx = r.X + h * 0.5f + 1;
            using (var b = new SolidBrush(Color.White)) g.FillEllipse(b, cx - h * 0.25f, cy - h * 0.25f, h * 0.5f, h * 0.5f);
            float cx2 = cx + h * 0.0625f;
            using (var b = new SolidBrush(red)) g.FillEllipse(b, cx2 - h * 0.2f, cy - h * 0.2f, h * 0.4f, h * 0.4f);
            // étoile à cinq branches, une pointe tournée vers le croissant
            float sx = cx2 + h * 0.2f + h * 0.15f, sr = h * 0.125f;
            var pts = new PointF[10];
            for (int i = 0; i < 10; i++)
            {
                double a = Math.PI + i * Math.PI / 5;
                float rad = i % 2 == 0 ? sr : sr * 0.4f;
                pts[i] = new PointF(sx + (float)(Math.Cos(a) * rad), cy + (float)(Math.Sin(a) * rad));
            }
            using (var b = new SolidBrush(Color.White)) g.FillPolygon(b, pts);
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
        const int HK_TOGGLE = 1, HK_POS = 2, HK_ROUTE = 3;
        const uint VK_F6 = 0x75, VK_F7 = 0x76, VK_F9 = 0x78;
        const int WM_INPUT = 0x00FF;
        // marque mise sur nos propres SendInput, pour les reconnaître dans le Raw Input
        const uint OwnInput = 0x424F5A;   // "BOZ"

        // ---- Raw Input (enregistrement du trajet) ----
        [DllImport("user32.dll")] static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] devices, uint count, uint size);
        [DllImport("user32.dll")] static extern uint GetRawInputData(IntPtr hRawInput, uint command, byte[] data, ref uint size, uint headerSize);
        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUTDEVICE { public ushort usUsagePage, usUsage; public uint dwFlags; public IntPtr hwndTarget; }
        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUTHEADER { public uint dwType, dwSize; public IntPtr hDevice, wParam; }

        // ---- mise en page ----
        const int W = 420, M = 16, CW = W - 2 * M, RowH = 42;
        const int KW = 23 * KeyboardView.U + 28;   // largeur du panneau clavier
        const int ACT_KEYBOARD = 5;                // action anti-AFK "Clavier"

        // ---- UI ----
        TrafficLights lights;
        Segmented segTabs, segMode, segRepeat, segPos, segKbOs, segKbMulti;
        Panel pnlClick, pnlAfk, pnlKb, pnlHarvest;
        Segmented segHarvSens;
        MacSelect cbHarvKey, cbHarvUnit;
        MacNumber numHarvWait;
        Label lblDetect, lblRoute;
        Toggle chkRoute;
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
        volatile int runMode;      // 0 = AutoClicker, 1 = Anti-AFK, 2 = Récolte
        volatile bool promptSeen;  // récolte : message vu à la dernière vérification
        // textes par mode (AutoClicker, Anti-AFK, Récolte), traduits avec T()
        static readonly string[] ModeOn = { "AutoClicker activé", "Anti-AFK activé", "Récolte activée" };
        static readonly string[] ModeOff = { "AutoClicker désactivé", "Anti-AFK désactivé", "Récolte désactivée" };
        static readonly string[] ModeBadge = { "AutoClicker actif  ·  F6", "Anti-AFK actif  ·  F6", "Récolte active  ·  F6" };
        static readonly string[] ModeStopped = { "Arrêté — {0} clics", "Arrêté — {0} actions", "Arrêté — {0} récoltes" };
        LangFlags flags;
        readonly List<KeyValuePair<Label, string>> fixedLabels = new List<KeyValuePair<Label, string>>();   // libellé → texte français
        bool detectSeen;
        string statusKey = "Prêt — F6 pour démarrer / arrêter";   // dernier statut affiché à l'arrêt (texte français)
        object statusArg;
        static readonly int[] HarvestKeys = { 0x12, 0x21, 0x22, 0x23, 0x2D, 0x39 };   // E, F, G, H, X, Espace
        static readonly int[] HarvestThresholds = { 300, 150, 60 };                  // sensibilité basse / normale / haute
        PromptDetector previewDetector;
        Route route = Route.Load();   // trajet de récolte (null = aucun)
        bool recording;               // enregistrement du trajet en cours (F9)
        Route recRoute;
        int recStart;
        bool hotKeyVariants;          // F6 / F9 aussi pris avec Alt / Ctrl / Maj (pendant un lancement ou un enregistrement)
        Rectangle visibleBounds;      // position de cette fenêtre à l'écran (vide si réduite), exclue de la détection
        readonly object boundsLock = new object();
        HashSet<int> recDown;         // touches enfoncées pendant l'enregistrement
        volatile bool harvestRoute;   // la récolte en cours suit le trajet
        int laps;                     // tours de trajet terminés
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
            lights.PinClick += delegate { TopMost = !TopMost; lights.Pinned = TopMost; lights.Invalidate(); SaveBounds(); };

            segTabs = new Segmented("AutoClicker", "Anti-AFK", "Récolte") { Size = new Size(330, 30), Location = new Point((W - 330) / 2, 50) };
            segTabs.SelectedIndexChanged += delegate
            {
                pnlClick.Visible = segTabs.SelectedIndex == 0;
                pnlAfk.Visible = segTabs.SelectedIndex == 1;
                pnlHarvest.Visible = segTabs.SelectedIndex == 2;
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

            // ===== Onglet Récolte : appuie sur E quand le message de récolte apparaît =====
            pnlHarvest = new Panel { Location = new Point(0, 94), Size = new Size(W, 372), BackColor = Theme.Bg, Visible = false };
            var h1 = MakeCard(pnlHarvest, 0, 4);
            cbHarvKey = new MacSelect("E", "F", "G", "H", "X", "Espace") { Width = 110 };
            AddRow(h1, 0, "Touche à appuyer", cbHarvKey);
            numHarvWait = new MacNumber(1, 100000, 5) { Width = 60 };
            cbHarvUnit = new MacSelect("millisecondes", "secondes") { Width = 120 };
            cbHarvUnit.SelectedIndex = 1;
            AddRow(h1, 1, "Pause après récolte", numHarvWait, cbHarvUnit);
            segHarvSens = new Segmented("Basse", "Normale", "Haute") { Width = 210 };
            segHarvSens.SelectedIndex = 1;
            AddRow(h1, 2, "Sensibilité", segHarvSens);

            lblDetect = new Label { AutoSize = false, Size = new Size(170, 28), TextAlign = ContentAlignment.MiddleRight, Font = fRow, ForeColor = Theme.Sub, Text = "pas visible" };
            AddRow(h1, 3, "Message de récolte", lblDetect);

            var h2 = MakeCard(pnlHarvest, 180, 2);
            chkRoute = new Toggle(route != null);
            AddRow(h2, 0, "Suivre un trajet enregistré", chkRoute);
            lblRoute = new Label { AutoSize = false, Size = new Size(230, 28), TextAlign = ContentAlignment.MiddleRight, Font = fRow };
            AddRow(h2, 1, "Trajet", lblRoute);
            UpdateRouteLabel();

            pnlHarvest.Controls.Add(new Label
            {
                Text = "Appuie sur la touche dès que le cadre vert du message de récolte apparaît au centre de l'écran.\n"
                     + "Trajet : en jeu, appuie sur F9, fais le tour du champ en revenant au point de départ, puis F9. "
                     + "Avec F6, le perso refait ce tour en boucle et s'arrête pour ramasser.",
                Location = new Point(M + 6, 272), MaximumSize = new Size(CW - 12, 0), AutoSize = true,
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

            flags = new LangFlags();
            flags.Location = new Point(W - M - flags.Width, 15);

            Controls.AddRange(new Control[] { lights, flags, segTabs, pnlClick, pnlAfk, pnlHarvest, pnlKb, btnStart, lblStatus });

            // traduction : on retient le texte français de chaque libellé fixe, puis on applique la langue choisie
            toast = new Overlay(false);
            badge = new Overlay(true);

            CollectFixedLabels(this);
            Lang.Changed += OnLangChanged;
            ApplyLanguage();

            UpdateLocks();

            uiTimer = new System.Windows.Forms.Timer { Interval = 200 };
            uiTimer.Tick += delegate
            {
                if (running) lblStatus.Text = StatusText();
                else if (uiRunning) ResetUi();
                UpdateDetectLabel();
                if (recording) UpdateRouteLabel();
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

        // ---------- traduction ----------
        static string T(string fr) { return Lang.T(fr); }
        static string T(string fr, params object[] args) { return Lang.T(fr, args); }

        // libellés au texte fixe ; ceux qui changent pendant l'utilisation sont remplis par leur propre code
        void CollectFixedLabels(Control root)
        {
            foreach (Control c in root.Controls)
            {
                var l = c as Label;
                if (l != null && l != lblStatus && l != lblPos && l != lblDetect && l != lblRoute && l != lblKeys && !string.IsNullOrEmpty(l.Text))
                    fixedLabels.Add(new KeyValuePair<Label, string>(l, l.Text));
                CollectFixedLabels(c);
            }
        }

        void OnLangChanged(object sender, EventArgs e) { ApplyLanguage(); }

        // statut affiché hors lancement : retenu pour pouvoir le retraduire si on change de langue
        void SetStatus(string key, object arg = null)
        {
            statusKey = key;
            statusArg = arg;
            lblStatus.Text = arg == null ? T(key) : T(key, arg);
        }

        void ApplyLanguage()
        {
            foreach (var p in fixedLabels) p.Key.Text = T(p.Value);
            btnStart.Text = T(uiRunning ? "Arrêter   (F6)" : "Démarrer   (F6)");
            if (!uiRunning) SetStatus(statusKey, statusArg);
            if (recording) badge.SetBadgeText(T("Enregistrement du trajet  ·  F9"));
            else if (uiRunning) badge.SetBadgeText(T(ModeBadge[runMode]));
            if (badge.Visible) badge.ShowBadge();   // repositionne l'indicateur (sa largeur a changé)
            if (fixedPos == Point.Empty) lblPos.Text = T("F7 pour enregistrer");
            UpdateForwardLabel();
            UpdateKeysLabel();
            UpdateRouteLabel();
            SetDetect(detectSeen);
            Invalidate(true);   // onglets, menus et clavier visuel se traduisent en se redessinant
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
                lblKeys.Text = T("aucune — clique sur le clavier");
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
            cbAfkAction.SetItem(3, (cbLayout.SelectedIndex == 0 ? "Z" : "W") + " " + T("(avancer)"));
        }

        static int DetectLayout()
        {
            string c = InputLanguage.CurrentInputLanguage.Culture.Name;
            if (c == "fr-FR" || c == "fr-BE" || c == "nl-BE" || c == "fr-MC" || c == "fr-LU") return 0;
            if (c.StartsWith("de-") || c.EndsWith("-CH") || c.StartsWith("cs-") || c.StartsWith("sk-")
                || c.StartsWith("hu-") || c.StartsWith("sl-") || c.StartsWith("hr-")) return 2;
            return 1;
        }

        // Position de la fenêtre lue par le thread de récolte : la fenêtre ne doit pas se détecter elle-même
        Rectangle VisibleBounds() { lock (boundsLock) return visibleBounds; }

        void SaveBounds()
        {
            // non épinglée, la fenêtre passe derrière le jeu : il ne faut pas exclure ce qu'il y a dessous
            lock (boundsLock) visibleBounds = WindowState == FormWindowState.Minimized || !TopMost ? Rectangle.Empty : Bounds;
        }

        protected override void OnMove(EventArgs e) { base.OnMove(e); SaveBounds(); }
        protected override void OnResize(EventArgs e) { base.OnResize(e); SaveBounds(); }

        // Onglet Récolte : montre en direct si le message est vu (pratique pour tester avant de lancer)
        void UpdateDetectLabel()
        {
            if (segTabs.SelectedIndex != 2 || WindowState == FormWindowState.Minimized) return;
            bool seen;
            if (running) seen = promptSeen;
            else
            {
                if (previewDetector == null) previewDetector = new PromptDetector { Exclude = VisibleBounds };
                seen = previewDetector.Count() >= HarvestThresholds[segHarvSens.SelectedIndex];
            }
            SetDetect(seen);
        }

        void SetDetect(bool seen)
        {
            detectSeen = seen;
            lblDetect.Text = T(seen ? "détecté" : "pas visible");
            lblDetect.ForeColor = seen ? Theme.Green : Theme.Sub;
        }

        string StatusText()
        {
            long n = Interlocked.Read(ref clicks);
            if (runMode == 0) return T("En cours — {0} clics  ·  F6 pour arrêter", n);
            if (runMode == 2)
                return T("Récolte active — {0} récoltes  ·  F6 pour arrêter", n) + "\n"
                     + (promptSeen ? T("Message détecté, récolte…")
                        : harvestRoute ? T("Trajet en cours — tour {0}", laps + 1)
                        : T("Déplace-toi vers un champignon"));
            long left = Interlocked.Read(ref stopLeftMs);
            return T("Anti-AFK actif — {0} actions  ·  prochaine dans {1}", n, Clock(Interlocked.Read(ref nextActionMs)))
                 + (left >= 0 ? "\n" + T("Arrêt automatique dans {0}", Clock(left))
                    : chkAfkIdle.Checked ? "\n" + T("Le compte à rebours repart quand tu bouges") : "");
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
            RegisterHotKeys(true);
            timeBeginPeriod(1);
        }

        // Raccourcis F6 / F7 / F9 seuls, en permanence. MOD_NOREPEAT : garder la touche enfoncée ne redéclenche pas
        // le raccourci. Identifiant = raccourci * 16 + modificateurs (MOD_ALT = 1, MOD_CONTROL = 2, MOD_SHIFT = 4).
        void RegisterHotKeys(bool on)
        {
            SetHotKey(HK_TOGGLE, VK_F6, 0, on);
            SetHotKey(HK_POS, VK_F7, 0, on);
            SetHotKey(HK_ROUTE, VK_F9, 0, on);
            if (!on) SetHotKeyVariants(false);
        }

        // Pendant un lancement ou un enregistrement, F6 et F9 marchent aussi avec Alt / Ctrl / Maj : le trajet
        // peut garder Maj enfoncé (sprint) et Windows verrait alors "Maj+F6" au lieu de F6. Le reste du temps
        // on laisse ces combinaisons aux autres applications (Ctrl+F6 dans Word, Alt+F9 de ShadowPlay…).
        void SetHotKeyVariants(bool on)
        {
            if (hotKeyVariants == on) return;
            hotKeyVariants = on;
            for (uint mods = 1; mods < 8; mods++)
            {
                SetHotKey(HK_TOGGLE, VK_F6, mods, on);
                SetHotKey(HK_ROUTE, VK_F9, mods, on);
            }
        }

        void SetHotKey(int hk, uint vk, uint mods, bool on)
        {
            const uint MOD_NOREPEAT = 0x4000;
            int id = hk * 16 + (int)mods;
            if (on) RegisterHotKey(Handle, id, mods | MOD_NOREPEAT, vk);
            else UnregisterHotKey(Handle, id);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopClicking();
            if (worker != null) worker.Join(1000);   // laisse le trajet relâcher ses touches avant de quitter
            if (recording) SetRawInput(false);
            RegisterHotKeys(false);
            Lang.Changed -= OnLangChanged;
            timeEndPeriod(1);
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32() / 16;
                if (id == HK_TOGGLE) { if (running) StopClicking(); else StartSelected(0); }
                else if (id == HK_POS)
                {
                    fixedPos = Cursor.Position;
                    segPos.SelectedIndex = 1;
                    lblPos.Text = "X " + fixedPos.X + "   Y " + fixedPos.Y;
                    lblPos.ForeColor = Theme.Text;
                }
                else if (id == HK_ROUTE) ToggleRecording();
            }
            else if (m.Msg == WM_INPUT && recording) OnRawInput(m.LParam);
            base.WndProc(ref m);
        }

        // ---------- enregistrement du trajet (F9) ----------
        // Raw Input en mode "sink" : on reçoit le clavier et les mouvements relatifs de la souris même quand
        // le jeu est au premier plan (c'est ce que lit le jeu, pas la position du curseur qui reste bloquée au centre).
        void ToggleRecording()
        {
            if (running) return;
            if (recording) { StopRecording(); return; }

            recRoute = new Route();
            recDown = new HashSet<int>();
            recStart = Environment.TickCount;
            if (!SetRawInput(true))
            {
                MessageBox.Show(this, T("Impossible d'écouter le clavier et la souris pour enregistrer le trajet."), AppName);
                return;
            }
            recording = true;
            SetHotKeyVariants(true);   // F9 doit marcher même en sprintant (Maj enfoncé)
            segTabs.SelectedIndex = 2;
            if (chkToast.Checked) toast.ShowToast(T("Enregistrement du trajet  ·  F9 pour finir"), Theme.Red);
            badge.SetBadgeText(T("Enregistrement du trajet  ·  F9"));
            if (chkBadge.Checked) badge.ShowBadge();
            UpdateRouteLabel();
        }

        void StopRecording()
        {
            recording = false;
            SetRawInput(false);
            SetHotKeyVariants(false);
            int end = Environment.TickCount - recStart;
            badge.Hide();

            // F9 appuyé deux fois de suite par erreur : on garde l'ancien trajet plutôt que de l'écraser
            bool moved = recRoute.Events.Exists(e => e.Kind != Route.KeyUp);
            if (end < 1000 || !moved)
            {
                if (chkToast.Checked) toast.ShowToast(T("Trajet trop court, ancien trajet conservé"), Theme.Red);
                UpdateRouteLabel();
                return;
            }

            foreach (int k in recDown) recRoute.Events.Add(new Route.Ev(end, Route.KeyUp, k, 0));   // touches encore enfoncées
            recRoute.Duration = end;
            route = recRoute;
            route.Save();
            chkRoute.Checked = true;
            if (!recRoute.Events.Exists(e => e.Kind == Route.KeyDown))   // seulement la souris : le perso ne bougera pas
                MessageBox.Show(this, T("Aucune touche n'a été enregistrée, seulement la souris : le perso tournera sur place sans avancer."), AppName);
            else if (chkToast.Checked) toast.ShowToast(T("Trajet enregistré  ·  {0}", Clock(end)), Theme.Green);
            UpdateRouteLabel();
        }

        bool SetRawInput(bool on)
        {
            const uint RIDEV_REMOVE = 0x1, RIDEV_INPUTSINK = 0x100;
            var d = new RAWINPUTDEVICE[2];
            d[0].usUsagePage = 1; d[0].usUsage = 2;   // souris
            d[1].usUsagePage = 1; d[1].usUsage = 6;   // clavier
            for (int i = 0; i < 2; i++)
            {
                d[i].dwFlags = on ? RIDEV_INPUTSINK : RIDEV_REMOVE;
                d[i].hwndTarget = on ? Handle : IntPtr.Zero;
            }
            return RegisterRawInputDevices(d, 2, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
        }

        void OnRawInput(IntPtr hRaw)
        {
            const uint RID_INPUT = 0x10000003;
            uint hdr = (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)), size = 0;
            GetRawInputData(hRaw, RID_INPUT, null, ref size, hdr);
            if (size < hdr) return;
            var buf = new byte[size];
            if (GetRawInputData(hRaw, RID_INPUT, buf, ref size, hdr) != size) return;

            uint type = BitConverter.ToUInt32(buf, 0);
            int o = (int)hdr, t = Environment.TickCount - recStart;
            var ev = recRoute.Events;

            // On n'ignore que nos propres entrées simulées : celles des claviers / souris gaming passent souvent
            // par un logiciel (Razer, Logitech G Hub, iCUE, AutoHotkey…) et arrivent sans périphérique (device = 0).
            if (type == 0 && size >= o + 24)   // RIM_TYPEMOUSE
            {
                if (BitConverter.ToUInt32(buf, o + 20) == OwnInput) return;
                ushort flags = BitConverter.ToUInt16(buf, o);
                if ((flags & 1) != 0) return;   // mouvement absolu (bureau à distance, tablette) : ignoré
                int dx = BitConverter.ToInt32(buf, o + 12), dy = BitConverter.ToInt32(buf, o + 16);
                if (dx == 0 && dy == 0) return;   // clics et molette : pas enregistrés (clic gauche = frapper dans GTA)
                // mouvements regroupés par tranches de 10 ms pour garder un fichier léger
                int last = ev.Count - 1;
                if (last >= 0 && ev[last].Kind == Route.Move && ev[last].T / 10 == t / 10)
                    ev[last] = new Route.Ev(ev[last].T, Route.Move, ev[last].A + dx, ev[last].B + dy);
                else
                    ev.Add(new Route.Ev(t, Route.Move, dx, dy));
            }
            else if (type == 1 && size >= o + 16)   // RIM_TYPEKEYBOARD
            {
                if (BitConverter.ToUInt32(buf, o + 12) == OwnInput) return;
                ushort make = BitConverter.ToUInt16(buf, o), kf = BitConverter.ToUInt16(buf, o + 2);
                if (make == 0 || make == 0xFF) return;
                int code = make | ((kf & 2) != 0 ? 0xE000 : 0);
                // F6, F7, F9 (raccourcis du programme) et la touche de récolte ne font pas partie du trajet
                if (code == 0x40 || code == 0x41 || code == 0x43 || code == HarvestKeys[cbHarvKey.SelectedIndex]) return;
                if ((kf & 1) != 0) { if (recDown.Remove(code)) ev.Add(new Route.Ev(t, Route.KeyUp, code, 0)); }
                else if (recDown.Add(code)) ev.Add(new Route.Ev(t, Route.KeyDown, code, 0));   // ignore la répétition auto
            }
        }

        void UpdateRouteLabel()
        {
            if (recording)
            {
                lblRoute.Text = T("enregistrement… {0}  ·  F9", Clock(Environment.TickCount - recStart));
                lblRoute.ForeColor = Theme.Red;
            }
            else if (route == null)
            {
                lblRoute.Text = T("aucun  ·  F9 pour enregistrer");
                lblRoute.ForeColor = Theme.Sub;
            }
            else
            {
                lblRoute.Text = T("{0}  ·  F9 pour refaire", Clock(route.Duration));
                lblRoute.ForeColor = Theme.Text;
            }
        }

        void StartSelected(int startDelayMs)
        {
            if (running || recording) return;
            // F6 deux fois très vite : on attend que l'ancien lancement soit vraiment fini,
            // sinon il continuerait à tourner (et à appuyer sur des touches) à côté du nouveau
            if (worker != null && worker.IsAlive && !worker.Join(500))
            {
                SetStatus("Arrêt en cours… réessaie dans un instant");
                return;
            }
            if (segTabs.SelectedIndex == 1) StartAfk(startDelayMs);
            else if (segTabs.SelectedIndex == 2) StartHarvest(startDelayMs);
            else StartClicking(startDelayMs);
        }

        void StartHarvest(int startDelayMs)
        {
            int key = HarvestKeys[cbHarvKey.SelectedIndex];
            long waitMs = (long)numHarvWait.Value * (cbHarvUnit.SelectedIndex == 1 ? 1000 : 1);
            int minPx = HarvestThresholds[segHarvSens.SelectedIndex];
            if (chkRoute.Checked && route == null)
            {
                MessageBox.Show(this, T("Enregistre d'abord un trajet : en jeu, appuie sur F9, fais le tour du champ, puis F9 à nouveau."), AppName);
                return;
            }
            Route r = chkRoute.Checked ? route : null;

            BeginRun(2, startDelayMs);
            promptSeen = false;
            harvestRoute = r != null;
            laps = 0;
            // la fenêtre est toujours au premier plan : elle ne doit pas cacher le message à détecter
            if (previewDetector == null) previewDetector = new PromptDetector { Exclude = VisibleBounds };
            if (Bounds.IntersectsWith(previewDetector.Zone)) WindowState = FormWindowState.Minimized;

            worker = new Thread(delegate () { HarvestLoop(key, waitMs, minPx, r, startDelayMs); });
            worker.IsBackground = true;
            worker.Start();
        }

        void StartClicking(int startDelayMs)
        {
            if (segPos.SelectedIndex == 1 && fixedPos == Point.Empty)
            {
                MessageBox.Show(this, T("Place la souris puis appuie sur F7 pour enregistrer le point fixe."), AppName);
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

            BeginRun(0, startDelayMs);
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
                MessageBox.Show(this, T("Choisis au moins une touche sur le clavier."), AppName);
                return;
            }
            long[] mult = { 1, 1000, 60000 };   // millisecondes, secondes, minutes
            long holdMs = (long)numKeyHold.Value * mult[cbHoldUnit.SelectedIndex];
            long gapMs = (long)numKeyGap.Value * mult[cbGapUnit.SelectedIndex];
            bool onlyIdle = chkAfkIdle.Checked;
            long stopAfterMs = cbAfkStop.SelectedIndex == 0 ? 0 : (long)numAfkStop.Value * (cbAfkStop.SelectedIndex == 1 ? 60000 : 3600000);

            BeginRun(1, startDelayMs);
            Interlocked.Exchange(ref nextActionMs, ms);
            Interlocked.Exchange(ref stopLeftMs, stopAfterMs > 0 ? stopAfterMs : -1);
            worker = new Thread(delegate () { AfkLoop(ms, action, keys, holdMs, gapMs, onlyIdle, stopAfterMs, startDelayMs); });
            worker.IsBackground = true;
            worker.Start();
        }

        void BeginRun(int mode, int startDelayMs)
        {
            runMode = mode;
            clicks = 0;
            stopEvent.Reset();
            SetHotKeyVariants(true);   // F6 doit pouvoir arrêter même si le trajet garde Maj enfoncé
            running = true;
            uiRunning = true;
            btnStart.Text = T("Arrêter   (F6)");
            btnStart.Fill = Theme.Red;
            UpdateLocks();
            if (startDelayMs > 0) lblStatus.Text = T("Démarrage dans {0} s…", startDelayMs / 1000);

            if (chkToast.Checked) toast.ShowToast(T(ModeOn[mode]), Theme.Green);
            badge.SetBadgeText(T(ModeBadge[mode]));
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
            if (!recording) SetHotKeyVariants(false);
            btnStart.Text = T("Démarrer   (F6)");
            btnStart.Fill = Theme.Accent;
            UpdateLocks();
            SetStatus(ModeStopped[runMode], Interlocked.Read(ref clicks));

            badge.Hide();
            if (chkToast.Checked) toast.ShowToast(T(ModeOff[runMode]), Theme.Red);
        }

        // Verrouille les réglages pendant que ça tourne, et grise les champs inutiles
        void UpdateLocks()
        {
            bool run = uiRunning;
            segTabs.Locked = run;
            LockAll(pnlClick, run);
            LockAll(pnlAfk, run);
            LockAll(pnlHarvest, run);
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

        // Récolte : appuie sur la touche quand le message apparaît. Avec un trajet, le perso le refait en boucle
        // et s'arrête le temps de ramasser ; le trajet reprend ensuite là où il en était.
        void HarvestLoop(int key, long waitMs, int minPx, Route route, int startDelayMs)
        {
            if (startDelayMs > 0 && stopEvent.WaitOne(startDelayMs)) return;

            var held = new HashSet<int>();   // touches du trajet actuellement enfoncées
            int start = Environment.TickCount;
            System.IO.StreamWriter log = null;
            try
            {
                // journal de diagnostic, réécrit à chaque lancement : ce que le détecteur voit, quand on appuie, etc.
                System.IO.Directory.CreateDirectory(Route.DataDir);
                log = new System.IO.StreamWriter(System.IO.Path.Combine(Route.DataDir, "recolte.log"), false) { AutoFlush = true };
                log.WriteLine("début  trajet=" + (route != null ? route.Duration + " ms, " + route.Events.Count + " événements" : "non")
                            + "  seuil=" + minPx + "  pause=" + waitMs + " ms");
            }
            catch { log = null; }
            Action<string> Log = s => { if (log != null) try { log.WriteLine((Environment.TickCount - start).ToString().PadLeft(7) + " ms  " + s); } catch { } };

            using (var det = new PromptDetector { Exclude = VisibleBounds })
            {
                int seen = 0, idx = 0;
                int t0 = Environment.TickCount;   // début du tour en cours, décalé de la durée des pauses de récolte
                int nextCheck = t0;
                while (running)
                {
                    int now = Environment.TickCount;
                    if (route != null)
                    {
                        int t = now - t0;
                        while (idx < route.Events.Count && route.Events[idx].T <= t) PlayEvent(route.Events[idx++], held);
                        if (idx >= route.Events.Count && t >= route.Duration)
                        {
                            ReleaseAll(held);   // fin du tour : on repart du début
                            idx = 0;
                            t0 = now;
                            Interlocked.Increment(ref laps);
                            Log("fin du tour " + laps);
                        }
                    }

                    // sans trajet on vérifie à chaque tour de boucle (toutes les 120 ms) ; avec trajet toutes les 50 ms,
                    // car en passant à côté d'un champignon le message ne reste affiché qu'un court instant
                    if (route == null || now - nextCheck >= 0)
                    {
                        nextCheck = now + 50;
                        int px = det.Count();
                        bool vis = px >= minPx;
                        if (px > 0) Log("vu " + px + " px" + (vis ? "" : " (sous le seuil)"));
                        promptSeen = vis;
                        seen = vis ? seen + 1 : 0;

                        // vu 2 fois de suite : on évite de réagir à une image isolée
                        if (seen >= 2)
                        {
                            seen = 0;
                            int pauseStart = Environment.TickCount;
                            var resume = new List<int>(held);
                            ReleaseAll(held);                 // le perso s'arrête pour ramasser
                            if (route != null)
                            {
                                // laisse le perso s'arrêter : en appuyant pendant qu'il glisse encore, il dépasse le champignon
                                if (Wait(300)) break;
                                Log("arrêté, encore visible : " + det.Count() + " px");
                            }
                            PressKey(key, 80);
                            Log("appui sur la touche de récolte");
                            Interlocked.Increment(ref clicks);
                            if (Wait(waitMs)) break;          // laisse l'animation de ramassage se finir
                            foreach (int k in resume) { SendKey(k, false); held.Add(k); }
                            int paused = Environment.TickCount - pauseStart;
                            t0 += paused;
                            nextCheck += paused;
                            continue;
                        }
                    }
                    if (stopEvent.WaitOne(route != null ? 2 : 120)) break;
                }
            }
            ReleaseAll(held);   // jamais de touche laissée enfoncée dans le jeu
            Log("arrêt  " + Interlocked.Read(ref clicks) + " récoltes");
            if (log != null) log.Dispose();
            running = false;
        }

        static void PlayEvent(Route.Ev e, HashSet<int> held)
        {
            if (e.Kind == Route.Move) MoveRel(e.A, e.B);
            else if (e.Kind == Route.KeyDown) { SendKey(e.A, false); held.Add(e.A); }
            else { SendKey(e.A, true); held.Remove(e.A); }
        }

        static void ReleaseAll(HashSet<int> held)
        {
            foreach (int k in held) SendKey(k, true);
            held.Clear();
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
            inputs[0].type = 0; inputs[0].u.mi.dwFlags = down; inputs[0].u.mi.dwExtraInfo = (IntPtr)OwnInput;
            inputs[1].type = 0; inputs[1].u.mi.dwFlags = up; inputs[1].u.mi.dwExtraInfo = (IntPtr)OwnInput;
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
            inputs[0].u.mi.dwExtraInfo = (IntPtr)OwnInput;
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
            input[0].u.ki.dwExtraInfo = (IntPtr)OwnInput;
            SendInput(1, input, Marshal.SizeOf(typeof(INPUT)));
        }

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var s = new SplashForm()) s.ShowDialog();   // écran d'ouverture animé
            CertTrust.MaybePrompt();                           // au 1er lancement : demande l'accord pour être reconnu
            Application.Run(new MainForm());
        }
    }

    // =====================================================================
    //  Écran d'ouverture animé (affiché à chaque lancement)
    // =====================================================================
    public class SplashForm : Form
    {
        readonly System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        readonly System.Windows.Forms.Timer timer;
        readonly Bitmap logo;
        const int Win = 460, Hin = 290;
        const int FadeIn = 320, Hold = 1480, FadeOut = 360;   // durées (ms)
        const int Total = FadeIn + Hold + FadeOut;

        public SplashForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(Win, Hin);
            BackColor = Color.FromArgb(18, 18, 20);
            DoubleBuffered = true;
            Opacity = 0;
            Region = new Region(Theme.Round(new RectangleF(0, 0, Win, Hin), 22));
            try { logo = Icon.ExtractAssociatedIcon(Application.ExecutablePath).ToBitmap(); } catch { logo = null; }

            timer = new System.Windows.Forms.Timer { Interval = 15 };
            timer.Tick += delegate { Tick(); };
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            sw.Start();
            timer.Start();
        }

        void Tick()
        {
            int t = (int)sw.ElapsedMilliseconds;
            Opacity = t < FadeIn ? (double)t / FadeIn
                    : t > FadeIn + Hold ? Math.Max(0, 1.0 - (double)(t - FadeIn - Hold) / FadeOut)
                    : 1;
            if (t >= Total) { timer.Stop(); Close(); return; }
            Invalidate();
        }

        static float EaseOut(float x) { return 1 - (float)Math.Pow(1 - x, 3); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Theme.Hq(g);
            int t = (int)sw.ElapsedMilliseconds;
            var full = new RectangleF(0, 0, Win, Hin);

            // fond dégradé diagonal
            using (var bg = new LinearGradientBrush(new Rectangle(0, 0, Win, Hin),
                        Color.FromArgb(26, 26, 30), Color.FromArgb(14, 14, 16), 60f))
                g.FillPath(bg, Theme.Round(full, 22));
            // liseré intérieur
            using (var pen = new Pen(Color.FromArgb(60, 255, 255, 255)))
                g.DrawPath(pen, Theme.Round(new RectangleF(0.5f, 0.5f, Win - 1, Hin - 1), 22));

            float cx = Win / 2f, cy = 104;

            // halo + anneaux pulsés derrière le logo
            float pulse = (t % 1400) / 1400f;
            for (int i = 0; i < 2; i++)
            {
                float p = (pulse + i * 0.5f) % 1f;
                float rad = 42 + p * 46;
                int a = (int)(110 * (1 - p));
                using (var pen = new Pen(Color.FromArgb(a, Theme.Accent), 2f))
                    g.DrawEllipse(pen, cx - rad, cy - rad, rad * 2, rad * 2);
            }
            using (var glow = new PathGradientBrush(Ellipse(cx, cy, 70)))
            {
                glow.CenterColor = Color.FromArgb(120, Theme.Accent);
                glow.SurroundColors = new[] { Color.FromArgb(0, Theme.Accent) };
                g.FillEllipse(glow, cx - 70, cy - 70, 140, 140);
            }

            // logo avec petit effet de zoom à l'entrée
            if (logo != null)
            {
                float pop = EaseOut(Math.Min(1f, t / 420f));
                float sz = 68 * (0.7f + 0.3f * pop);
                var st = g.Save();
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(logo, cx - sz / 2, cy - sz / 2, sz, sz);
                g.Restore(st);
            }

            // titre avec reflet qui balaie
            var tr = new RectangleF(0, 168, Win, 40);
            using (var f = new Font("Segoe UI Semibold", 25f))
            {
                Theme.DrawText(g, "AutoClicker", f, Theme.Text, tr, StringAlignment.Center);
                using (var path = new GraphicsPath())
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    path.AddString("AutoClicker", f.FontFamily, (int)FontStyle.Bold, 34, tr, sf);
                    float shine = ((t % 1600) / 1600f) * (Win + 200) - 100;
                    var st = g.Save();
                    g.SetClip(path);
                    using (var sb = new LinearGradientBrush(new RectangleF(shine - 60, 160, 120, 56),
                                Color.FromArgb(0, 255, 255, 255), Color.FromArgb(170, 255, 255, 255), 0f))
                        g.FillRectangle(sb, shine - 60, 160, 120, 56);
                    g.Restore(st);
                }
            }
            using (var f = new Font("Segoe UI", 10.5f))
                Theme.DrawText(g, "by BOZYILDIZ", f, Theme.Accent, new RectangleF(0, 206, Win, 22), StringAlignment.Center);

            // barre de progression
            float bw = 240, bx = cx - bw / 2, by = 244;
            using (var b = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
                Theme.FillRound(g, Color.FromArgb(40, 255, 255, 255), new RectangleF(bx, by, bw, 4), 2);
            float prog = EaseOut(Math.Min(1f, Math.Max(0, (t - 200f) / (Total - 500f))));
            if (prog > 0)
            {
                var fill = new RectangleF(bx, by, bw * prog, 4);
                using (var lg = new LinearGradientBrush(new RectangleF(bx, by, bw, 4),
                            Theme.Accent, Color.FromArgb(94, 92, 230), 0f))
                    g.FillPath(lg, Theme.Round(fill, 2));
            }
        }

        static GraphicsPath Ellipse(float cx, float cy, float r)
        {
            var p = new GraphicsPath();
            p.AddEllipse(cx - r, cy - r, r * 2, r * 2);
            return p;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { timer.Dispose(); if (logo != null) logo.Dispose(); }
            base.Dispose(disposing);
        }
    }

    // =====================================================================
    //  Confiance du certificat : rend l'app "reconnue" sur le PC, avec l'accord
    //  explicite de l'utilisateur (demandé une seule fois). Windows affiche en plus
    //  son propre avertissement de sécurité : rien ne se fait en douce.
    // =====================================================================
    public static class CertTrust
    {
        // Partie PUBLIQUE du certificat BOZYILDIZ (aucun secret ici).
        const string CerB64 =
            "MIIEIDCCAoigAwIBAgIQK+BUhRvmia1B8lsJ2UhvVTANBgkqhkiG9w0BAQsFADAoMRIwEAYDVQQKDAlCT1pZSUxESVoxEjAQBgNVBAMMCUJPWllJTERJWjAeFw0yNjEwMzAxNjE1MTZaFw0zNjEwMzAxNjI1MTZaMCgxEjAQBgNVBAoMCUJPWllJTERJWjESMBAGA1UEAwwJQk9aWUlMRElaMIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEAqFVnxLDZ3U9iOhFdRwaJp833tRk6VoMAN6U9j+8Z9ijO16W/WDN317X6m4GDveO5g+pTW4ISgrf1jOhTx6OBK/ElidxLV5fBcz+ESneEVIRC1kC1ksX9lyvC4Zt3ZVhpBB9yUcjPG6kSVtCK/Yj8O2o8oR9JP6zL/DVVESbN/GULEd6R9DtTtZ8q3cu848Qx79Yk/XHtR9V8YmUoqIdVHm7Cqid+SNbompSvFKlY1sapakdu1ZDyJ50PdFwdeBHZd3O1qadd/4vZ+V4Pb6oYDgVnINSfHcJzPlZTZquo7QDH70UEWh4/9Aurm9wl2hUKpD3OMu2CVZfzdph7dfvU//7/SISesYczPXhobqnSznfeXCpvpAKE9Kwj8Dge0Ypyi0lSlY7SsfmLpyBZhLE7JYZMBaj5/uaRDSCr2RKiLRpFrFheYc5ypZ6mvR4j5UAqncxIvXwW00eoDzWiA3/FiMHxgQ7QS4pk0a/kLpqXfxLP+gGKgz5DVpUkgNUycm2xAgMBAAGjRjBEMA4GA1UdDwEB/wQEAwIHgDATBgNVHSUEDDAKBggrBgEFBQcDAzAdBgNVHQ4EFgQU4H6nZSkT1IF+RE82VqSKRLnvzBIwDQYJKoZIhvcNAQELBQADggGBAGRYsLg1cji71T9PXQQX/BIr9KE0tEO0LScfS5rGzGHZlwiePIFrAajpMiNTm35QOadnGjwJHefGq91sjZa6VFZsYRDpwxV6C5DlUZWQdyFddK3ki0tdvkpT0KjRNVBHP8V0mwtkyBvm/lNlaGPRIJC7K2fg1CGW+VlSZiKjOTpaE1f5QL+kpqUC8wUGJymp3OZbndF4MQnO/fCbF0TVg2smpNy5CQyvEBXAm3Tl30qnICWcz2Lx5RC9RSMqzmTbb1f6n4q7A+9UwEOS209gldLI6MRc9tAPZb+AgmkOAkHhc3kpFTR3kTvtXV7/dPUW0uNapr+H0FAEZJZBuf/0fhNx5FC4cCNLT8O+ngqfF5LmbvRPxa4NpfhbbYeRsChBU8SDfbBopBiPQpH4TOkYd2Id0S3tq3rOkQ2rbcnSggMQbwIYgfUtdI/hIbzwXshAYn8wBVxBcljAisPjsjwn2zYhIWUpJoEejR7dAIOX64XkSAWDfVC9u1ZlxRHP0yzmrg==";

        static string SkipFlag { get { return System.IO.Path.Combine(Route.DataDir, "cert-skip"); } }

        static X509Certificate2 Cert() { return new X509Certificate2(Convert.FromBase64String(CerB64)); }

        static bool IsTrusted()
        {
            try
            {
                using (var cert = Cert())
                using (var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadOnly);
                    return store.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, false).Count > 0;
                }
            }
            catch { return true; }   // en cas de doute on n'embête pas l'utilisateur
        }

        public static void MaybePrompt()
        {
            try
            {
                if (IsTrusted() || System.IO.File.Exists(SkipFlag)) return;
                using (var dlg = new CertConsentForm())
                {
                    var r = dlg.ShowDialog();
                    if (r == DialogResult.OK) Install();
                    else if (dlg.DontAsk)
                    {
                        try { System.IO.Directory.CreateDirectory(Route.DataDir); System.IO.File.WriteAllText(SkipFlag, ""); } catch { }
                    }
                }
            }
            catch { }
        }

        // Ajoute le certificat public aux magasins de l'utilisateur courant (pas besoin d'admin).
        // Le magasin "Racine" déclenche l'avertissement de sécurité de Windows : l'utilisateur confirme.
        static void Install()
        {
            foreach (StoreName name in new[] { StoreName.Root, StoreName.TrustedPublisher })
            {
                try
                {
                    using (var cert = Cert())
                    using (var store = new X509Store(name, StoreLocation.CurrentUser))
                    {
                        store.Open(OpenFlags.ReadWrite);
                        store.Add(cert);
                    }
                }
                catch { }   // refus de l'avertissement Windows, etc. : on n'insiste pas
            }
        }
    }

    // Fenêtre d'accord (style sombre), au 1er lancement uniquement.
    public class CertConsentForm : Form
    {
        public bool DontAsk { get; private set; }

        public CertConsentForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(440, 300);
            BackColor = Theme.Bg;
            Font = Theme.F(10f);
            Region = new Region(Theme.Round(new RectangleF(0, 0, 440, 300), 16));
            Shown += delegate { Theme.MacWindow(Handle); };

            var title = new Label { Text = Lang.T("Rendre AutoClicker reconnu sur ce PC ?"), ForeColor = Theme.Text,
                Font = Theme.FS(13f), AutoSize = false, Bounds = new Rectangle(24, 24, 392, 28) };
            var body = new Label { ForeColor = Theme.Sub, AutoSize = false, Bounds = new Rectangle(24, 60, 392, 118),
                Text = Lang.T("Windows ne connaît pas encore cette application et peut l'afficher comme « éditeur inconnu ». "
                    + "Cette étape ajoute le certificat de BOZYILDIZ pour qu'AutoClicker soit reconnu sur ce PC.\n\n"
                    + "Windows affichera un avertissement de sécurité : choisis « Oui » pour confirmer. "
                    + "Tu peux refuser, l'application fonctionnera quand même.") };

            var chk = new CheckBox { Text = Lang.T("Ne plus me demander"), ForeColor = Theme.Sub, AutoSize = true,
                Location = new Point(24, 188), FlatStyle = FlatStyle.Flat };
            chk.CheckedChanged += delegate { DontAsk = chk.Checked; };

            var ok = new MacButton(Lang.T("Rendre reconnu"), Theme.Accent) { Bounds = new Rectangle(214, 240, 202, 40) };
            var later = new MacButton(Lang.T("Plus tard"), Theme.Field) { Bounds = new Rectangle(24, 240, 180, 40) };
            ok.Click += delegate { DialogResult = DialogResult.OK; Close(); };
            later.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(title); Controls.Add(body); Controls.Add(chk); Controls.Add(ok); Controls.Add(later);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(Color.FromArgb(50, 255, 255, 255)))
                e.Graphics.DrawPath(pen, Theme.Round(new RectangleF(0.5f, 0.5f, 439, 299), 16));
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
