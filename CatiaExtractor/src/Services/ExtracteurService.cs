using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using INFITF;
using MECMOD;
using PARTITF;

public class ExtracteurService
{
    private readonly Application _catia;

    public ExtracteurService(Application catia)
    {
        _catia = catia;
    }

    // Retourne la liste des chemins absolus des fichiers JSON générés (un par fichier source).
    public List<string> Extraire(List<string> cheminsFichiers)
    {
        var jsonPaths = new List<string>();
        string dataDir = TrouverRepertoireData();
        var opts = new JsonSerializerOptions { WriteIndented = true };

        foreach (var cheminComplet in cheminsFichiers)
        {
            string nomFichier = Path.GetFileName(cheminComplet);
            Console.WriteLine($"Ouverture de : {nomFichier}");

            try
            {
                PartDocument partDoc = (PartDocument)_catia.Documents.Open(cheminComplet);
                Part part = partDoc.Part;
                Body body = part.Bodies.Item(1);

                var dims     = ExtraireDimensions(part);
                var features = ExtraireFeaturesOrdonnees(body, part);
                var plans    = ExtrairePlans(part, body);
                var relations = ExtraireRelations(part);

                var piece = new Piece
                {
                    Nom        = part.get_Name(),
                    Fichier    = nomFichier,
                    Dimensions = dims,
                    Features   = features,
                    Plans      = plans,
                    Relations  = relations
                };

                Console.WriteLine($"  → {dims.Count} paramètres, {features.Count} features/sketches, {plans.Count} plans extraits !");

                string json      = JsonSerializer.Serialize(piece, opts);
                string nomSortie = Path.GetFileNameWithoutExtension(cheminComplet) + ".json";
                string jsonPath  = Path.Combine(dataDir, nomSortie);
                System.IO.File.WriteAllText(jsonPath, json);
                Console.WriteLine($"  → JSON : {jsonPath}");
                jsonPaths.Add(jsonPath);

                partDoc.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur sur {nomFichier} : {ex.Message}");
            }
        }

        Console.WriteLine($"\n{jsonPaths.Count}/{cheminsFichiers.Count} fichier(s) traité(s) avec succès.");
        return jsonPaths;
    }

    // Remonte l'arborescence depuis le répertoire de l'exe jusqu'à trouver un dossier "data/".
    // Crée ce dossier s'il n'existe pas encore (cas d'un premier lancement sans data/).
    private static string TrouverRepertoireData()
    {
        string dir = AppDomain.CurrentDomain.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            string candidat = Path.Combine(dir, "data");
            if (Directory.Exists(candidat))
                return candidat;
            string parent = Path.GetDirectoryName(dir);
            if (parent == dir) break;
            dir = parent;
        }
        // Fallback : crée data/ à côté de l'exe si introuvable plus haut.
        string fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        Directory.CreateDirectory(fallback);
        return fallback;
    }

    private Dictionary<string, double> ExtraireDimensions(Part part)
    {
        var dims = new Dictionary<string, double>();
        var parameters = part.Parameters;

        for (int i = 1; i <= parameters.Count; i++)
        {
            try
            {
                var param = parameters.Item(i);
                string valeur = param.ValueAsString()
                    .Replace("mm", "")
                    .Replace("deg", "")
                    .Trim();

                if (double.TryParse(valeur,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double result))
                {
                    dims[param.get_Name()] = result;
                }
            }
            catch { }
        }

        return dims;
    }

    private List<FeatureInfo> ExtraireFeaturesOrdonnees(Body body, Part part)
    {
        Shapes shapes = body.Shapes;
        Sketches sketchList = body.Sketches;

        var sketchesByName = new Dictionary<string, Sketch>();
        for (int i = 1; i <= sketchList.Count; i++)
        {
            Sketch s = sketchList.Item(i);
            sketchesByName[s.get_Name()] = s;
        }

        var resultat = new List<FeatureInfo>();
        var sketchesEmis = new HashSet<string>();
        int compteurGlobal = 1;
        string nomShapePrecedente = null;

        for (int i = 1; i <= shapes.Count; i++)
        {
            Shape shape = shapes.Item(i);

            List<string> sketchRefs = GetSketchReferences(shape);

            foreach (string refName in sketchRefs)
            {
                if (sketchesByName.TryGetValue(refName, out Sketch sk)
                    && !sketchesEmis.Contains(refName))
                {
                    resultat.Add(ExtraireSketch(sk, compteurGlobal++));
                    sketchesEmis.Add(refName);
                }
            }

            FeatureInfo feature = shape switch
            {
                Pad pad             => ExtrairePad(pad, sketchRefs, compteurGlobal++),
                Pocket pocket       => ExtrairePocket(pocket, sketchRefs, compteurGlobal++),
                EdgeFillet fillet   => ExtraireConge(fillet, compteurGlobal++),
                Chamfer chamfer     => ExtraireChanfrein(chamfer, compteurGlobal++),
                Shaft shaft         => ExtraireShaft(shaft, sketchRefs, compteurGlobal++),
                Groove groove       => ExtraireGroove(groove, sketchRefs, compteurGlobal++),
                Rib rib             => ExtraireRib(rib, sketchRefs, compteurGlobal++),
                Slot slot           => ExtraireSlot(slot, sketchRefs, compteurGlobal++),
                Mirror mirror       => ExtraireMirror(mirror, compteurGlobal++),
                _                   => FeatureNonGeree(shape, sketchRefs, compteurGlobal++)
            };

            // Heuristique Mirror : MirroringObject renvoie une référence circulaire vers
            // le Mirror lui-même. En l'absence d'API fiable pour remonter à la source,
            // on capture le nom de la shape qui précède immédiatement dans body.Shapes —
            // dans l'arbre CATIA, la feature mirrorée se trouve typiquement juste avant
            // le Mirror. Ce n'est pas garanti à 100% (ex. si le Mirror s'applique à
            // plusieurs features ou à une feature non-contiguë).
            if (shape is Mirror && nomShapePrecedente != null)
                feature.Refs["element_source_probable"] = nomShapePrecedente;

            resultat.Add(feature);
            nomShapePrecedente = shape.get_Name();
        }

        foreach (var kvp in sketchesByName)
        {
            if (!sketchesEmis.Contains(kvp.Key))
            {
                resultat.Add(ExtraireSketch(kvp.Value, compteurGlobal++));
            }
        }

        return resultat;
    }

    // Déroule les TargetInvocationException imbriquées pour retrouver la vraie cause COM.
    private static Exception CauseReelle(Exception ex)
    {
        while (ex is TargetInvocationException tie && tie.InnerException != null)
            ex = tie.InnerException;
        return ex;
    }

    private static object TryDynamicMember(object comObject, params string[] candidateNames)
    {
        if (comObject == null) return null;
        Type t = comObject.GetType();

        foreach (var name in candidateNames)
        {
            try
            {
                var valeur = t.InvokeMember(name,
                    BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance,
                    null, comObject, null);
                if (valeur != null) return valeur;
            }
            catch { }

            try
            {
                var valeur = t.InvokeMember(name,
                    BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance,
                    null, comObject, null);
                if (valeur != null) return valeur;
            }
            catch { }
        }

        return null;
    }

    private static double? ExtraireValeurNumerique(object obj)
    {
        if (obj == null) return null;

        var valeurInterne = TryDynamicMember(obj, "Value");
        if (valeurInterne != null)
        {
            try { return Convert.ToDouble(valeurInterne); } catch { }
        }

        try { return Convert.ToDouble(obj); } catch { return null; }
    }

    private static (double x, double y)? GetPoint2DCoordinates(object point)
    {
        if (point == null) return null;

        if (point is Point2D p2d)
        {
            try
            {
                object[] coords = new object[2];
                p2d.GetCoordinates(coords);
                return (Convert.ToDouble(coords[0]), Convert.ToDouble(coords[1]));
            }
            catch { }
        }

        try
        {
            Type t = point.GetType();
            object[] coords = new object[2];
            t.InvokeMember("GetCoordinates",
                BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance,
                null, point, new object[] { coords });
     
     
            return (Convert.ToDouble(coords[0]), Convert.ToDouble(coords[1]));
        }
        catch
        {
            return null;
        }
    }

    private static Placement3D GetSketchPlacement(Sketch sketch)
    {
        string nomSketch = "(inconnu)";
        try { nomSketch = sketch.get_Name(); } catch { }

        try
        {
            // Appel vtable direct (pas InvokeMember/IDispatch) : le paramètre [In,Out] object[]
            // est correctement marshalé via early binding ; InvokeMember causait TYPEMISMATCH 0x80020005.
            object[] data = new object[9];
            sketch.GetAbsoluteAxisData(data);
            Console.WriteLine($"[GetSketchPlacement/{nomSketch}] GetAbsoluteAxisData brut : [{string.Join(", ", data)}]");

            return new Placement3D
            {
                OrigineX = Convert.ToDouble(data[0]), OrigineY = Convert.ToDouble(data[1]), OrigineZ = Convert.ToDouble(data[2]),
                AxeXx    = Convert.ToDouble(data[3]), AxeXy    = Convert.ToDouble(data[4]), AxeXz    = Convert.ToDouble(data[5]),
                AxeYx    = Convert.ToDouble(data[6]), AxeYy    = Convert.ToDouble(data[7]), AxeYz    = Convert.ToDouble(data[8])
            };
        }
        catch (Exception ex)
        {
            var r = CauseReelle(ex);
            Console.WriteLine($"[GetSketchPlacement/{nomSketch}] ECHEC : {r.GetType().Name} — {r.Message}");
            return null;
        }
    }

    private List<string> GetSketchReferences(Shape shape)
    {
        var refs = new List<string>();

        switch (shape)
        {
            case Pad pad:
                AjouterSiPresent(refs, pad.Sketch as Sketch);
                break;

            case Pocket pocket:
                AjouterSiPresent(refs, pocket.Sketch as Sketch);
                break;

            case Shaft shaft:
                AjouterSiPresent(refs, shaft.Sketch as Sketch);
                break;

            case Groove groove:
                AjouterSiPresent(refs, groove.Sketch as Sketch);
                break;

            case Rib rib:
                if (TryDynamicMember(rib, "Sketch", "Profile", "ProfileElement") is Sketch profilRib)
                    refs.Add(profilRib.get_Name());
                if (TryDynamicMember(rib, "CenterCurve", "Trajectory", "Guide") is Sketch trajRib)
                    refs.Add(trajRib.get_Name());
                break;

            case Slot slot:
                if (TryDynamicMember(slot, "Sketch", "Profile", "ProfileElement") is Sketch profilSlot)
                    refs.Add(profilSlot.get_Name());
                if (TryDynamicMember(slot, "CenterCurve", "Trajectory", "Guide") is Sketch trajSlot)
                    refs.Add(trajSlot.get_Name());
                break;
        }

        return refs;
    }

    private void AjouterSiPresent(List<string> refs, Sketch sketch)
    {
        if (sketch != null) refs.Add(sketch.get_Name());
    }

    private FeatureInfo ExtrairePad(Pad pad, List<string> sketchRefs, int ordre)
    {
        var f = new FeatureInfo { Ordre = ordre, Type = "Pad", Nom = pad.get_Name(), SketchReferences = sketchRefs };
        try { f.Parametres["longueur"] = pad.FirstLimit.Dimension.Value; } catch { }
        try { f.Parametres["longueur_seconde"] = pad.SecondLimit.Dimension.Value; } catch { }
        ExtraireDirectionPrisme(pad, f);
        return f;
    }

    private FeatureInfo ExtrairePocket(Pocket pocket, List<string> sketchRefs, int ordre)
    {
        var f = new FeatureInfo { Ordre = ordre, Type = "Pocket", Nom = pocket.get_Name(), SketchReferences = sketchRefs };
        try { f.Parametres["profondeur"] = pocket.FirstLimit.Dimension.Value; } catch { }
        try { f.Parametres["profondeur_seconde"] = pocket.SecondLimit.Dimension.Value; } catch { }
        ExtraireDirectionPrisme(pocket, f);
        return f;
    }

    // Prism est la base commune de Pad et Pocket — confirmé par réflexion.
    // GetDirection est [In,Out] object[] : appel vtable direct requis (InvokeMember via IDispatch
    // ne remplissait pas le tableau, silencieusement, même maladie que GetAbsoluteAxisData).
    private static void ExtraireDirectionPrisme(Prism prisme, FeatureInfo f)
    {
        try
        {
            object[] dir = new object[3];
            prisme.GetDirection(dir);
            Console.WriteLine($"[ExtraireDirectionPrisme/{f.Nom}] GetDirection brut : [{dir[0]}, {dir[1]}, {dir[2]}]  types=[{dir[0]?.GetType().Name}, {dir[1]?.GetType().Name}, {dir[2]?.GetType().Name}]");
            f.Parametres["direction_x"] = Convert.ToDouble(dir[0] ?? 0.0, System.Globalization.CultureInfo.InvariantCulture);
            f.Parametres["direction_y"] = Convert.ToDouble(dir[1] ?? 0.0, System.Globalization.CultureInfo.InvariantCulture);
            f.Parametres["direction_z"] = Convert.ToDouble(dir[2] ?? 0.0, System.Globalization.CultureInfo.InvariantCulture);
            Console.WriteLine($"[ExtraireDirectionPrisme/{f.Nom}] Stocké : x={f.Parametres["direction_x"]}  y={f.Parametres["direction_y"]}  z={f.Parametres["direction_z"]}");
        }
        catch (Exception ex)
        {
            var r = CauseReelle(ex);
            Console.WriteLine($"[ExtraireDirectionPrisme/{f.Nom}] GetDirection ECHEC : {r.GetType().Name} — {r.Message}");
        }

        try { f.Parametres["direction_orientation"] = Convert.ToDouble((int)prisme.DirectionOrientation); }
        catch (Exception ex)
        {
            var r = CauseReelle(ex);
            Console.WriteLine($"[ExtraireDirectionPrisme/{f.Nom}] DirectionOrientation ECHEC : {r.GetType().Name} — {r.Message}");
        }

        try { f.Parametres["direction_type"] = Convert.ToDouble((int)prisme.DirectionType); }
        catch (Exception ex)
        {
            var r = CauseReelle(ex);
            Console.WriteLine($"[ExtraireDirectionPrisme/{f.Nom}] DirectionType ECHEC : {r.GetType().Name} — {r.Message}");
        }

        try { f.Parametres["est_symetrique"] = prisme.IsSymmetric ? 1.0 : 0.0; }
        catch (Exception ex)
        {
            var r = CauseReelle(ex);
            Console.WriteLine($"[ExtraireDirectionPrisme/{f.Nom}] IsSymmetric ECHEC : {r.GetType().Name} — {r.Message}");
        }
    }

    private FeatureInfo ExtraireConge(EdgeFillet fillet, int ordre)
    {
        var f = new FeatureInfo { Ordre = ordre, Type = "EdgeFillet", Nom = fillet.get_Name() };
        var rayon = ExtraireValeurNumerique(TryDynamicMember(fillet, "Radius", "EdgeRadius", "GetRadius"));
        if (rayon.HasValue) f.Parametres["rayon"] = rayon.Value;
        return f;
    }

    private FeatureInfo ExtraireChanfrein(Chamfer chamfer, int ordre)
    {
        var f = new FeatureInfo { Ordre = ordre, Type = "Chamfer", Nom = chamfer.get_Name() };
        try { f.Parametres["longueur"] = chamfer.Length1.Value; } catch { }
        try { f.Parametres["angle"] = chamfer.Angle.Value; } catch { }
        return f;
    }

    private FeatureInfo ExtraireShaft(Shaft shaft, List<string> sketchRefs, int ordre)
    {
        var f = new FeatureInfo { Ordre = ordre, Type = "Shaft", Nom = shaft.get_Name(), SketchReferences = sketchRefs };
        try { f.Parametres["angle"] = shaft.FirstAngle.Value; } catch { }
        return f;
    }

    private FeatureInfo ExtraireGroove(Groove groove, List<string> sketchRefs, int ordre)
    {
        var f = new FeatureInfo { Ordre = ordre, Type = "Groove", Nom = groove.get_Name(), SketchReferences = sketchRefs };
        try { f.Parametres["angle"] = groove.FirstAngle.Value; } catch { }
        return f;
    }

    private FeatureInfo ExtraireRib(Rib rib, List<string> sketchRefs, int ordre)
    {
        return new FeatureInfo { Ordre = ordre, Type = "Rib", Nom = rib.get_Name(), SketchReferences = sketchRefs };
    }

    private FeatureInfo ExtraireSlot(Slot slot, List<string> sketchRefs, int ordre)
    {
        return new FeatureInfo { Ordre = ordre, Type = "Slot", Nom = slot.get_Name(), SketchReferences = sketchRefs };
    }

    private FeatureInfo ExtraireMirror(Mirror mirror, int ordre)
    {
        var f = new FeatureInfo { Ordre = ordre, Type = "Mirror", Nom = mirror.get_Name() };

        // 1. Énumère TOUTES les propriétés déclarées sur typeof(Mirror) (statiques + héritées)
        //    — permet de voir si l'interop expose quelque chose au-delà de MirroringObject/Plane.
        Console.WriteLine($"[ExtraireMirror/{f.Nom}] === typeof(Mirror).GetProperties() ===");
        foreach (var pi in typeof(Mirror).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            try
            {
                object val = pi.GetValue(mirror);
                string desc = val == null ? "null"
                    : $"runtime={val.GetType().Name}  name=\"{TryDynamicMember(val, "get_Name", "Name", "DisplayName") ?? "?"}\"";
                Console.WriteLine($"[ExtraireMirror/{f.Nom}]   {pi.Name} ({pi.PropertyType.Name}) → {desc}");
            }
            catch (Exception ex) { Console.WriteLine($"[ExtraireMirror/{f.Nom}]   {pi.Name} ECHEC : {CauseReelle(ex).Message}"); }
        }

        // 1b. Dump des méthodes — pour voir si Mirror expose des accesseurs non publiés en propriété.
        Console.WriteLine($"[ExtraireMirror/{f.Nom}] === typeof(Mirror).GetMethods() ===");
        foreach (var mi in typeof(Mirror).GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            var sb = new System.Text.StringBuilder();
            foreach (var p in mi.GetParameters())
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append($"{p.ParameterType.Name} {p.Name}");
            }
            Console.WriteLine($"[ExtraireMirror/{f.Nom}]   {mi.Name}({sb}) → {mi.ReturnType.Name}");
        }

        // 2. MirroringObject : détecte si le résultat est circulaire (même nom que la feature)
        //    et sonde ses sous-membres pour identifier son vrai type COM.
        try
        {
            AnyObject mObj = mirror.MirroringObject;
            if (mObj != null)
            {
                string nomObj = mObj.get_Name();
                bool isCirculaire = nomObj == f.Nom;
                Console.WriteLine($"[ExtraireMirror/{f.Nom}] MirroringObject name=\"{nomObj}\"  runtime={mObj.GetType().Name}  isCirculaire={isCirculaire}");

                if (!isCirculaire)
                    f.Refs["objet_miroire"] = nomObj;

                foreach (var sub in new[] { "Shapes", "Sketches", "Features", "HybridShapes",
                    "Count", "Bodies", "ShapeFeature", "Value", "Element", "Reference",
                    "get_Name", "Name", "DisplayName", "Label" })
                {
                    object subVal = TryDynamicMember(mObj, sub);
                    if (subVal != null)
                        Console.WriteLine($"[ExtraireMirror/{f.Nom}]   MirroringObject.{sub}  runtime={subVal.GetType().Name}  val=\"{subVal}\"");
                }
            }
        }
        catch { }

        // 3. MirroringPlane → plan_symetrie (confirmé fonctionnel via DisplayName)
        try
        {
            Reference plan = mirror.MirroringPlane;
            if (plan != null)
            {
                string nomPlan = TryDynamicMember(plan, "DisplayName", "get_Name", "Name")?.ToString();
                if (!string.IsNullOrEmpty(nomPlan))
                {
                    // Normalise les plans standard vers des tokens canoniques indépendants de la locale,
                    // parce que DisplayName renvoie un libellé UI ("xy plane", "plan XY", etc.)
                    // qui n'est pas un nom d'objet CATIA recherchable via l'API de reconstruction.
                    string lower = nomPlan.ToLowerInvariant();
                    if      (lower.Contains("xy")) nomPlan = "#XY";
                    else if (lower.Contains("yz")) nomPlan = "#YZ";
                    else if (lower.Contains("zx") || lower.Contains("xz")) nomPlan = "#ZX";
                    // Sinon : libellé brut conservé (plan non standard — reconstruction à gérer au cas par cas)
                    f.Refs["plan_symetrie"] = nomPlan;
                }
            }
        }
        catch { }

        return f;
    }

    private FeatureInfo ExtraireSketch(Sketch sketch, int ordre)
    {
        var geo = new SketchGeometrie();
        string nomSketch = "(inconnu)";
        try { nomSketch = sketch.get_Name(); } catch { }

        geo.Placement = GetSketchPlacement(sketch);

        try
        {
            GeometricElements elements = sketch.GeometricElements;
            for (int i = 1; i <= elements.Count; i++)
            {
                try
                {
                    var elt = elements.Item(i);

                    if (elt is Circle2D cercle)
                    {
                        var centre = GetPoint2DCoordinates(cercle.CenterPoint);
                        bool estConstr = false;
                        try { estConstr = cercle.Construction; } catch { }

                        geo.Cercles.Add(new CercleInfo
                        {
                            CentreX = centre?.x ?? 0,
                            CentreY = centre?.y ?? 0,
                            Rayon = cercle.Radius,
                            EstConstruction = estConstr
                        });
                    }
                    else if (elt is Line2D ligne)
                    {
                        var debut = GetPoint2DCoordinates(ligne.StartPoint);
                        var fin = GetPoint2DCoordinates(ligne.EndPoint);
                        bool estConstr = false;
                        try { estConstr = ligne.Construction; } catch { }

                        geo.Lignes.Add(new LigneInfo
                        {
                            X1 = debut?.x ?? 0,
                            Y1 = debut?.y ?? 0,
                            X2 = fin?.x ?? 0,
                            Y2 = fin?.y ?? 0,
                            EstConstruction = estConstr
                        });
                    }
                    else if (elt is Axis2D axe)
                    {
                        bool estConstr = false;
                        try { estConstr = axe.Construction; } catch { }
                        Console.WriteLine($"[EstConstr/{nomSketch}] Axis2D[{i}] Construction={estConstr}");

                        // Axe par défaut (H/V) : Construction=false, ignoré
                        if (estConstr)
                        {
                            var axeInfo = new AxeInfo { EstConstruction = true };
                            try
                            {
                                var orig = GetPoint2DCoordinates(axe.Origin);
                                axeInfo.OrigineX = orig?.x ?? 0;
                                axeInfo.OrigineY = orig?.y ?? 0;
                                Console.WriteLine($"[EstConstr/{nomSketch}] Axis2D[{i}] Origine=({axeInfo.OrigineX},{axeInfo.OrigineY})");
                            }
                            catch (Exception ex) { var r = CauseReelle(ex); Console.WriteLine($"[EstConstr/{nomSketch}] Axis2D[{i}] Origin ECHEC : {r.Message}"); }
                            try
                            {
                                Line2D hRef = axe.HorizontalReference;
                                if (hRef != null)
                                {
                                    var p1 = GetPoint2DCoordinates(hRef.StartPoint);
                                    var p2 = GetPoint2DCoordinates(hRef.EndPoint);
                                    axeInfo.RefHorizontaleX1 = p1?.x ?? 0; axeInfo.RefHorizontaleY1 = p1?.y ?? 0;
                                    axeInfo.RefHorizontaleX2 = p2?.x ?? 0; axeInfo.RefHorizontaleY2 = p2?.y ?? 0;
                                }
                            }
                            catch { }
                            try
                            {
                                Line2D vRef = axe.VerticalReference;
                                if (vRef != null)
                                {
                                    var p1 = GetPoint2DCoordinates(vRef.StartPoint);
                                    var p2 = GetPoint2DCoordinates(vRef.EndPoint);
                                    axeInfo.RefVerticaleX1 = p1?.x ?? 0; axeInfo.RefVerticaleY1 = p1?.y ?? 0;
                                    axeInfo.RefVerticaleX2 = p2?.x ?? 0; axeInfo.RefVerticaleY2 = p2?.y ?? 0;
                                }
                            }
                            catch { }
                            geo.Axes.Add(axeInfo);
                        }
                    }
                    else if (elt is Point2D)
                    {
                        Console.WriteLine($"[EstConstr/{nomSketch}] Point2D[{i}] ignoré");
                    }
                    else
                    {
                        string typeElt = elt?.GetType().Name ?? "null";
                        object rawConstr = TryDynamicMember(elt, "Construction");
                        Console.WriteLine($"[EstConstr/{nomSketch}] autre[{i}] runtime={typeElt}  Construction={rawConstr ?? "null"}");

                        var centreObj = TryDynamicMember(elt, "CenterPoint", "Center");
                        var centre = centreObj != null ? GetPoint2DCoordinates(centreObj) : null;
                        double? rayon = ExtraireValeurNumerique(TryDynamicMember(elt, "Radius"));

                        if (centre.HasValue && rayon.HasValue)
                        {
                            double? angleDebut = ExtraireValeurNumerique(TryDynamicMember(elt, "StartAngle"));
                            double? angleFin = ExtraireValeurNumerique(TryDynamicMember(elt, "EndAngle"));
                            // Construction sur Geometry2D (base confirmée par réflexion)
                            bool estConstr = false;
                            try { estConstr = Convert.ToBoolean(TryDynamicMember(elt, "Construction")); } catch { }

                            geo.Arcs.Add(new ArcInfo
                            {
                                CentreX = centre.Value.x,
                                CentreY = centre.Value.y,
                                Rayon = rayon.Value,
                                AngleDebut = angleDebut ?? 0,
                                AngleFin = angleFin ?? 0,
                                EstConstruction = estConstr
                            });
                        }
                        else
                        {
                            Console.WriteLine($"[ExtraireSketch/{nomSketch}] autre[{i}] {typeElt} : CenterPoint={(centreObj != null ? "trouvé" : "null")}  Radius={(rayon.HasValue ? $"{rayon.Value}" : "null")} — arc non capturé");
                        }
                    }
                }
                catch { }
            }
        }
        catch { }

        try
        {
            Constraints constraints = sketch.Constraints;
            for (int i = 1; i <= constraints.Count; i++)
            {
                try
                {
                    Constraint c = constraints.Item(i);
                    double? valeur = null;
                    try { valeur = c.Dimension?.Value; } catch { }

                    geo.Contraintes.Add(new ContrainteInfo
                    {
                        Type = c.Type.ToString(),
                        Valeur = valeur
                    });
                }
                catch { }
            }
        }
        catch { }

        return new FeatureInfo
        {
            Ordre = ordre,
            Type = "Sketch",
            Nom = sketch.get_Name(),
            Geometrie = geo
        };
    }

    // Parcourt part.HybridBodies → HybridBody.HybridShapes pour capturer les plans de référence.
    // HybridShapePlane* n'existe pas dans les DLL d'interop statiques (confirmé par réflexion) :
    // les sous-types sont détectés dynamiquement en sondant leurs propriétés COM.
    private List<PlanReference> ExtrairePlans(Part part, Body body)
    {
        var plans = new List<PlanReference>();
        bool dumpPlanOffsetFait = false;

        // Dump réflexion exécuté une seule fois pour HybridShapePlaneOffset,
        // dans l'une ou l'autre des deux boucles ci-dessous.
        void DumperOffset(HybridShape hs, string nom)
        {
            if (dumpPlanOffsetFait) return;
            dumpPlanOffsetFait = true;
            Console.WriteLine($"[ExtrairePlans/{nom}] === DUMP HybridShapePlaneOffset — runtime {hs.GetType().Name} — GetProperties() ===");
            foreach (var pi in hs.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                try
                {
                    object val = pi.GetValue(hs);
                    Console.WriteLine($"[ExtrairePlans/{nom}]   PROP {pi.Name} ({pi.PropertyType.Name}) → {val ?? "null"}");
                }
                catch (Exception ex) { Console.WriteLine($"[ExtrairePlans/{nom}]   PROP {pi.Name} ECHEC : {CauseReelle(ex).Message}"); }
            }
            Console.WriteLine($"[ExtrairePlans/{nom}] === DUMP HybridShapePlaneOffset — GetMethods() ===");
            foreach (var mi in hs.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var sb = new System.Text.StringBuilder();
                foreach (var p in mi.GetParameters())
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append($"{p.ParameterType.Name} {p.Name}");
                }
                Console.WriteLine($"[ExtrairePlans/{nom}]   METH {mi.Name}({sb}) → {mi.ReturnType.Name}");
            }
        }

        // OriginElements : plans de base de la pièce (XY, YZ, ZX) — toujours loggués
        // pour distinguer "aucun plan user" de "boucle HybridBodies cassée".
        try
        {
            var origObj = TryDynamicMember(part, "OriginElements");
            if (origObj != null)
            {
                string xy = TryDynamicMember(origObj, "PlaneXY")?.ToString() ?? "(nul)";
                string yz = TryDynamicMember(origObj, "PlaneYZ")?.ToString() ?? "(nul)";
                string zx = TryDynamicMember(origObj, "PlaneZX")?.ToString() ?? "(nul)";
                Console.WriteLine($"[ExtrairePlans] OriginElements → PlaneXY={xy}, PlaneYZ={yz}, PlaneZX={zx}");
            }
            else
            {
                Console.WriteLine("[ExtrairePlans] OriginElements → null (TryDynamicMember a échoué)");
            }
        }
        catch (Exception ex)
        {
            var r = CauseReelle(ex);
            Console.WriteLine($"[ExtrairePlans] OriginElements ECHEC : {r.GetType().Name} — {r.Message}");
        }

        try
        {
            object hbObj = TryDynamicMember(part, "HybridBodies");
            Console.WriteLine($"[ExtrairePlans] TryDynamicMember(part, \"HybridBodies\") → {(hbObj == null ? "null" : hbObj.GetType().FullName)}");

            if (hbObj is not HybridBodies hybridBodies)
            {
                Console.WriteLine("[ExtrairePlans] Cast 'is HybridBodies' ECHOUE — retour liste vide.");
                return plans;
            }

            Console.WriteLine($"[ExtrairePlans] HybridBodies.Count = {hybridBodies.Count}");

            for (int i = 1; i <= hybridBodies.Count; i++)
            {
                try
                {
                    HybridBody hb = hybridBodies.Item(i);
                    HybridShapes hybridShapes = hb.HybridShapes;
                    Console.WriteLine($"[ExtrairePlans] HybridBody[{i}] = \"{hb.get_Name()}\", HybridShapes.Count = {hybridShapes.Count}");

                    for (int j = 1; j <= hybridShapes.Count; j++)
                    {
                        try
                        {
                            HybridShape hs = hybridShapes.Item(j);
                            var plan = new PlanReference
                            {
                                Nom = hs.get_Name(),
                                Type = DetecterTypeHybridShape(hs)
                            };

                            if (plan.Type == "HybridShapePlaneOffset")
                                DumperOffset(hs, plan.Nom);

                            try
                            {
                                plan.Offset = ExtraireValeurNumerique(TryDynamicMember(hs, "Offset"));
                            }
                            catch (Exception ex)
                            {
                                var r = CauseReelle(ex);
                                Console.WriteLine($"[ExtrairePlans/{plan.Nom}] Offset ECHEC : {r.GetType().Name} — {r.Message}");
                            }

                            try
                            {
                                var refObj = TryDynamicMember(hs, "Reference", "Element", "FirstElement");
                                if (refObj is AnyObject refAny)
                                    plan.ElementReference = refAny.get_Name();
                                else if (refObj != null)
                                    plan.ElementReference = TryDynamicMember(refObj, "DisplayName", "get_Name", "Name")?.ToString();
                            }
                            catch (Exception ex)
                            {
                                var r = CauseReelle(ex);
                                Console.WriteLine($"[ExtrairePlans/{plan.Nom}] ElementReference ECHEC : {r.GetType().Name} — {r.Message}");
                            }

                            plans.Add(plan);
                        }
                        catch (Exception ex)
                        {
                            var r = CauseReelle(ex);
                            Console.WriteLine($"[ExtrairePlans] HybridShapes.Item({j}) ECHEC : {r.GetType().Name} — {r.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    var r = CauseReelle(ex);
                    Console.WriteLine($"[ExtrairePlans] HybridBodies.Item({i}) ECHEC : {r.GetType().Name} — {r.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            var r = CauseReelle(ex);
            Console.WriteLine($"[ExtrairePlans] ERREUR GLOBALE part.HybridBodies : {r.GetType().Name} — {r.Message}");
        }

        // Body.HybridShapes : plans nichés directement dans PartBody
        // (confirmé par réflexion : Body expose HybridShapes comme propriété statique)
        try
        {
            HybridShapes bodyHs = body.HybridShapes;
            Console.WriteLine($"[ExtrairePlans] body.HybridShapes.Count = {bodyHs.Count}");

            for (int j = 1; j <= bodyHs.Count; j++)
            {
                try
                {
                    HybridShape hs = bodyHs.Item(j);
                    string nomHs  = hs.get_Name();
                    string typeHs = DetecterTypeHybridShape(hs);
                    Console.WriteLine($"[ExtrairePlans] body.HybridShapes[{j}] nom=\"{nomHs}\"  type_detect={typeHs}  runtime={hs.GetType().Name}");

                    if (typeHs == "HybridShapePlaneOffset")
                        DumperOffset(hs, nomHs);

                    if (!typeHs.Contains("HybridShapePlane"))
                    {
                        Console.WriteLine($"[ExtrairePlans] body.HybridShapes[{j}] ignoré (non-plan)");
                        continue;
                    }

                    var plan = new PlanReference { Nom = nomHs, Type = typeHs };

                    try { plan.Offset = ExtraireValeurNumerique(TryDynamicMember(hs, "Offset")); }
                    catch (Exception ex) { var r = CauseReelle(ex); Console.WriteLine($"[ExtrairePlans/{nomHs}] Offset ECHEC : {r.GetType().Name} — {r.Message}"); }

                    try
                    {
                        var refObj = TryDynamicMember(hs, "Reference", "Element", "FirstElement");
                        if (refObj is AnyObject refAny)
                            plan.ElementReference = refAny.get_Name();
                        else if (refObj != null)
                            plan.ElementReference = TryDynamicMember(refObj, "DisplayName", "get_Name", "Name")?.ToString();
                    }
                    catch (Exception ex) { var r = CauseReelle(ex); Console.WriteLine($"[ExtrairePlans/{nomHs}] ElementReference ECHEC : {r.GetType().Name} — {r.Message}"); }

                    plans.Add(plan);
                }
                catch (Exception ex)
                {
                    var r = CauseReelle(ex);
                    Console.WriteLine($"[ExtrairePlans] body.HybridShapes.Item({j}) ECHEC : {r.GetType().Name} — {r.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            var r = CauseReelle(ex);
            Console.WriteLine($"[ExtrairePlans] body.HybridShapes ECHEC : {r.GetType().Name} — {r.Message}");
        }

        return plans;
    }

    private static FeatureInfo FeatureNonGeree(Shape shape, List<string> sketchRefs, int ordre)
    {
        Console.WriteLine($"[ExtraireFeaturesOrdonnees] ATTENTION : type de feature non géré : {shape.GetType().Name}, nom={shape.get_Name()} — cette feature ne sera pas reconstructible fidèlement");
        return new FeatureInfo
        {
            Ordre = ordre,
            Type = shape.GetType().Name,
            Nom = shape.get_Name(),
            SketchReferences = sketchRefs
        };
    }

    private List<RelationInfo> ExtraireRelations(Part part)
    {
        var liste = new List<RelationInfo>();
        try
        {
            object relCol = TryDynamicMember(part, "Relations");
            if (relCol == null)
            {
                Console.WriteLine("[ExtraireRelations] part.Relations → null");
                return liste;
            }
            Console.WriteLine($"[ExtraireRelations] part.Relations → runtime={relCol.GetType().Name}");

            object countObj = TryDynamicMember(relCol, "Count");
            if (countObj == null) { Console.WriteLine("[ExtraireRelations] Count → null"); return liste; }
            int count = Convert.ToInt32(countObj);
            Console.WriteLine($"[ExtraireRelations] Count={count}");

            for (int i = 1; i <= count; i++)
            {
                try
                {
                    object rel = relCol.GetType().InvokeMember("Item",
                        BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance,
                        null, relCol, new object[] { i });
                    if (rel == null) continue;

                    string nom = TryDynamicMember(rel, "get_Name", "Name")?.ToString() ?? $"Relation_{i}";
                    // Body donne le texte de la formule/règle ; Value donne la valeur calculée (nombre)
                    string formule = TryDynamicMember(rel, "Body", "get_Body", "Value", "get_Value", "Expression")?.ToString();

                    Console.WriteLine($"[ExtraireRelations] [{i}] nom=\"{nom}\"  formule=\"{formule ?? "(null)"}\"");
                    liste.Add(new RelationInfo { Nom = nom, Formule = formule });
                }
                catch (Exception ex)
                {
                    var r = CauseReelle(ex);
                    Console.WriteLine($"[ExtraireRelations] Item({i}) ECHEC : {r.GetType().Name} — {r.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            var r = CauseReelle(ex);
            Console.WriteLine($"[ExtraireRelations] ERREUR GLOBALE : {r.GetType().Name} — {r.Message}");
        }
        return liste;
    }

    // Déduit le sous-type d'un HybridShape en sondant ses propriétés COM caractéristiques.
    private static string DetecterTypeHybridShape(HybridShape hs)
    {
        bool hasOffset = false;
        bool hasAngle = false;
        bool hasReference = false;

        try { hasOffset = TryDynamicMember(hs, "Offset") != null; } catch { }
        try { hasAngle = !hasOffset && TryDynamicMember(hs, "Angle") != null; } catch { }
        try { hasReference = TryDynamicMember(hs, "Reference") != null; } catch { }

        if (hasOffset) return "HybridShapePlaneOffset";
        if (hasAngle) return "HybridShapePlaneAngle";
        if (hasReference) return "HybridShapeExplicit";
        return hs.GetType().Name;
    }
}