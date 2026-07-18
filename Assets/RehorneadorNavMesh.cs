using UnityEngine;
using Unity.AI.Navigation;

// Re-hornea el NavMesh una vez al amanecer, tras la construcción nocturna.
// Requiere un NavMeshSurface que cubra el piso de la tienda (asignado en el Inspector).
public class RehorneadorNavMesh : MonoBehaviour
{
    [Tooltip("El NavMeshSurface que cubre la tienda")]
    public NavMeshSurface superficie;

    void OnEnable()
    {
        if (GameClock.Instancia != null)
            GameClock.Instancia.OnAmanecer += Rehornear;
    }

    void OnDisable()
    {
        if (GameClock.Instancia != null)
            GameClock.Instancia.OnAmanecer -= Rehornear;
    }

    void Rehornear()
    {
        if (superficie != null)
            superficie.BuildNavMesh();
    }
}
