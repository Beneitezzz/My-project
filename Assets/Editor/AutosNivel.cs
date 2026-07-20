using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Coloca autos abandonados (pack Quaternius) sobre la calzada, como decorado post-apo.
// Corré: Herramientas > Nivel > Colocar autos. Idempotente (rehace el grupo) + undo.
// Es un pase MECÁNICO: posiciones y rumbos fijos; ajustá a gusto después a mano.
// Nota: son visuales (sin colliders). Si querés que el jugador no los atraviese,
// activá "Generate Colliders" en el import de los FBX de autos (un toggle para todos).
public static class AutosNivel
{
    const string CarpetaFBX = "Assets/Realistic Car Pack - Nov 2018/FBX/";

    struct Auto { public string modelo; public float x, z, rumbo; }

    // Calzada = Z 3..10, X 0..42 (sellada en los extremos). Los clientes no la usan, así que
    // los autos son puro escenario. Dos coinciden con los placeholders Auto_1 (~10,6) y Auto_2 (~30,7).
    static readonly Auto[] Autos = new Auto[]
    {
        new Auto { modelo = "SUV",        x = 10f, z = 6.5f, rumbo = 20f },
        new Auto { modelo = "Taxi",       x = 30f, z = 7.0f, rumbo = -25f },
        new Auto { modelo = "NormalCar1", x = 18f, z = 4.5f, rumbo = 100f },
        new Auto { modelo = "Cop",        x = 38f, z = 6.0f, rumbo = 80f },
        new Auto { modelo = "SportsCar",  x = 24f, z = 8.5f, rumbo = 10f },
        new Auto { modelo = "NormalCar2", x = 6f,  z = 8.0f, rumbo = 200f },
    };

    [MenuItem("Herramientas/Nivel/Colocar autos")]
    public static void Colocar()
    {
        int grupoUndo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Colocar autos");

        var arte = GameObject.Find("_Arte");
        if (arte == null)
        {
            arte = new GameObject("_Arte");
            Undo.RegisterCreatedObjectUndo(arte, "Colocar autos");
        }

        // Idempotencia: borrar el grupo de autos previo.
        var previo = arte.transform.Find("Autos");
        if (previo != null) Undo.DestroyObjectImmediate(previo.gameObject);

        var grupo = new GameObject("Autos");
        Undo.RegisterCreatedObjectUndo(grupo, "Colocar autos");
        grupo.transform.SetParent(arte.transform, false);

        int puestos = 0;
        foreach (var a in Autos)
        {
            string ruta = CarpetaFBX + a.modelo + ".fbx";
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            if (fbx == null)
            {
                Debug.LogWarning("No encontré el modelo: " + ruta);
                continue;
            }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            inst.transform.SetParent(grupo.transform, false);
            // Mantener la rotación horneada del FBX (auto parado sobre las ruedas) y sumarle el
            // rumbo alrededor del eje vertical del mundo, así no se tumba por el rot X=90 del import.
            inst.transform.rotation = Quaternion.AngleAxis(a.rumbo, Vector3.up) * inst.transform.rotation;
            inst.transform.position = new Vector3(a.x, 0f, a.z);
            Undo.RegisterCreatedObjectUndo(inst, "Colocar autos");
            puestos++;
        }

        Selection.activeGameObject = grupo;
        Undo.CollapseUndoOperations(grupoUndo);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Autos colocados: {puestos}/{Autos.Length} sobre la calzada, bajo _Arte/Autos. " +
                  "Si alguno flota o se hunde, ajustá su Y. Ctrl+Z para deshacer todo.");
    }
}
