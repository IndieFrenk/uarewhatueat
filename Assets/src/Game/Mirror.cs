using UnityEngine;

/// <summary>
/// Specchio: Interactable che descrive il marchio del protagonista.
/// Il protagonista non vede la propria fronte se non allo specchio.
/// </summary>
public class Mirror : Interactable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Hook() => GameBootstrap.Ready += () => new GameObject(nameof(Mirror)).AddComponent<Mirror>();

    void Start()
    {
        gameObject.name = nameof(Mirror);
        if (WorldAnchors.HomeSpawn == null) { Destroy(gameObject); return; }

        // Quad verticale segnaposto
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.SetParent(transform);
        quad.transform.localPosition = Vector3.zero;
        quad.transform.localRotation = Quaternion.identity;
        quad.transform.localScale = new Vector3(0.8f, 1.2f, 1f);
        Destroy(quad.GetComponent<Collider>());
        Destroy(quad.GetComponent<MeshRenderer>());

        var col = gameObject.AddComponent<BoxCollider>();
        col.size = new Vector3(0.8f, 1.2f, 0.1f);
        col.isTrigger = true;

        gameObject.AddComponent<Rigidbody>().isKinematic = true;
        gameObject.transform.position = WorldAnchors.HomeSpawn.position + Vector3.left * 1.5f;
    }

    public override string Prompt => "Guardati allo specchio";
    public override bool CanInteract => GameState.Current?.Protagonist != null;

    public override void Interact()
    {
        var protagonist = GameState.Current.Protagonist;
        if (protagonist == null) return;

        string desc = protagonist.Mark switch
        {
            >= 0.8f => "Vedi il marchio blu sulla tua fronte, netto e scuro. Ti guarda dritto negli occhi.",
            >= 0.5f => "Sulla tua fronte c'è ancora il marchio, ma più sfumato. Meno sicuro di te.",
            >= 0.35f => "Il segno è quasi sparito. Ti senti... vulnerabile.",
            _ => "La tua fronte è pulita. Nessuna protezione.",
        };

        UIKit.Toast(desc, 4f);
    }
}
