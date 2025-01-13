using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Labb3_MongoDBProg_ITHS.NET.Backend;
internal class InputHandler
{
    public static InputHandler Instance { get; private set; } = new();
    private InputHandler() { }

    internal Task<ConsoleKeyInfo> InputListener = default!;
    private Dictionary<ConsoleKey, IInputEndpoint> _listeners = new();
    public bool ReadAllKeys { get; set; } = false;

    private bool running = false;

    internal void Clear()
    {
		_listeners.Clear();
	}
    internal ConsoleKeyInfo Start()
    {
        ConsoleKeyInfo k = default;
        running = true;
        while (running)
        {
            k = Console.ReadKey(true);
            if (!running) break;

            if (_listeners.TryGetValue(k.Key, out var listener))
            {
                listener.KeyPressed(k);
            }
        }
        return k;
    }

    internal void Stop()
    {
        running = false;
    }
    internal bool AddKeyListener(ConsoleKey key, IInputEndpoint listener)
    {
        return _listeners.TryAdd(key, listener);
    }
    internal ConsoleKeyInfo AwaitNextKey()
    {
        running = false;
        var key = InputListener.Result;

        InputListener = Task.Run(Start);

        return key;
    }
}
