using System.Collections.Generic;
using UnityEngine;

// Directorio central de puntos de interés del mundo (patrón singleton .Instancia).
// Los puntos se registran/desregistran solos en OnEnable/OnDisable, así el orden de
// arranque no importa: Instancia resuelve lazy (crea el objeto si nadie lo puso en escena).
public class RegistroPuntosInteres : MonoBehaviour
{
    private static RegistroPuntosInteres _instancia;

    // true si ya existe una instancia, SIN crearla (para usar en OnDisable durante teardown).
    public static bool ExisteInstancia => _instancia != null;

    public static RegistroPuntosInteres Instancia
    {
        get
        {
            if (_instancia == null)
            {
                var go = new GameObject("RegistroPuntosInteres (auto)");
                _instancia = go.AddComponent<RegistroPuntosInteres>();
            }
            return _instancia;
        }
    }

    private readonly Registro<Estanteria> _estanterias = new Registro<Estanteria>();
    private readonly Registro<PuntoInteres> _entradas = new Registro<PuntoInteres>();
    private readonly Registro<PuntoInteres> _cajas = new Registro<PuntoInteres>();
    private readonly Registro<PuntoInteres> _calles = new Registro<PuntoInteres>();

    void Awake()
    {
        if (_instancia != null && _instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        _instancia = this;
    }

    void OnDestroy()
    {
        if (_instancia == this) _instancia = null;
    }

    // --- Registro (lo llaman los puntos de interés) ---

    public void RegistrarEstanteria(Estanteria e) => _estanterias.Registrar(e);
    public void DesregistrarEstanteria(Estanteria e) => _estanterias.Desregistrar(e);

    public void RegistrarPunto(PuntoInteres p)
    {
        switch (p.tipo)
        {
            case TipoPunto.Entrada: _entradas.Registrar(p); break;
            case TipoPunto.Caja:    _cajas.Registrar(p);    break;
            case TipoPunto.Calle:   _calles.Registrar(p);   break;
        }
    }

    public void DesregistrarPunto(PuntoInteres p)
    {
        _entradas.Desregistrar(p);
        _cajas.Desregistrar(p);
        _calles.Desregistrar(p);
    }

    // --- Consultas (las llaman NPCs y sistemas). Todas toleran registro vacío. ---

    public Estanteria EstanteriaAlAzar()
    {
        if (_estanterias.Cuenta == 0) return null;
        return _estanterias.Todos[Random.Range(0, _estanterias.Cuenta)];
    }

    public Transform EntradaAlAzar() => PuntoAlAzar(_entradas);
    public Transform CalleAlAzar()   => PuntoAlAzar(_calles);

    private Transform PuntoAlAzar(Registro<PuntoInteres> reg)
    {
        if (reg.Cuenta == 0) return null;
        return reg.Todos[Random.Range(0, reg.Cuenta)].transform;
    }

    public Transform PuntoCajaMasCercano(Vector3 desde)
    {
        if (_cajas.Cuenta == 0) return null;

        var posiciones = new List<Vector3>(_cajas.Cuenta);
        for (int i = 0; i < _cajas.Cuenta; i++)
            posiciones.Add(_cajas.Todos[i].transform.position);

        int idx = SeleccionLogica.IndiceMasCercano(posiciones, desde);
        return idx >= 0 ? _cajas.Todos[idx].transform : null;
    }
}
