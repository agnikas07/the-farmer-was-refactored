using System;
using Jint;
using Jint.Native;

namespace TheFarmerWasRefactored.Runtime
{
    public static class MovementApi
    {
        public static void Bind(Engine engine, ActionMailbox mailbox, Action onRequest)
        {
            engine.SetValue("move", new Func<JsValue, bool>(direction =>
            {
                if (!direction.IsString())
                    throw new ArgumentException("move requires a direction string.");
                onRequest?.Invoke();
                return mailbox.Move(direction.AsString());
            }));
        }
    }
}
