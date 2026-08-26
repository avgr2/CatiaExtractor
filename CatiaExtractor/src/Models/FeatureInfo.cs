using System.Collections.Generic;

// Représente une feature de l'arbre de spécification (Pad, Pocket, Sketch, EdgeFillet, ...)
// dans l'ordre chronologique de création/dépendance.
public class FeatureInfo
{
    // Position dans l'ordre chronologique global (features + sketches confondus,
    // un sketch étant toujours émis juste avant la feature qui le consomme)
    public int Ordre { get; set; }

    // Type CATIA réel (Pad, Pocket, EdgeFillet, Chamfer, Loft, Rib, Shaft, Groove, Slot, Sweep, Sketch, Mirror, ...)
    public string Type { get; set; }

    public string Nom { get; set; }

    // Sketch(s) consommé(s) par cette feature (vide pour un Sketch lui-même)
    public List<string> SketchReferences { get; set; } = new();

    // Paramètres numériques propres à la feature (longueur, profondeur, rayon, angle...)
    public Dictionary<string, double> Parametres { get; set; } = new();

    // Références nommées non-numériques (ex: objet_miroire, plan_symetrie pour Mirror)
    public Dictionary<string, string> Refs { get; set; } = new();

    // Renseigné uniquement quand Type == "Sketch"
    public SketchGeometrie Geometrie { get; set; }
}

public class SketchGeometrie
{
    // Placement 3D absolu du sketch (origine + 2 axes du plan), via GetAbsoluteAxisData.
    // Remplace l'ancien champ "Support" (string) qui n'a jamais pu être rempli —
    // ceci est plus utile : on obtient directement la position et l'orientation
    // du plan du sketch dans la pièce, sans avoir besoin de connaître le plan/face
    // de référence sous-jacent.
    public Placement3D Placement { get; set; }

    public List<CercleInfo> Cercles { get; set; } = new();
    public List<LigneInfo> Lignes { get; set; } = new();
    public List<ArcInfo> Arcs { get; set; } = new();
    public List<AxeInfo> Axes { get; set; } = new();
    public List<ContrainteInfo> Contraintes { get; set; } = new();
}

public class Placement3D
{
    public double OrigineX { get; set; }
    public double OrigineY { get; set; }
    public double OrigineZ { get; set; }
    public double AxeXx { get; set; }
    public double AxeXy { get; set; }
    public double AxeXz { get; set; }
    public double AxeYx { get; set; }
    public double AxeYy { get; set; }
    public double AxeYz { get; set; }
}

public class CercleInfo
{
    public double CentreX { get; set; }
    public double CentreY { get; set; }
    public double Rayon { get; set; }
    public bool EstConstruction { get; set; }
}

public class LigneInfo
{
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
    public bool EstConstruction { get; set; }
}

public class ArcInfo
{
    public double CentreX { get; set; }
    public double CentreY { get; set; }
    public double Rayon { get; set; }
    public double AngleDebut { get; set; }
    public double AngleFin { get; set; }
    public bool EstConstruction { get; set; }
}

// Axe de révolution (Axis2D) d'un sketch — origine + lignes de référence H/V
public class AxeInfo
{
    public double OrigineX { get; set; }
    public double OrigineY { get; set; }
    public double RefHorizontaleX1 { get; set; }
    public double RefHorizontaleY1 { get; set; }
    public double RefHorizontaleX2 { get; set; }
    public double RefHorizontaleY2 { get; set; }
    public double RefVerticaleX1 { get; set; }
    public double RefVerticaleY1 { get; set; }
    public double RefVerticaleX2 { get; set; }
    public double RefVerticaleY2 { get; set; }
    public bool EstConstruction { get; set; }
}

// Plan de référence extrait des HybridBodies (Geometrical Sets)
public class PlanReference
{
    public string Nom { get; set; }
    // Type déduit par sondage dynamique (HybridShapePlaneOffset, HybridShapePlaneAngle, ...)
    public string Type { get; set; }
    public double? Offset { get; set; }
    // Nom de l'élément de référence (plan ou face d'origine) — ex : "xy plane" pour un plan décalé de XY.
    // Reste null dans l'état actuel : la propriété COM s'appelle "PlaneRef" sur HybridShapePlaneOffset,
    // mais il n'existe aucun assembly .NET interop pour ce sous-type dans l'installation CATIA B19
    // (GSMITF.dll est une DLL COM native sans manifeste .NET ; aucune alternative trouvée dans le bin).
    // TryDynamicMember avec "Reference"/"Element"/"FirstElement" ne retourne rien via IDispatch.
    // Pour débloquer : générer un wrapper avec tlbimp.exe sur GSMITF.dll, ou tester "PlaneRef" en late binding.
    public string ElementReference { get; set; }
}

public class ContrainteInfo
{
    public string Type { get; set; }
    public double? Valeur { get; set; }
}

public class RelationInfo
{
    public string Nom { get; set; }
    public string Formule { get; set; }
}