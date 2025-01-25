namespace Labb3_MongoDBProg_ITHS.NET.Backend;
internal class InputHandler
{
    public static InputHandler Instance { get; private set; } = new();
    private InputHandler() { }

    internal Task<ConsoleKeyInfo> InputListener = default!;
    private Dictionary<ConsoleKey, IInputEndpoint> _listeners = new();
    private Dictionary<ConsoleKeyInfo, IInputEndpoint> _commandListeners = new();
    private bool _readAllKeys = false;
    private ConsoleKeyInfo? _nextKey;

    private bool running = false;

    internal void Clear()
    {
		_listeners.Clear();
	}
    internal void Start()
    {
        if(running) return;
		running = true;
		InputListener = Task.Run(Listener);
	}
	private ConsoleKeyInfo Listener()
    {
        ConsoleKeyInfo k = default;
        while (running)
        {
            // This makes sure the input goes to any "Console.Read*" calls first and does not take the next key after Stop is called.
			if(Console.KeyAvailable)
				k = Console.ReadKey(true);
            else
            {
                Thread.Sleep(50);
                continue;
            }
            if (!running) break;

            if(_readAllKeys)
                _nextKey = k;
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
		//running = false;
		_nextKey = null;
		_readAllKeys = true;

        while(_nextKey == null)
            Thread.Sleep(50);

        var key = _nextKey!.Value;

		//Start();
		_readAllKeys = false;
        _nextKey = null;
		return key;
    }
}
