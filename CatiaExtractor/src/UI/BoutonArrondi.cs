using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

// Utilitaires GDI+ partagés entre les composants visuels.
internal static class GraphicsUtils
{
    public static GraphicsPath RoundedRect(Rectangle b, int r)
    {
        int d = r * 2;
        var p = new GraphicsPath();
        p.AddArc(b.X,         b.Y,          d, d, 180, 90);
        p.AddArc(b.Right - d, b.Y,          d, d, 270, 90);
        p.AddArc(b.Right - d, b.Bottom - d, d, d,   0, 90);
        p.AddArc(b.X,         b.Bottom - d, d, d,  90, 90);
        p.CloseFigure();
        return p;
    }

    public static void SetSmooth(Graphics g)
    {
        g.SmoothingMode        = SmoothingMode.AntiAlias;
        g.TextRenderingHint    = TextRenderingHint.ClearTypeGridFit;
        g.InterpolationMode    = InterpolationMode.HighQualityBicubic;
    }
}

// Bouton avec coins arrondis et transition de couleur au survol.
public class BoutonArrondi : Control
{
    private static readonly Color CouleurOff = Color.FromArgb(0xB8, 0xC4, 0xCE);

    public Color CouleurNormale { get; set; } = Color.FromArgb(0x0A, 0x2A, 0x4A);
    public Color CouleurSurvol  { get; set; } = Color.FromArgb(0x1E, 0x5F, 0x8C);
    public Color CouleurPresse  { get; set; } = Color.FromArgb(0x06, 0x18, 0x2C);
    public int   Rayon          { get; set; } = 8;

    private Color _actuelle;

    public BoutonArrondi()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
        _actuelle = CouleurNormale;
        Cursor    = Cursors.Hand;
        Font      = new Font("Segoe UI", 9.5f);
        ForeColor = Color.White;
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        base.OnEnabledChanged(e);
        _actuelle = Enabled ? CouleurNormale : CouleurOff;
        Cursor    = Enabled ? Cursors.Hand : Cursors.Default;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (!Enabled) return;
        _actuelle = CouleurSurvol;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _actuelle = Enabled ? CouleurNormale : CouleurOff;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (!Enabled || e.Button != MouseButtons.Left) return;
        _actuelle = CouleurPresse;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _actuelle = Enabled ? CouleurSurvol : CouleurOff;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        GraphicsUtils.SetSmooth(g);

        using var path  = GraphicsUtils.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Rayon);
        using var brush = new SolidBrush(_actuelle);
        g.FillPath(brush, path);

        var sf = new StringFormat
        {
            Alignment     = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        using var tb = new SolidBrush(Enabled ? ForeColor : Color.FromArgb(0xEC, 0xEF, 0xF2));
        g.DrawString(Text, Font, tb, new RectangleF(0, 0, Width, Height), sf);
    }
}
