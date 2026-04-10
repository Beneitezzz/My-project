using UnityEngine;

[CreateAssetMenu(fileName = "NuevoItem", menuName = "Ferreteria/Item")]
public class ItemData : ScriptableObject
{
    public string nombreProducto;
    public Sprite icono;
    public float precio;
    // Quitamos la capacidad de acá, porque el producto no sabe dónde lo van a guardar.
}