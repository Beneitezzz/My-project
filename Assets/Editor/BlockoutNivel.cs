using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;

// Genera el greybox de la Zona 1 (mapa de la ciudad) por código, para NO editar la escena a mano.
// Uso: Herramientas > Nivel > Generar blockout Zona 1.
// Es idempotente (borra el _Nivel previo antes de reconstruir) y se deshace con Ctrl+Z.
// Es BLOCKOUT: geometría de bloqueo + materiales placeholder + marcadores. El arte final es otra tarea.
//
// Coordenadas (metros, Y arriba): X = oeste(0) -> este(42). Z = sur -> norte en bandas.
public static class BlockoutNivel
{
    // --- Dimensiones ---
    const float AlturaMuro = 3f;
    const float Espesor = 0.3f;
    const float EspesorPiso = 0.2f;

    // Bandas en Z (sur -> norte)
    const float SurRuinaMin = -8f, SurRuinaMax = 0f;
    const float CalzadaMin = 3f, CalzadaMax = 10f;
    const float VeredaSurMin = 0f, VeredaSurMax = 3f;
    const float VeredaNorteMin = 10f, VeredaNorteMax = 13f;
    const float NorteRuinaMin = 13f, NorteRuinaMax = 21f;

    // Largo de la cuadra en X
    const float CuadraMin = 0f, CuadraMax = 42f;

    [MenuItem("Herramientas/Nivel/Generar blockout Zona 1")]
    public static void Generar()
    {
        // Idempotencia: borrar el blockout anterior (undo-able).
        var previo = GameObject.Find("_Nivel");
        if (previo != null) Undo.DestroyObjectImmediate(previo);

        var root = new GameObject("_Nivel");
        Undo.RegisterCreatedObjectUndo(root, "Generar blockout Zona 1");

        // Materiales placeholder (URP Lit, color plano).
        var matPiso = Mat("mat_piso", new Color(0.50f, 0.50f, 0.50f));
        var matFerreteria = Mat("mat_ferreteria", new Color(0.75f, 0.20f, 0.18f));
        var matRuina = Mat("mat_ruina", new Color(0.33f, 0.33f, 0.35f));
        var matVereda = Mat("mat_vereda", new Color(0.55f, 0.55f, 0.55f));
        var matCalzada = Mat("mat_calzada", new Color(0.24f, 0.24f, 0.26f));
        var matAuto = Mat("mat_auto", new Color(0.70f, 0.35f, 0.15f));
        var matEscombros = Mat("mat_escombros", new Color(0.50f, 0.40f, 0.25f));

        ConstruirCalle(Carpeta("Calle", root.transform), matVereda, matCalzada, matAuto);
        ConstruirFerreteria(Carpeta("Ferreteria", root.transform), matPiso, matFerreteria);
        ConstruirRuinas(Carpeta("Ruinas", root.transform), matPiso, matRuina);
        ConstruirLimites(Carpeta("Limites", root.transform), matEscombros, matRuina);
        ConstruirPuntos(Carpeta("PuntosInteres", root.transform));
        ConstruirMarcadores(Carpeta("_MarcadoresReservados", root.transform));
        ReubicarGenerador();

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Blockout Zona 1 generado bajo '_Nivel'. Revisá la escena y guardá (Ctrl+S). Ctrl+Z para deshacer.");
    }

    // ---------- Helpers de construcción ----------

    static GameObject Carpeta(string nombre, Transform parent)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(parent, false);
        return go;
    }

    static Material Mat(string nombre, Color color)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Blockout"))
            AssetDatabase.CreateFolder("Assets/Materials", "Blockout");

        string path = "Assets/Materials/Blockout/" + nombre + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.1f);
            mat.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    // Cubo primitivo (trae BoxCollider) posicionado por centro y tamaño.
    static GameObject Caja(string nombre, Transform parent, Vector3 centro, Vector3 tam, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = centro;
        go.transform.localScale = tam;
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    // Piso delgado con la cara de arriba en Y=0.
    static GameObject Piso(string nombre, Transform parent, float xMin, float xMax, float zMin, float zMax, Material mat)
    {
        return Caja(nombre, parent,
            new Vector3((xMin + xMax) / 2f, -EspesorPiso / 2f, (zMin + zMax) / 2f),
            new Vector3(xMax - xMin, EspesorPiso, zMax - zMin), mat);
    }

    // Muro que corre a lo largo de X (delgado en Z). baseY/altura permiten alfeizar y dintel (vidriera).
    static GameObject MuroX(string nombre, Transform parent, float xMin, float xMax, float z, float baseY, float altura, Material mat)
    {
        return Caja(nombre, parent,
            new Vector3((xMin + xMax) / 2f, baseY + altura / 2f, z),
            new Vector3(xMax - xMin, altura, Espesor), mat);
    }

    // Muro que corre a lo largo de Z (delgado en X).
    static GameObject MuroZ(string nombre, Transform parent, float zMin, float zMax, float x, float altura, Material mat)
    {
        return Caja(nombre, parent,
            new Vector3(x, altura / 2f, (zMin + zMax) / 2f),
            new Vector3(Espesor, altura, zMax - zMin), mat);
    }

    // Edificio genérico (ruina): piso + fondo + dos laterales + frente con hueco de puerta centrado.
    // puertaAlNorte = true: el frente (puerta) mira a +Z; false: mira a -Z.
    static void Edificio(string nombre, Transform parent, float xMin, float xMax, float zMin, float zMax,
                         float altura, bool puertaAlNorte, float anchoPuerta, Material matPiso, Material matPared)
    {
        var t = Carpeta(nombre, parent).transform;
        Piso("Piso", t, xMin, xMax, zMin, zMax, matPiso);
        MuroZ("Muro_O", t, zMin, zMax, xMin, altura, matPared);
        MuroZ("Muro_E", t, zMin, zMax, xMax, altura, matPared);

        float frenteZ = puertaAlNorte ? zMax : zMin;
        float fondoZ = puertaAlNorte ? zMin : zMax;
        MuroX("Muro_Fondo", t, xMin, xMax, fondoZ, 0f, altura, matPared);

        float cx = (xMin + xMax) / 2f;
        MuroX("Muro_Frente_A", t, xMin, cx - anchoPuerta / 2f, frenteZ, 0f, altura, matPared);
        MuroX("Muro_Frente_B", t, cx + anchoPuerta / 2f, xMax, frenteZ, 0f, altura, matPared);
    }

    static void Obstaculo(GameObject go)
    {
        var o = go.AddComponent<NavMeshObstacle>();
        o.shape = NavMeshObstacleShape.Box;
        o.size = Vector3.one;   // el localScale del cubo ya define el volumen real
        o.center = Vector3.zero;
        o.carving = true;
    }

    // ---------- Construcción del nivel ----------

    static void ConstruirCalle(GameObject g, Material matVereda, Material matCalzada, Material matAuto)
    {
        var t = g.transform;
        Piso("VeredaSur", t, CuadraMin, CuadraMax, VeredaSurMin, VeredaSurMax, matVereda);
        Piso("Calzada", t, CuadraMin, CuadraMax, CalzadaMin, CalzadaMax, matCalzada);
        Piso("VeredaNorte", t, CuadraMin, CuadraMax, VeredaNorteMin, VeredaNorteMax, matVereda);

        var auto1 = Caja("Auto_1", t, new Vector3(10f, 0.6f, 6f), new Vector3(2f, 1.2f, 4f), matAuto);
        auto1.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
        Obstaculo(auto1);

        var auto2 = Caja("Auto_2", t, new Vector3(30f, 0.6f, 7f), new Vector3(2f, 1.2f, 4.2f), matAuto);
        auto2.transform.localRotation = Quaternion.Euler(0f, -22f, 0f);
        Obstaculo(auto2);
    }

    static void ConstruirFerreteria(GameObject g, Material matPiso, Material matFerreteria)
    {
        var t = g.transform;
        const float xMin = 15f, xMax = 25f;
        Piso("Piso", t, xMin, xMax, SurRuinaMin, SurRuinaMax, matPiso);

        // Fondo y laterales (rojo, para leerse como el local).
        MuroZ("Muro_O", t, SurRuinaMin, SurRuinaMax, xMin, AlturaMuro, matFerreteria);
        MuroZ("Muro_E", t, SurRuinaMin, SurRuinaMax, xMax, AlturaMuro, matFerreteria);
        MuroX("Muro_Fondo", t, xMin, xMax, SurRuinaMin, 0f, AlturaMuro, matFerreteria);

        // Fachada en Z=0: puerta (hueco a piso) + vidriera (hueco medio, con alfeizar y dintel).
        const float frente = 0f;
        const float puertaCx = 20f, puertaAncho = 1.4f;
        MuroX("Fachada_1", t, xMin, puertaCx - puertaAncho / 2f, frente, 0f, AlturaMuro, matFerreteria); // 15 -> 19.3
        MuroX("Fachada_2", t, puertaCx + puertaAncho / 2f, 21f, frente, 0f, AlturaMuro, matFerreteria);   // 20.7 -> 21
        MuroX("Fachada_Alfeizar", t, 21f, 24f, frente, 0f, 1f, matFerreteria);                            // vidriera: abajo
        MuroX("Fachada_Dintel", t, 21f, 24f, frente, 2.5f, 0.5f, matFerreteria);                          // vidriera: arriba
        MuroX("Fachada_3", t, 24f, xMax, frente, 0f, AlturaMuro, matFerreteria);                          // 24 -> 25
    }

    static void ConstruirRuinas(GameObject g, Material matPiso, Material matRuina)
    {
        var t = g.transform;
        // Fila sur (flanquean la ferretería): puerta al norte, a la vereda sur.
        Edificio("Ruina_SO", t, 2f, 13f, SurRuinaMin, SurRuinaMax, 2.6f, true, 1.4f, matPiso, matRuina);
        Edificio("Ruina_SE", t, 27f, 39f, SurRuinaMin, SurRuinaMax, 3.2f, true, 1.4f, matPiso, matRuina);
        // Fila norte: puerta al sur, a la vereda norte. Callejón entre N2 y N3 (hueco 25..27).
        Edificio("Ruina_NO", t, 2f, 14f, NorteRuinaMin, NorteRuinaMax, 3.0f, false, 1.4f, matPiso, matRuina);
        Edificio("Ruina_N2", t, 16f, 25f, NorteRuinaMin, NorteRuinaMax, 2.4f, false, 1.4f, matPiso, matRuina);
        Edificio("Ruina_N3", t, 27f, 39f, NorteRuinaMin, NorteRuinaMax, 3.4f, false, 1.4f, matPiso, matRuina);
    }

    static void ConstruirLimites(GameObject g, Material matEscombros, Material matRuina)
    {
        var t = g.transform;
        // Escombros oeste: cierran calzada + vereda norte (Z 3..13); dejan la vereda sur (0..3) abierta = entrada de clientes.
        Caja("Escombros_Oeste_a", t, new Vector3(1f, 1.5f, 8f), new Vector3(2f, 3f, 10f), matEscombros);
        Caja("Escombros_Oeste_b", t, new Vector3(2.2f, 1f, 5.5f), new Vector3(1.5f, 2f, 3f), matEscombros);
        Caja("Escombros_Oeste_c", t, new Vector3(1.5f, 2.2f, 11f), new Vector3(2.2f, 1.6f, 3f), matEscombros);

        // Portón este: cierra todo el corredor (Z 0..13). Bloqueado (sólido).
        Caja("Porton_Este", t, new Vector3(41f, 1.6f, 6.5f), new Vector3(2f, 3.2f, 13f), matEscombros);

        // Telón Zona 2: siluetas de fondo detrás del portón (no jugable todavía).
        Caja("Telon_Z2_a", t, new Vector3(44f, 3f, 15f), new Vector3(4f, 6f, 4f), matRuina);
        Caja("Telon_Z2_b", t, new Vector3(46f, 2.5f, 19f), new Vector3(4f, 5f, 4f), matRuina);
        Caja("Telon_Z2_c", t, new Vector3(45f, 2f, 11f), new Vector3(5f, 4f, 3f), matRuina);
    }

    static void ConstruirPuntos(GameObject g)
    {
        var t = g.transform;
        Punto("Punto_Entrada", t, new Vector3(20f, 0.1f, 0.5f), TipoPunto.Entrada);
        Punto("Punto_Calle_1", t, new Vector3(14f, 0.1f, 1.5f), TipoPunto.Calle);
        Punto("Punto_Calle_2", t, new Vector3(30f, 0.1f, 1.5f), TipoPunto.Calle);
    }

    static void Punto(string nombre, Transform parent, Vector3 pos, TipoPunto tipo)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.AddComponent<PuntoInteres>().tipo = tipo;
    }

    static void ConstruirMarcadores(GameObject g)
    {
        var t = g.transform;
        // Loot (uno por ruina): reservado para la feature de exploración.
        Marcador("LootPoint_SO", t, new Vector3(7.5f, 0.2f, -4f));
        Marcador("LootPoint_SE", t, new Vector3(33f, 0.2f, -4f));
        Marcador("LootPoint_NO", t, new Vector3(8f, 0.2f, 17f));
        Marcador("LootPoint_N2", t, new Vector3(20.5f, 0.2f, 17f));
        Marcador("LootPoint_N3", t, new Vector3(33f, 0.2f, 17f));
        // Saqueadores y desbloqueo: reservados para sus features.
        Marcador("SpawnSaqueador_Este", t, new Vector3(39f, 0.5f, 6f));
        Marcador("SpawnSaqueador_Callejon", t, new Vector3(26f, 0.5f, 12f));
        Marcador("DesbloqueoPorton", t, new Vector3(40f, 0.5f, 6f));
    }

    static void Marcador(string nombre, Transform parent, Vector3 pos)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
    }

    static void ReubicarGenerador()
    {
        var gen = Object.FindFirstObjectByType<GeneradorClientes>();
        if (gen != null)
        {
            Undo.RecordObject(gen.transform, "Reubicar GeneradorClientes");
            // X=5 (no X=1): el NavMesh deja un margen de ~0.5 m contra el borde del mundo y
            // los escombros, así que un spawn pegado a la esquina oeste cae fuera de la malla.
            gen.transform.position = new Vector3(5f, 0.1f, 1.5f);
        }
        else
        {
            Debug.LogWarning("No encontré un GeneradorClientes en la escena. Cuando lo tengas, ubicalo en (5, 0.1, 1.5) para que los clientes entren por el oeste, sobre el NavMesh.");
        }
    }
}
