using UnityEngine;

[CreateAssetMenu(fileName = "NuevaMejora", menuName = "Ferreteria/Mejora")]
public class MejoraData : ScriptableObject
{
    public string nombreMejora;
    public float precio;
    public GameObject prefabMejora; // El estante real que va a aparecer
    [TextArea] public string descripcion;
}