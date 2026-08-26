using System;
using System.Runtime.InteropServices;
using INFITF;

public class CatiaConnector
{
    [DllImport("oleaut32.dll")]
    static extern int GetActiveObject(
        ref Guid rclsid,
        IntPtr pvReserved,
        [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

    static Guid GetCatiaGuid()
    {
        Type t = Type.GetTypeFromProgID("CATIA.Application");
        if (t == null) throw new Exception(
            "CATIA.Application introuvable dans le registre !");
        return t.GUID;
    }

    public static Application Connecter()
    {
        Application catia;

        
        // Sinon → on lance Catia en arrière-plan
        Type catiaType = Type.GetTypeFromProgID("CATIA.Application");
        catia = (Application)Activator.CreateInstance(catiaType);
        Console.WriteLine("Catia lancé en arrière-plan !");
        

        // Toujours en arrière-plan
        catia.Visible = false;
        catia.RefreshDisplay = false;

        return catia;
    }

    public static void Deconnecter(Application catia)
    {
        catia.RefreshDisplay = true;
        catia.Quit();
        Console.WriteLine("Catia fermé !");
    }
}