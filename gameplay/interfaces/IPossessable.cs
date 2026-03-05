public interface IPossessable
{
    void SetInputEnabled(bool enabled);

    void OnPossessed(Controller controller);
    void OnUnpossessed();
}