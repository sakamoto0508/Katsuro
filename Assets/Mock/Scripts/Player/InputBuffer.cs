using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class InputBuffer : MonoBehaviour
{
    public InputAction MoveAction => _moveAction;
    public InputAction LightAttackAction => _lightAttackAction;
    public InputAction StrongAttackAction => _strongAttackAction;
    public InputAction EvasionAction => _evasionAction;
    public InputAction BuffeAction => _buffeAction;
    public InputAction LookOnAction => _lookOnAction;
    public InputAction HealAction => _healAction;

    private const string MOVE_ACTION = "Move";
    private const string LIGHTATTACK_ACTION = "LightAttack";
    private const string STRONGATTACK_ACTION = "StrongAttack";
    private const string EVASION_ACTION = "Evasion";
    private const string BUFFE_ACTION = "Buffe";
    private const string LOOKON_ACTION = "LookOn";
    private const string HEAL_ACTION = "Heal";

    private InputAction _moveAction;
    private InputAction _lightAttackAction;
    private InputAction _strongAttackAction;
    private InputAction _evasionAction;
    private InputAction _buffeAction;
    private InputAction _lookOnAction;
    private InputAction _healAction;
    
    public void Init()
    {
        if(TryGetComponent<PlayerInput>(out var playerInput))
        {
            _moveAction = playerInput.actions[MOVE_ACTION];
            _lightAttackAction = playerInput.actions[LIGHTATTACK_ACTION];
            _strongAttackAction = playerInput.actions[STRONGATTACK_ACTION];
            _evasionAction = playerInput.actions[EVASION_ACTION];
            _buffeAction = playerInput.actions[BUFFE_ACTION];
            _lookOnAction = playerInput.actions[LOOKON_ACTION];
            _healAction = playerInput.actions[HEAL_ACTION];
        }
    }
}
