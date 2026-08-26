using System;
using System.Windows.Forms;

class Program
{
    // [STAThread] est obligatoire pour WinForms (OpenFileDialog, COM, etc.).
    // Les top-level statements ne l'appliquent pas automatiquement en .NET 8.
    [STAThread]
    static void Main()
    {
        // Connexion à CATIA avant d'afficher le formulaire.
        INFITF.Application catia = CatiaConnector.Connecter();

        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        System.Windows.Forms.Application.Run(new MainForm(catia));

        // La déconnexion CATIA est gérée par l'événement FormClosing de MainForm.
    }
}
