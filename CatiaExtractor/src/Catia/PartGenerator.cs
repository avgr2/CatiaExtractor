using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using INFITF;
using MECMOD;
using PARTITF;

public class PartGenerator
{
    private readonly Application _catia;

    public PartGenerator(Application catia)
    {
        _catia = catia;
    }

    // Phase 1 : génère uniquement les Sketches de Cylinder.CATPart.
    // Les autres types (Pad, Pocket, Mirror, Fillet…) sont stubbés avec un log clair.
    public void GenererCylinder(string cheminJson, string cheminSortie)
    {
        string json = System.IO.File.ReadAllText(cheminJson);
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var root = JsonSerializer.Deserialize<JsonRootDto>(json, opts);
        Piece piece = root.Pieces.First(p => p.Fichier.Contains("Cylinder"));
        GenererPiece(piece, cheminSortie);
    }

    private void GenererPiece(Piece piece, string cheminSortie)
    {
        PartDocument partDoc = (PartDocument)_catia.Documents.Add("Part");
        Part part = partDoc.Part;
        Body body = part.Bodies.Item(1);

        // Sketches créés indexés par nom — utilisés ensuite pour Pad/Pocket
        var sketchesCrees = new Dictionary<string, Sketch>(StringComparer.OrdinalIgnoreCase);
        string featureEnCours = "(init)";

        try
        {
            foreach (var feature in piece.Features.OrderBy(f => f.Ordre))
            {
                featureEnCours = $"{feature.Type}/{feature.Nom}";
                Console.WriteLine($"[PartGenerator] ▶ {featureEnCours}");

                try
                {
                    switch (feature.Type)
                    {
                        case "Sketch":
                            var sk = CreerSketch(part, body, feature);
                            if (sk != null) sketchesCrees[feature.Nom] = sk;
                            break;

                        case "Pad":
                        case "Pocket":
                        case "Shaft":
                        case "Groove":
                        case "Rib":
                        case "Slot":
                        case "Mirror":
                            Console.WriteLine($"[PartGenerator/{feature.Nom}] → non implémenté (phase 1 : sketches uniquement)");
                            break;

                        case "EdgeFillet":
                        case "Chamfer":
                            Console.WriteLine($"[PartGenerator/{feature.Nom}] → ignoré (références d'arêtes non disponibles dans le JSON)");
                            break;

                        default:
                            Console.WriteLine($"[PartGenerator/{feature.Nom}] ATTENTION : type '{feature.Type}' inconnu — ignoré");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PartGenerator] ERREUR feature '{featureEnCours}' : {ex.GetType().Name} — {ex.Message}");
                }

                // Update après chaque feature : wrappé séparément pour rester diagnostiquable
                // même si la feature elle-même a réussi
                try { part.Update(); }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PartGenerator] Part.Update() ECHEC après '{featureEnCours}' : {ex.GetType().Name} — {ex.Message}");
                }
            }
        }
        finally
        {
            try
            {
                partDoc.SaveAs(cheminSortie);
                partDoc.Close();
                Console.WriteLine($"[PartGenerator] Pièce sauvegardée → {cheminSortie}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PartGenerator] SaveAs ECHEC : {ex.GetType().Name} — {ex.Message}");
            }
        }
    }

    private Sketch CreerSketch(Part part, Body body, FeatureInfo f)
    {
        var pl = f.Geometrie?.Placement;
        if (pl == null)
        {
            Console.WriteLine($"[PartGenerator/{f.Nom}] Placement absent — sketch ignoré");
            return null;
        }

        AnyObject planSupport = ResoldrePlan(part, pl, f.Nom);
        if (planSupport == null) return null;

        // Sketches.Add attend une Reference, pas un AnyObject
        Reference planRef = part.CreateReferenceFromObject(planSupport);
        Sketch sketch = body.Sketches.Add(planRef);
        sketch.OpenEdition();
        Factory2D f2d = sketch.Factory2D;

        foreach (var cercle in f.Geometrie.Cercles)
        {
            try
            {
                // Cercle complet : startAngle=0, endAngle=2π (radians)
                Circle2D c = f2d.CreateCircle(cercle.CentreX, cercle.CentreY, cercle.Rayon, 0, 2.0 * Math.PI);
                if (cercle.EstConstruction) c.Construction = true;
                Console.WriteLine($"[PartGenerator/{f.Nom}] cercle ok ({cercle.CentreX:F3},{cercle.CentreY:F3}) r={cercle.Rayon:F3} constr={cercle.EstConstruction}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PartGenerator/{f.Nom}] Cercle ({cercle.CentreX:F3},{cercle.CentreY:F3}) r={cercle.Rayon:F3} ECHEC : {ex.GetType().Name} — {ex.Message}");
            }
        }

        foreach (var ligne in f.Geometrie.Lignes)
        {
            try
            {
                Line2D l = f2d.CreateLine(ligne.X1, ligne.Y1, ligne.X2, ligne.Y2);
                if (ligne.EstConstruction) l.Construction = true;
                Console.WriteLine($"[PartGenerator/{f.Nom}] ligne ok ({ligne.X1:F3},{ligne.Y1:F3})→({ligne.X2:F3},{ligne.Y2:F3}) constr={ligne.EstConstruction}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PartGenerator/{f.Nom}] Ligne ({ligne.X1:F3},{ligne.Y1:F3})→({ligne.X2:F3},{ligne.Y2:F3}) ECHEC : {ex.GetType().Name} — {ex.Message}");
            }
        }

        sketch.CloseEdition();
        Console.WriteLine($"[PartGenerator/{f.Nom}] ✓ sketch fermé — {f.Geometrie.Cercles.Count} cercle(s), {f.Geometrie.Lignes.Count} ligne(s), {f.Geometrie.Arcs.Count} arc(s)");
        return sketch;
    }

    // Détermine le plan support CATIA à partir du Placement3D.
    // Normale = AxeX × AxeY (tolérance ε=1e-6 pour absorber le bruit flottant).
    // Offset = projection scalaire de l'origine sur la normale.
    // Plans standard XY/YZ/ZX à l'origine → OriginElements directement.
    // Plans standard décalés → HybridShapeFactory.AddNewPlaneOffset (late binding).
    // Plans non standards (inclinés) → non implémentés.
    private static AnyObject ResoldrePlan(Part part, Placement3D p, string nomSketch)
    {
        const double ε = 1e-6;

        double nx = p.AxeXy * p.AxeYz - p.AxeXz * p.AxeYy;
        double ny = p.AxeXz * p.AxeYx - p.AxeXx * p.AxeYz;
        double nz = p.AxeXx * p.AxeYy - p.AxeXy * p.AxeYx;

        OriginElements orig = part.OriginElements;
        AnyObject basePlane;
        string planNom;

        // XY : normale ≈ (0, 0, ±1)
        if (Math.Abs(nx) < ε && Math.Abs(ny) < ε && Math.Abs(Math.Abs(nz) - 1.0) < ε)
        {
            basePlane = (AnyObject)orig.PlaneXY;
            planNom = "XY";
        }
        // YZ : normale ≈ (±1, 0, 0)
        else if (Math.Abs(Math.Abs(nx) - 1.0) < ε && Math.Abs(ny) < ε && Math.Abs(nz) < ε)
        {
            basePlane = (AnyObject)orig.PlaneYZ;
            planNom = "YZ";
        }
        // ZX : normale ≈ (0, ±1, 0)
        else if (Math.Abs(nx) < ε && Math.Abs(Math.Abs(ny) - 1.0) < ε && Math.Abs(nz) < ε)
        {
            basePlane = (AnyObject)orig.PlaneZX;
            planNom = "ZX";
        }
        else
        {
            Console.WriteLine($"[PartGenerator/{nomSketch}] Plan non standard — normale=({nx:F6},{ny:F6},{nz:F6}) — non implémenté, sketch ignoré");
            return null;
        }

        // Offset signé = dot(normale, origine) — seule composante pertinente pour un plan standard
        double offset = nx * p.OrigineX + ny * p.OrigineY + nz * p.OrigineZ;

        if (Math.Abs(offset) < ε)
        {
            Console.WriteLine($"[PartGenerator/{nomSketch}] → plan {planNom} à l'origine (normale=({nx:F6},{ny:F6},{nz:F6}))");
            return basePlane;
        }

        Console.WriteLine($"[PartGenerator/{nomSketch}] → plan {planNom} décalé de {offset:F6} mm — création HybridShapePlaneOffset");
        return CreerPlanOffset(part, basePlane, offset, nomSketch);
    }

    // Crée un plan parallèle à basePlane décalé de offset mm via HybridShapeFactory (late binding,
    // car HybridShapeFactory est dans GSMITF.dll qui n'a pas de wrapper .NET en B19).
    // offset signé : positif = dans le sens de la normale, négatif = sens inverse.
    private static AnyObject CreerPlanOffset(Part part, AnyObject basePlane, double offset, string nomSketch)
    {
        try
        {
            object hsFactory = part.GetType().InvokeMember(
                "HybridShapeFactory", BindingFlags.GetProperty, null, part, null);

            Reference basePlaneRef = part.CreateReferenceFromObject(basePlane);
            bool reverseDirection = (offset < 0);
            double distance = Math.Abs(offset);

            object planOffset = hsFactory.GetType().InvokeMember(
                "AddNewPlaneOffset", BindingFlags.InvokeMethod, null, hsFactory,
                new object[] { basePlaneRef, distance, reverseDirection });

            // Insérer dans un HybridBody existant ou nouveau pour que CATIA valide le plan
            object hybridBodies = part.GetType().InvokeMember(
                "HybridBodies", BindingFlags.GetProperty, null, part, null);

            int countHB = (int)hybridBodies.GetType().InvokeMember(
                "Count", BindingFlags.GetProperty, null, hybridBodies, null);

            object hybridBody = countHB > 0
                ? hybridBodies.GetType().InvokeMember("Item", BindingFlags.InvokeMethod, null, hybridBodies, new object[] { 1 })
                : hybridBodies.GetType().InvokeMember("Add", BindingFlags.InvokeMethod, null, hybridBodies, null);

            hybridBody.GetType().InvokeMember(
                "AppendHybridShape", BindingFlags.InvokeMethod, null, hybridBody,
                new object[] { planOffset });

            part.Update();
            Console.WriteLine($"[PartGenerator/{nomSketch}] ✓ plan offset créé (d={distance:F4} mm, reverse={reverseDirection})");
            return (AnyObject)planOffset;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PartGenerator/{nomSketch}] CreerPlanOffset ECHEC : {ex.GetType().Name} — {ex.Message}");
            return null;
        }
    }

    private class JsonRootDto
    {
        public List<Piece> Pieces { get; set; }
    }
}
