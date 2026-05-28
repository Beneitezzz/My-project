using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "NuevoItem", menuName = "Ferreteria/Item")]
public class ItemData : ScriptableObject
{
    public string nombreProducto;
    public Sprite icono;
    [FormerlySerializedAs("precio")]
    public float precioBase;
}
