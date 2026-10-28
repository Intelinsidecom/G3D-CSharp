using System;
using G3D;

namespace G3D.Tests
{
    internal static partial class Program
    {
        private sealed class TGThread : GThread
        {
            public TGThread(string n)
                : base(n)
            {
                _value = 0;
            }

            public int value()
            {
                // Shouldn't need lock
                return _value;
            }

            public void incValue()
            {
                getterMutex.@lock();
                ++_value;
                getterMutex.unlock();
            }

            protected override void threadMain()
            {
                ++_value;
                return;
            }

            private int _value;
            private readonly GMutex getterMutex = new GMutex();
        }

        private static void testGThread()
        {
            Console.Write("G3D::GThread ");

            {
                TGThread tGThread = new TGThread("tGThread");
                Debug.debugAssert(tGThread.value() == 0);

                bool started = tGThread.start();
                Debug.debugAssert(started);

                tGThread.waitForCompletion();
                Debug.debugAssert(tGThread.completed());

                Debug.debugAssert(tGThread.value() == 1);

                tGThread.incValue();
                Debug.debugAssert(tGThread.value() == 2);
            }

            Console.Write("passed\n");
        }
    }
}
