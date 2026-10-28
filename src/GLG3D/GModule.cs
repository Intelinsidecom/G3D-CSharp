/**
 @file GModule.h

 @maintainer Morgan McGuire, morgan3d@users.sourceforge.net


 @created 2006-04-22
 @edited 2006-04-22
*/

using System;
using G3D;

namespace G3D
{
    /**
     Interface for 2D or 3D objects that experience standard
     virtual world events and are rendered.

     GModule is an interface for "widget"-like objects.  You could think of it as a bare-bones scene graph.

     Modules are objects like the FPController, GConsole, and debug text overlay that need to receive almost the same set of events (onXXX methods) as a GApplet and that you would like to be called from the corresponding methods of a GApplet.  They are a way to break large pieces of functionality for UI and debugging off so that they can be mixed and matched.


     @beta
    */
    public abstract class GModule : ReferenceCountedObject
    {
        /** 
         Appends a posed model for this object to the array, if it has a graphic representation.
         The posed model appended is allowed to reference the agent and is allowed to mutate
         if the agent is mutated. 
         */
        public abstract void getPosedModel(
            Array<ReferenceCountedPointer<PosedModel>> posedArray,
            Array<ReferenceCountedPointer<PosedModel2D>> posed2DArray);

        public abstract void onSimulation(RealTime rdt, SimTime sdt, SimTime idt);

        /** Returning true consumes the event and prevents other GModules from seeing it. */
        public abstract bool onEvent(GEvent @event);

        public abstract void onUserInput(UserInput ui);

        public abstract void onNetwork();

        public abstract void onLogic();
    }

    /**
     Exports a coordinate frame, typically in response to user input.
     Examples:
     G3D::ThirdPersonManipulator,
     G3D::FirstPersonManipulator
    */
    public abstract class Manipulator : GModule
    {
        public abstract void getFrame(out CoordinateFrame c);

        public abstract CoordinateFrame frame();
    }

    /**
     Manages a group of GModules.  This is used internally by GApp and GApplet
     to process their modules.  It also enables use of GModules without
     the GApp infrastructure.

     You can use GModules without this class.
    */
    public class GModuleManager : GModule
    {
        public enum EventPriority {LOW_PRIORITY, NORMAL_PRIORITY, HIGH_PRIORITY, NUM_PRIORITY};

        /** Module index is the priority */
        private Array<ReferenceCountedPointer<GModule>>[] m_moduleArray = new Array<ReferenceCountedPointer<GModule>>[(int)EventPriority.NUM_PRIORITY];

        private class Add
        {
            public ReferenceCountedPointer<GModule> module;
            public EventPriority priority;

            public Add()
            {
            }

            public Add(ReferenceCountedPointer<GModule> m, EventPriority p)
            {
                module = m;
                priority = p;
            }
        }

        private Array<Add> m_addList = new Array<Add>();

        private Array<ReferenceCountedPointer<GModule>> m_removeList = new Array<ReferenceCountedPointer<GModule>>();

        /** Number of elements. */
        private int m_size;

        private bool m_locked;

        /** If true, when the lock is lifted all objects should be removed. */
        private bool m_removeAll;

        public GModuleManager()
        {
            for (int i = 0; i < m_moduleArray.Length; ++i)
            {
                m_moduleArray[i] = new Array<ReferenceCountedPointer<GModule>>();
            }
            m_locked = false;
            m_size = 0;
            m_removeAll = false;
        }

        public static ReferenceCountedPointer<GModuleManager> create()
        {
            return new ReferenceCountedPointer<GModuleManager>(new GModuleManager());
        }

        /** 
          Between beginLock and endLock, add and remove operations are delayed so that 
          iteration is safe.  Locks may not be executed recursively; only one level of
          locking is allowed.
          */
        public void beginLock()
        {
            if (m_locked)
            {
                Debug.debugAssert(!m_locked);
                throw new InvalidOperationException("GModuleManager locks may not be executed recursively.");
            }
            m_locked = true;
        }

        public void endLock()
        {
            if (!m_locked)
            {
                Debug.debugAssert(m_locked);
                throw new InvalidOperationException("GModuleManager is not locked.");
            }
            m_locked = false;

            // Process add list
            for (int i = 0; i < m_addList.size(); ++i)
            {
                add(m_addList[i].module, m_addList[i].priority);
            }
            m_addList.clear();

            // Process remove list
            for (int i = 0; i < m_removeList.size(); ++i)
            {
                remove(m_removeList[i]);
            }
            m_removeList.clear();

            if (m_removeAll)
            {
                clear();
                m_removeAll = false;
            }
        }

        /** 
            If a lock is in effect, the add may be delayed until the unlock.

            Priorities should generally not be used; they are largely for supporting
            debugging components at HIGH_PRIORITY that intercept events before they
            can hit the regular infrastructure.
          */
        public void add(ReferenceCountedPointer<GModule> m, EventPriority p)
        {
            if ((int)p < (int)EventPriority.LOW_PRIORITY || (int)p > (int)EventPriority.HIGH_PRIORITY)
            {
                Debug.debugAssert((int)p >= (int)EventPriority.LOW_PRIORITY && (int)p <= (int)EventPriority.HIGH_PRIORITY);
                throw new ArgumentOutOfRangeException("p");
            }
            if (m_locked)
            {
                m_addList.append(new Add(m, p));
            }
            else
            {
                ++m_size;
                m_moduleArray[(int)p].append(m);
            }
        }

        public void add(ReferenceCountedPointer<GModule> m)
        {
            add(m, EventPriority.NORMAL_PRIORITY);
        }

        /**
           If a lock is in effect the remove will be delayed until the unlock.
         */
        public void remove(ReferenceCountedPointer<GModule> m)
        {
            if (m_locked)
            {
                m_removeList.append(m);
            }
            else
            {
                for (int p = 0; p < (int)EventPriority.NUM_PRIORITY; ++p)
                {
                    int j = m_moduleArray[p].findIndex(m);
                    if (j != -1)
                    {
                        m_moduleArray[p].fastRemove(j);
                        --m_size;
                        return;
                    }
                }
                Debug.debugAssertM(false, "Removed a GModule that was not in the manager.");
            }
        }

        /**
         Removes all.
         */
        public void clear()
        {
            if (m_locked)
            {
                m_removeAll = true;
            }
            else
            {
                for (int p = 0; p < (int)EventPriority.NUM_PRIORITY; ++p)
                {
                    m_moduleArray[p].clear();
                }
            }
        }

        public int size()
        {
            return m_size;
        }

        public ReferenceCountedPointer<GModule> this[int i]
        {
            get
            {
                if (i < 0 || i >= m_size)
                {
                    Debug.debugAssert(i >= 0 && i < m_size);
                    throw new ArgumentOutOfRangeException("i");
                }
                for (int p = (int)EventPriority.NUM_PRIORITY - 1; p >= 0; --p)
                {
                    if (i < m_moduleArray[p].size())
                    {
                        return m_moduleArray[p][i];
                    }
                    else
                    {
                        // Look in the next array.  We are guaranteed
                        // not to underflow by the assertion above.
                        i -= m_moduleArray[p].size();
                    }
                }

                return null;
            }
        }

        public ReferenceCountedPointer<GModule> getModule(int i)
        {
            return this[i];
        }

        // Iterate through all modules in priority order.
        // This same iteration code is used to implement
        // all GModule methods concisely.

        /** Calls getPosedModel on all children.*/
        public override void getPosedModel(
            Array<ReferenceCountedPointer<PosedModel>> posedArray,
            Array<ReferenceCountedPointer<PosedModel2D>> posed2DArray)
        {
            beginLock();
            try
            {
                for (int p = (int)EventPriority.NUM_PRIORITY - 1; p >= 0; --p)
                {
                    Array<ReferenceCountedPointer<GModule>> array = m_moduleArray[p];
                    for (int i = array.size() - 1; i >= 0; --i)
                    {
                        array[i].pointer().getPosedModel(posedArray, posed2DArray);
                    }
                }
            }
            finally
            {
                endLock();
            }
        }

        public override void onSimulation(RealTime rdt, SimTime sdt, SimTime idt)
        {
            beginLock();
            try
            {
                for (int p = (int)EventPriority.NUM_PRIORITY - 1; p >= 0; --p)
                {
                    Array<ReferenceCountedPointer<GModule>> array = m_moduleArray[p];
                    for (int i = array.size() - 1; i >= 0; --i)
                    {
                        array[i].pointer().onSimulation(rdt, sdt, idt);
                    }
                }
            }
            finally
            {
                endLock();
            }
        }

        public override bool onEvent(GEvent @event)
        {
            // if the event is ever consumed, abort iteration
            beginLock();
            try
            {
                for (int p = (int)EventPriority.NUM_PRIORITY - 1; p >= 0; --p)
                {
                    Array<ReferenceCountedPointer<GModule>> array = m_moduleArray[p];
                    for (int i = array.size() - 1; i >= 0; --i)
                    {
                        if (array[i].pointer().onEvent(@event))
                        {
                            return true;
                        }
                    }
                }
            }
            finally
            {
                endLock();
            }
            return false;
        }

        public override void onUserInput(UserInput ui)
        {
            beginLock();
            try
            {
                for (int p = (int)EventPriority.NUM_PRIORITY - 1; p >= 0; --p)
                {
                    Array<ReferenceCountedPointer<GModule>> array = m_moduleArray[p];
                    for (int i = array.size() - 1; i >= 0; --i)
                    {
                        array[i].pointer().onUserInput(ui);
                    }
                }
            }
            finally
            {
                endLock();
            }
        }

        public override void onNetwork()
        {
            beginLock();
            try
            {
                for (int p = (int)EventPriority.NUM_PRIORITY - 1; p >= 0; --p)
                {
                    Array<ReferenceCountedPointer<GModule>> array = m_moduleArray[p];
                    for (int i = array.size() - 1; i >= 0; --i)
                    {
                        array[i].pointer().onNetwork();
                    }
                }
            }
            finally
            {
                endLock();
            }
        }

        public override void onLogic()
        {
            beginLock();
            try
            {
                for (int p = (int)EventPriority.NUM_PRIORITY - 1; p >= 0; --p)
                {
                    Array<ReferenceCountedPointer<GModule>> array = m_moduleArray[p];
                    for (int i = array.size() - 1; i >= 0; --i)
                    {
                        array[i].pointer().onLogic();
                    }
                }
            }
            finally
            {
                endLock();
            }
        }

        /** Runs the event handles of each manager interlaced, as if all the modules from a were in b.*/
        public static bool onEvent(GEvent @event, ReferenceCountedPointer<GModuleManager> a, ReferenceCountedPointer<GModuleManager> b)
        {
            bool aLocked = false;
            bool bLocked = false;

            try
            {
                a.pointer().beginLock();
                aLocked = true;

                b.pointer().beginLock();
                bLocked = true;

                for (int p = (int)EventPriority.NUM_PRIORITY - 1; p >= 0; --p)
                {
                    // Process each, interlaced
                    for (int k = 0; k < 2; ++k)
                    {
                        Array<ReferenceCountedPointer<GModule>> array =
                            (k == 0) ?
                                a.pointer().m_moduleArray[p] :
                                b.pointer().m_moduleArray[p];

                        for (int i = array.size() - 1; i >= 0; --i)
                        {
                            if (array[i].pointer().onEvent(@event))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            finally
            {
                try
                {
                    if (bLocked)
                    {
                        b.pointer().endLock();
                    }
                }
                finally
                {
                    if (aLocked)
                    {
                        a.pointer().endLock();
                    }
                }
            }

            return false;
        }
    }
}
