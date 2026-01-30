using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class InputBuffer : MonoBehaviour
{
    public InputAction MoveAction => _moveAction;
    public InputAction LightAttackAction => _lightAttackAction;
    public InputAction StrongAttackAction => _strongAttackAction;
    public InputAction GhostAction => _ghostAction;
    public InputAction BuffAction => _buffAction;
    public InputAction LookOnAction => _lookOnAction;
    public InputAction HealAction => _healAction;
    public InputAction SprintAction => _sprintAction;

    private const string MOVE_ACTION = "Move";
    private const string LIGHTATTACK_ACTION = "LightAttack";
    private const string STRONGATTACK_ACTION = "StrongAttack";
    private const string GHOST_ACTION = "Evasion";
    private const string BUFF_ACTION = "Buff";
    private const string LOOKON_ACTION = "LookOn";
    private const string HEAL_ACTION = "Heal";
    private const string SPRINT_ACTION = "Sprint";

    private InputAction _moveAction;
    private InputAction _lightAttackAction;
    private InputAction _strongAttackAction;
    private InputAction _ghostAction;
    private InputAction _buffAction;
    private InputAction _lookOnAction;
    private InputAction _healAction;
    private InputAction _sprintAction;

    private void Awake()
    {
        if(TryGetComponent<PlayerInput>(out var playerInput))
        {
            _moveAction = playerInput.actions[MOVE_ACTION];
            _lightAttackAction = playerInput.actions[LIGHTATTACK_ACTION];
            _strongAttackAction = playerInput.actions[STRONGATTACK_ACTION];
            _ghostAction = playerInput.actions[GHOST_ACTION];
            _buffAction = playerInput.actions[BUFF_ACTION];
            _lookOnAction = playerInput.actions[LOOKON_ACTION];
            _healAction = playerInput.actions[HEAL_ACTION];
            _sprintAction = playerInput.actions[SPRINT_ACTION];
        }
    }
}
