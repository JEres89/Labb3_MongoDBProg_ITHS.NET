namespace Labb3_MongoDBProg_ITHS.NET.Backend;
internal interface IInputEndpoint
{
    internal void KeyPressed(ConsoleKey key);
    internal void CommandPressed(ConsoleKeyInfo command);

    internal void RegisterKeys(InputHandler handler);
}
