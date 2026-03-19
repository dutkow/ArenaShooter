using Godot;

public abstract class Singleton<T> where T : class, new()
{
    public static T Instance { get; private set; }

    public static void Initialize()
    {
        if (Instance != null)
        {
            GD.Print($"{typeof(T).Name} already exists");
            return;
        }

        Instance = new T();
        (Instance as Singleton<T>)?.OnInitialize();
    }

    public static void Shutdown()
    {
        (Instance as Singleton<T>)?.OnShutdown();
        Instance = null;
    }

    // Override in subclasses for custom setup/cleanup
    protected virtual void OnInitialize() { }
    protected virtual void OnShutdown() { }
}