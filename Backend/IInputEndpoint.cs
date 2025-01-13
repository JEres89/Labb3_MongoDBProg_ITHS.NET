using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Labb2_CsProg_ITHS.NET.Backend;
internal interface IInputEndpoint
{
    internal void KeyPressed(ConsoleKeyInfo key);

    internal void RegisterKeys(InputHandler handler);
}
