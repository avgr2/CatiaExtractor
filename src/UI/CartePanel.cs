using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

// Panneau avec coins arrondis et bordure fine — sert de "carte" dans l'interface.
public class CartePanel : Panel
{
    public int Rayon { get; set; } = 10;

    public CartePanel()
    {
        BackColor = Color.White;
        Padding   = new Padding(14);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (Width < 1 || Height < 1) return;
        using var path = GraphicsUtils.RoundedRect(new Rectangle(0, 0, Width, Height), Rayon);
        Region = new Region(path);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen  = new Pen(Color.FromArgb(0xE2, 0xE8, 0xF0));
        using var path = GraphicsUtils.RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), Rayon);
        g.DrawPath(pen, path);
    }
}
