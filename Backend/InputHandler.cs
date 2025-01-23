namespace Labb3_MongoDBProg_ITHS.NET.Backend;
internal class InputHandler
{
    public static InputHandler Instance { get; private set; } = new();
    private InputHandler() { }

    internal Task<ConsoleKeyInfo> InputListener = default!;
    private Dictionary<ConsoleKey, IInputEndpoint> _listeners = new();
    private Dictionary<ConsoleKeyInfo, IInputEndpoint> _commandListeners = new();
    public bool ReadAllKeys { get; set; } = false;

    private bool running = false;

    internal void Clear()
    {
		_listeners.Clear();
	}
    internal void Start()
    {
        running = true;
		InputListener = Task.Run(Listener);
	}
	private ConsoleKeyInfo Listener()
    {
        ConsoleKeyInfo k = default;
        while (running)
        {
            k = Console.ReadKey(true);
            if (!running) break;

			// Commands take precedence over regular keys
			if(_commandListeners.TryGetValue(k, out var listener))
			{
				listener.CommandPressed(k);
			}
            else if (_listeners.TryGetValue(k.Key, out listener))
            {
                listener.KeyPressed(k.Key);
            }
		}
        return k;
    }

    internal void Stop()
    {
        running = false;
	}
    internal void AddKeyListener(ConsoleKey key, IInputEndpoint listener)
    {
        _listeners[key] = listener;
    }
    internal void AddCommandListener(ConsoleKeyInfo command, IInputEndpoint listener)
    {
        if(command.Modifiers == 0) 
            return;

		_commandListeners[command] = listener;
    }
    internal ConsoleKeyInfo AwaitNextKey()
    {
        running = false;
        var key = InputListener.Result;

        Start();

        return key;
    }
}
