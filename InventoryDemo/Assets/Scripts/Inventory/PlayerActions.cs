using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerActions : MonoBehaviour
{
    // Input Manager
    private InputManager _inputs;
    // ActionBar
    [SerializeField] private ActionBar _actionBar;

    public System.Action<GameObject> TriggerAction;
    public bool IsTriggeringAction { get; private set; }
    public bool IsExecutingAction { get; set; }

    void Start()
    {
        _inputs = FindObjectOfType<InputManager>();
    }
    // Update is called once per frame
    void Update()
    {
        // Agregado: el repo original no chequeaba esto, y cualquier clic sobre
        // la UI del inventario disparaba tambien la accion del arma equipada.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (!IsTriggeringAction && _inputs.Fire)
        {
            IsTriggeringAction = true;
            TriggerAction.Invoke(gameObject);
        }
        else if(IsTriggeringAction && !_inputs.Fire)
        {
            IsTriggeringAction = false;
        }
    }
}
