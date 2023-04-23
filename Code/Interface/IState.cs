
public interface IState
{
    CharacterState GetState();
    void StateEnter();
    void StateUpdate();
    void StateInterrupt();
    void StateExit();
}
