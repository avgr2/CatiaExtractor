using System.Collections.Generic;

public class Piece
{
    public string Nom { get; set; }
    public string Fichier { get; set; }
    public Dictionary<string, double> Dimensions { get; set; } = new();

    // Features et sketches dans l'ordre chronologique de création/dépendance
    public List<FeatureInfo> Features { get; set; } = new();

    // Plans de référence extraits des HybridBodies (Geometrical Sets)
    public List<PlanReference> Plans { get; set; } = new();

    // Formules et relations issues de part.Relations
    public List<RelationInfo> Relations { get; set; } = new();
}
