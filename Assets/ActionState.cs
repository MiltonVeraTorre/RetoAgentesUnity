public class ActionState
{
    public string action_type { get; set; }
    public State old_state { get; set; }

    public ActionState(State old_state)
    {

        action_type = "get_best_action";
        this.old_state = old_state;
    }
}
