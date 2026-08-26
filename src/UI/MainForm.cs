using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using INFITF;

public class MainForm : Form
{
    // ── Palette IPSA ─────────────────────────────────────────────────────────
    private static readonly Color CouleurBleufonce  = Color.FromArgb(0x0A, 0x2A, 0x4A);
    private static readonly Color CouleurBleuAccent = Color.FromArgb(0x1E, 0x5F, 0x8C);
    private static readonly Color CouleurFond       = Color.FromArgb(0xF5, 0xF7, 0xFA);
    private static readonly Color CouleurTexte      = Color.FromArgb(0x2C, 0x3E, 0x50);
    private static readonly Color CouleurErreur     = Color.FromArgb(0xC0, 0x39, 0x2B);
    private static readonly Color CouleurLigneAlt   = Color.FromArgb(0xF0, 0xF5, 0xFD);

    // ── Polices ───────────────────────────────────────────────────────────────
    private static readonly Font PoliceGenerale  = new Font("Segoe UI", 9.5f);
    private static readonly Font PoliceSemiGras  = new Font("Segoe UI", 10f, FontStyle.Bold);
    private static readonly Font PoliceTitre     = new Font("Segoe UI", 17f, FontStyle.Bold);
    private static readonly Font PoliceFooter    = new Font("Segoe UI", 8f);
    private static readonly Font PoliceMonospace = new Font("Consolas", 9f);
    private static readonly Font PolicePetite    = new Font("Segoe UI", 8f);

    // ── Hauteurs du panel principal ───────────────────────────────────────────
    // top_pad(14) + dépôt(100) + gap(12) + fichiers(150) + gap(12) + bouton(52) + bottom_gap(14)
    private const int HauteurAvecFichiers = 354;
    // top_pad(14) + dépôt(100) + gap(12) + bouton(52) + bottom_gap(14)
    private const int HauteurSansFichiers = 192;

    // ── État ─────────────────────────────────────────────────────────────────
    private readonly INFITF.Application _catia;
    private readonly List<string>       _fichiersSelectionnes = new();
    private bool                        _extractionEnCours;
    private bool                        _dragSurvol;

    // ── Contrôles clés ────────────────────────────────────────────────────────
    private FlowLayoutPanel  _flowFichiers;
    private BoutonArrondi    _btnExtraire;
    private RichTextBox      _txtLog;
    private Label            _lblStatut;
    private Panel            _zoneDepot;
    private CartePanel       _carteFichiers;
    private Panel            _outerPanelPrincipal;
    private TableLayoutPanel _innerTableLayout;

    public MainForm(INFITF.Application catia)
    {
        _catia = catia;
        InitialiserComposants();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Construction
    // ═════════════════════════════════════════════════════════════════════════

    private void InitialiserComposants()
    {
        Text          = "Extracteur CATIA — IPSA";
        Size          = new Size(980, 740);
        MinimumSize   = new Size(760, 560);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor     = CouleurFond;
        Font          = PoliceGenerale;
        ForeColor     = CouleurTexte;

        // Ordre : Fill → Bottom → Top (dernier ajouté = priorité haute pour les Top)
        Controls.Add(ConstruireZoneLog());
        Controls.Add(ConstruireFooter());
        Controls.Add(ConstruirePanelPrincipal());
        Controls.Add(ConstruireHeader());

        FormClosing += (s, e) =>
        {
            try { CatiaConnector.Deconnecter(_catia); }
            catch (Exception ex) { Console.WriteLine($"Erreur déconnexion CATIA : {ex.Message}"); }
        };
    }

    // ── Bandeau supérieur ─────────────────────────────────────────────────────

    private Panel ConstruireHeader()
    {
        var panel = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = CouleurBleufonce };

        int xTitre = 18;
        string logoPath = TrouverFichier(Path.Combine("assets", "ipsa_logo.png"));
        if (logoPath != null)
        {
            try
            {
                var pic = new PictureBox
                {
                    Image = Image.FromFile(logoPath), SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(56, 56), Location = new Point(16, 16), BackColor = Color.Transparent
                };
                panel.Controls.Add(pic);
                xTitre = 86;
            }
            catch { }
        }

        var lbl = new Label
        {
            Text = "Extracteur CATIA", ForeColor = Color.White, Font = PoliceTitre,
            AutoSize = true, BackColor = Color.Transparent
        };
        lbl.Location = new Point(xTitre, (panel.Height - lbl.PreferredHeight) / 2);

        var sub = new Label
        {
            Text = "IPSA — Ingénieur de l'Air et de l'Espace", ForeColor = Color.FromArgb(160, 200, 230),
            Font = new Font("Segoe UI", 8.5f), AutoSize = true, BackColor = Color.Transparent
        };
        sub.Location = new Point(xTitre, lbl.Location.Y + lbl.PreferredHeight + 2);

        panel.Controls.Add(lbl);
        panel.Controls.Add(sub);
        return panel;
    }

    // ── Pied de page ─────────────────────────────────────────────────────────

    private Panel ConstruireFooter()
    {
        var panel = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = CouleurFond };
        panel.Paint += (s, e) =>
            e.Graphics.DrawLine(new Pen(Color.FromArgb(0xD8, 0xDF, 0xE8)), 0, 0, panel.Width, 0);
        panel.Controls.Add(new Label
        {
            Text = $"IPSA  ·  Extracteur CATIA  ·  {DateTime.Now.Year}",
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(0xA8, 0xB2, 0xBE), Font = PoliceFooter
        });
        return panel;
    }

    // ── Panel principal (cartes) ──────────────────────────────────────────────

    private Panel ConstruirePanelPrincipal()
    {
        _outerPanelPrincipal = new Panel { Dock = DockStyle.Top, BackColor = CouleurFond };
        _outerPanelPrincipal.Height = HauteurSansFichiers;

        _innerTableLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5,
            BackColor = CouleurFond, Padding = new Padding(20, 14, 20, 0)
        };
        _innerTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _innerTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100)); // dépôt
        _innerTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));  // espacement
        _innerTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));   // fichiers (masqué initialement)
        _innerTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));   // espacement (masqué initialement)
        _innerTableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));  // bouton

        _carteFichiers = ConstruireCarteFichiers();
        _carteFichiers.Visible = false;

        _innerTableLayout.Controls.Add(ConstruireZoneDepot(),   0, 0);
        _innerTableLayout.Controls.Add(new Panel { BackColor = CouleurFond }, 0, 1);
        _innerTableLayout.Controls.Add(_carteFichiers,           0, 2);
        _innerTableLayout.Controls.Add(new Panel { BackColor = CouleurFond }, 0, 3);
        _innerTableLayout.Controls.Add(ConstruireBarreAction(),  0, 4);

        _outerPanelPrincipal.Controls.Add(_innerTableLayout);
        return _outerPanelPrincipal;
    }

    // ── Zone dépôt (drag & drop + clic) ──────────────────────────────────────

    private Panel ConstruireZoneDepot()
    {
        _zoneDepot = new Panel { Dock = DockStyle.Fill, BackColor = Color.White,
                                  AllowDrop = true, Cursor = Cursors.Hand };

        _zoneDepot.Resize += (s, e) =>
        {
            if (_zoneDepot.Width < 1 || _zoneDepot.Height < 1) return;
            using var path = GraphicsUtils.RoundedRect(
                new Rectangle(0, 0, _zoneDepot.Width, _zoneDepot.Height), 10);
            _zoneDepot.Region = new Region(path);
        };

        _zoneDepot.Paint    += ZoneDepot_Paint;
        _zoneDepot.Click    += (s, e) => { if (!_extractionEnCours) OuvrirDialogSelection(); };
        _zoneDepot.DragEnter += ZoneDepot_DragEnter;
        _zoneDepot.DragLeave += ZoneDepot_DragLeave;
        _zoneDepot.DragDrop  += ZoneDepot_DragDrop;

        return _zoneDepot;
    }

    private void ZoneDepot_Paint(object sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        GraphicsUtils.SetSmooth(g);
        var p = _zoneDepot;

        Color bordure = _dragSurvol ? CouleurBleuAccent : Color.FromArgb(0xC8, 0xD4, 0xE0);
        Color texte   = _dragSurvol ? CouleurBleuAccent : Color.FromArgb(0x8A, 0x97, 0xA6);

        using var bgBrush = new SolidBrush(_dragSurvol ? Color.FromArgb(0xF0, 0xF5, 0xFD) : Color.White);
        using var bgPath  = GraphicsUtils.RoundedRect(new Rectangle(1, 1, p.Width - 3, p.Height - 3), 9);
        g.FillPath(bgBrush, bgPath);

        using var pen = new Pen(bordure, 1.5f);
        pen.DashStyle   = DashStyle.Dash;
        pen.DashPattern = new float[] { 6, 4 };
        g.DrawPath(pen, bgPath);

        int cx = p.Width / 2, cy = p.Height / 2 - 14;
        using var iconBrush = new SolidBrush(bordure);
        g.FillRectangle(iconBrush, cx - 2, cy - 10, 5, 14);
        g.FillPolygon(iconBrush, new[] { new Point(cx, cy + 14),
                                          new Point(cx - 9,  cy + 4),
                                          new Point(cx + 10, cy + 4) });
        g.FillRectangle(iconBrush, cx - 11, cy + 15, 23, 3);

        string txt = _dragSurvol ? "Relâchez pour ajouter les fichiers"
                                  : "Glissez vos fichiers ici  ou  cliquez pour parcourir";
        var sf = new StringFormat { Alignment = StringAlignment.Center };
        using var tBrush = new SolidBrush(texte);
        g.DrawString(txt, _dragSurvol ? PoliceSemiGras : PoliceGenerale, tBrush,
                     new RectangleF(12, cy + 22, p.Width - 24, 22), sf);

        if (!_dragSurvol)
        {
            using var sb = new SolidBrush(Color.FromArgb(0xB8, 0xC2, 0xCC));
            g.DrawString("Formats acceptés : .CATPart  ·  .CATProduct", PolicePetite, sb,
                         new RectangleF(12, cy + 46, p.Width - 24, 18), sf);
        }
    }

    private void ZoneDepot_DragEnter(object sender, DragEventArgs e)
    {
        if (!_extractionEnCours && e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            _dragSurvol = true;
            e.Effect    = DragDropEffects.Copy;
            _zoneDepot.Invalidate();
        }
        else e.Effect = DragDropEffects.None;
    }

    private void ZoneDepot_DragLeave(object sender, EventArgs e)
    { _dragSurvol = false; _zoneDepot.Invalidate(); }

    private void ZoneDepot_DragDrop(object sender, DragEventArgs e)
    {
        _dragSurvol = false;
        _zoneDepot.Invalidate();
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        foreach (string f in (string[])e.Data.GetData(DataFormats.FileDrop))
        {
            string ext = Path.GetExtension(f).ToLowerInvariant();
            if (ext == ".catpart" || ext == ".catproduct") AjouterFichier(f);
        }
    }

    // ── Carte fichiers sélectionnés ───────────────────────────────────────────

    private CartePanel ConstruireCarteFichiers()
    {
        var carte = new CartePanel { Dock = DockStyle.Fill };

        var lbl = new Label
        {
            Text = "Fichiers sélectionnés", Dock = DockStyle.Top, Height = 26,
            Font = PoliceSemiGras, ForeColor = CouleurTexte
        };

        _flowFichiers = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, AutoScroll = true, BackColor = Color.White
        };
        _flowFichiers.SizeChanged += (s, e) =>
        {
            int w = _flowFichiers.ClientSize.Width;
            foreach (Control c in _flowFichiers.Controls) c.Width = w;
        };

        // Fill avant Top (ordre d'ajout WinForms)
        carte.Controls.Add(_flowFichiers);
        carte.Controls.Add(lbl);
        return carte;
    }

    // ── Barre d'action (bouton Extraire) ─────────────────────────────────────

    private Panel ConstruireBarreAction()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = CouleurFond };

        _btnExtraire = new BoutonArrondi
        {
            Text = "▶   Extraire les données CATIA", Enabled = false,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            Dock = DockStyle.Right, Width = 320, Height = 46
        };
        _btnExtraire.Click += LancerExtraction;
        panel.Controls.Add(_btnExtraire);
        return panel;
    }

    // ── Zone journal (Dock=Fill) ──────────────────────────────────────────────

    private Panel ConstruireZoneLog()
    {
        var outer = new Panel
        {
            Dock = DockStyle.Fill, BackColor = CouleurFond,
            Padding = new Padding(20, 10, 20, 10)
        };

        var carte = new CartePanel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(0xFA, 0xFB, 0xFC) };

        var lbl = new Label
        {
            Text = "Journal d'extraction", Dock = DockStyle.Top, Height = 26,
            Font = PoliceSemiGras, ForeColor = CouleurTexte
        };

        _lblStatut = new Label
        {
            Text = string.Empty, Dock = DockStyle.Bottom, Height = 22,
            ForeColor = CouleurBleuAccent, Font = PolicePetite
        };

        _txtLog = new RichTextBox
        {
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(0xFA, 0xFB, 0xFC),
            ForeColor = CouleurTexte,
            Font = new Font("Consolas", 10.5f),
            WordWrap = false,
            BorderStyle = BorderStyle.None,
            Padding = new Padding(4, 0, 4, 0)
        };

        carte.Controls.Add(_txtLog);
        carte.Controls.Add(_lblStatut);
        carte.Controls.Add(lbl);
        outer.Controls.Add(carte);
        return outer;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Gestion de la liste de fichiers
    // ═════════════════════════════════════════════════════════════════════════

    private void AjouterFichier(string chemin)
    {
        if (_fichiersSelectionnes.Exists(f => f.Equals(chemin, StringComparison.OrdinalIgnoreCase)))
            return;
        _fichiersSelectionnes.Add(chemin);
        _flowFichiers.Controls.Add(CreerLigneFichier(chemin));
        ActualiserVisibiliteFichiers();
        MettreAJourBoutons();
    }

    private void SupprimerFichier(string chemin)
    {
        _fichiersSelectionnes.RemoveAll(f => f.Equals(chemin, StringComparison.OrdinalIgnoreCase));
        Control aRetirer = null;
        foreach (Control c in _flowFichiers.Controls)
            if (c.Tag as string == chemin) { aRetirer = c; break; }
        if (aRetirer != null) _flowFichiers.Controls.Remove(aRetirer);
        AppliquerAlternance();
        ActualiserVisibiliteFichiers();
        MettreAJourBoutons();
    }

    private void ActualiserVisibiliteFichiers()
    {
        bool visible = _fichiersSelectionnes.Count > 0;
        if (_carteFichiers.Visible == visible) return;

        SuspendLayout();
        _carteFichiers.Visible         = visible;
        _innerTableLayout.RowStyles[2] = new RowStyle(SizeType.Absolute, visible ? 150 : 0);
        _innerTableLayout.RowStyles[3] = new RowStyle(SizeType.Absolute, visible ? 12  : 0);
        _outerPanelPrincipal.Height    = visible ? HauteurAvecFichiers : HauteurSansFichiers;
        ResumeLayout();
    }

    private void AppliquerAlternance()
    {
        int i = 0;
        foreach (Control row in _flowFichiers.Controls)
        {
            Color bg = (i % 2 == 0) ? Color.White : CouleurLigneAlt;
            row.BackColor = bg;
            foreach (Control child in row.Controls)
                if (child.Tag as string == "altbg") child.BackColor = bg;
            i++;
        }
    }

    private Panel CreerLigneFichier(string chemin)
    {
        int idx   = _fichiersSelectionnes.Count - 1;
        var bgRow = idx % 2 == 0 ? Color.White : CouleurLigneAlt;

        string nom    = Path.GetFileName(chemin);
        string taille = "";
        try { var fi = new FileInfo(chemin); if (fi.Exists) taille = $"  ·  {fi.Length / 1024.0:F0} Ko"; }
        catch { }

        var row = new Panel
        {
            Tag = chemin, Height = 50, BackColor = bgRow,
            Margin = new Padding(0), Padding = new Padding(0),
            Width = Math.Max(_flowFichiers.ClientSize.Width, 100)
        };

        var strip = new Panel { Dock = DockStyle.Left,   Width = 4,  BackColor = CouleurBleuAccent };
        var sep   = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(0xEE, 0xF2, 0xF6) };

        var btnX = new Button
        {
            Dock = DockStyle.Right, Width = 40, FlatStyle = FlatStyle.Flat, Tag = "altbg",
            Text = "✕", BackColor = bgRow, ForeColor = Color.FromArgb(0xC0, 0xC8, 0xD4),
            Font = new Font("Segoe UI", 9f), Cursor = Cursors.Hand
        };
        btnX.FlatAppearance.BorderSize = 0;
        btnX.MouseEnter += (s, e) => btnX.ForeColor = CouleurErreur;
        btnX.MouseLeave += (s, e) => btnX.ForeColor = Color.FromArgb(0xC0, 0xC8, 0xD4);
        btnX.Click      += (s, e) => SupprimerFichier(chemin);

        var panelLabels = new Panel
        {
            Dock = DockStyle.Fill, BackColor = bgRow, Tag = "altbg",
            Padding = new Padding(10, 4, 4, 4)
        };

        var lblNom = new Label
        {
            Text = nom, Dock = DockStyle.Top, Height = 24,
            Font = PoliceSemiGras, ForeColor = CouleurTexte,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var lblPath = new Label
        {
            Text = chemin + taille, Dock = DockStyle.Fill,
            Font = PolicePetite, ForeColor = Color.FromArgb(0x8A, 0x97, 0xA6),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Fill added first (processed last = fills remaining space), then Top
        panelLabels.Controls.Add(lblPath);
        panelLabels.Controls.Add(lblNom);

        // Fill first, then Right, Left, Bottom (Bottom added last = processed first by layout engine)
        row.Controls.Add(panelLabels);
        row.Controls.Add(btnX);
        row.Controls.Add(strip);
        row.Controls.Add(sep);
        return row;
    }

    private void MettreAJourBoutons()
        => _btnExtraire.Enabled = _fichiersSelectionnes.Count > 0 && !_extractionEnCours;

    // ═════════════════════════════════════════════════════════════════════════
    //  Logique métier (inchangée)
    // ═════════════════════════════════════════════════════════════════════════

    private void OuvrirDialogSelection()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Sélectionner des fichiers CATIA",
            Filter = "Fichiers CATIA (*.CATPart;*.CATProduct)|*.CATPart;*.CATProduct",
            Multiselect = true, CheckFileExists = true
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        foreach (string chemin in dlg.FileNames) AjouterFichier(chemin);
    }

    private async void LancerExtraction(object sender, EventArgs e)
    {
        if (_fichiersSelectionnes.Count == 0) return;

        var fichiers = new List<string>(_fichiersSelectionnes);

        _extractionEnCours        = true;
        _btnExtraire.Enabled      = false;
        _zoneDepot.Cursor         = Cursors.Default;
        _txtLog.Clear();
        _lblStatut.Text           = "Extraction en cours…";
        _lblStatut.ForeColor      = Color.FromArgb(0xD4, 0x6A, 0x00);

        var ancienOut = Console.Out;
        Console.SetOut(new EcriteurUi(_txtLog, this));

        List<string> jsonPaths  = null;
        string       erreurFinale = null;

        try
        {
            jsonPaths = await Task.Run(() =>
            {
                var extracteur = new ExtracteurService(_catia);
                return extracteur.Extraire(fichiers);
            });
        }
        catch (Exception ex) { erreurFinale = ex.Message; }
        finally { Console.SetOut(ancienOut); }

        _extractionEnCours   = false;
        _zoneDepot.Cursor    = Cursors.Hand;
        MettreAJourBoutons();

        if (erreurFinale != null)
        {
            _lblStatut.Text      = "Extraction échouée.";
            _lblStatut.ForeColor = CouleurErreur;
            MessageBox.Show($"Erreur lors de l'extraction :\n{erreurFinale}",
                "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        else
        {
            int n = jsonPaths?.Count ?? 0;
            _lblStatut.Text      = $"{n} fichier(s) extrait(s) → data/";
            _lblStatut.ForeColor = Color.FromArgb(0x1A, 0x7A, 0x3C);
            string liste = jsonPaths != null ? string.Join("\n", jsonPaths) : "";
            MessageBox.Show($"Extraction terminée — {n} JSON généré(s) :\n\n{liste}",
                "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    // ── Utilitaires ───────────────────────────────────────────────────────────

    private static string TrouverFichier(string cheminRelatif)
    {
        string dir = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            string c = Path.Combine(dir, cheminRelatif);
            if (System.IO.File.Exists(c)) return c;
            string parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        return null;
    }

    // ── Redirection Console → RichTextBox avec coloration ────────────────────

    private sealed class EcriteurUi : TextWriter
    {
        private readonly RichTextBox _textBox;
        private readonly Control     _owner;

        private static readonly Color ColNeutral = Color.FromArgb(0x2C, 0x3E, 0x50);
        private static readonly Color ColSucces  = Color.FromArgb(0x4C, 0xAF, 0x50);
        private static readonly Color ColErreur  = Color.FromArgb(0xE7, 0x4C, 0x3C);

        public EcriteurUi(RichTextBox textBox, Control owner)
        { _textBox = textBox; _owner = owner; }

        public override Encoding Encoding => Encoding.UTF8;

        public override void WriteLine(string value) => Ecrire(value + Environment.NewLine);
        public override void Write(string value)      => Ecrire(value);

        private void Ecrire(string texte)
        {
            if (_owner.InvokeRequired) { _owner.BeginInvoke(new Action(() => Ecrire(texte))); return; }

            Color couleur = ColNeutral;
            if (texte.Contains("Erreur") || texte.Contains("ECHEC") || texte.Contains("Exception"))
                couleur = ColErreur;
            else if (texte.Contains("extraits !") || texte.Contains("extrait !") || texte.Contains("succès"))
                couleur = ColSucces;

            _textBox.SelectionStart  = _textBox.TextLength;
            _textBox.SelectionLength = 0;
            _textBox.SelectionColor  = couleur;
            _textBox.AppendText(texte);
            _textBox.ScrollToCaret();
        }
    }
}
