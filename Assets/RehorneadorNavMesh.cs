using UnityEngine;
using Unity.AI.Navigation;

// Re-hornea el NavMesh una vez al amanecer, tras la construcción nocturna.
// Requiere un NavMeshSurface que cubra el piso de la tienda (asignado en el Inspector).
public class RehorneadorNavMesh : MonoBehaviour
{
    [Tooltip("El NavMeshSurface que cubre la tienda")]
    public NavMeshSurface superficie;

    void Start()
    {
        if (GameClock.Instancia != null)
            GameClock.Instancia.OnAmanecer += Rehornear;
        else
            Debug.LogWarning("RehorneadorNavMesh: no hay GameClock en la escena; el re-horneado al amanecer no correrá.");
    }

    void OnDestroy()
    {
        if (GameClock.Instancia != null)
            GameClock.Instancia.OnAmanecer -= Rehornear;
    }

    void Rehornear()
    {
        if (superficie != null)
            superficie.BuildNavMesh();
        else
            Debug.LogWarning("RehorneadorNavMesh: 'superficie' sin asignar; no se puede re-hornear.");
    }
}
