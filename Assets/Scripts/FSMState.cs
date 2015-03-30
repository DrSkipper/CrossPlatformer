using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    class FSMState
    {
        public string Identifier { get; private set; }
        public StateEnteredCallback EnteredCallback { get; private set; }
        public StateExitedCallback ExitedCallback { get; private set; }
        public StateUpdateHandler UpdateHandler { get; private set; }

        public FSMState(string identifier, StateEnteredCallback enteredCallback = null, StateExitedCallback exitedCallback = null, StateUpdateHandler updateHandler = null)
        {
            this.Identifier = identifier;
            this.EnteredCallback = enteredCallback;
            this.ExitedCallback = exitedCallback;
            this.UpdateHandler = updateHandler;
        }

        public void Enter()
        {
            if (this.EnteredCallback != null)
                this.EnteredCallback();
        }

        public void Exit()
        {
            if (this.ExitedCallback != null)
                this.ExitedCallback();
        }

        public string Update()
        {
            if (this.UpdateHandler != null)
                return this.UpdateHandler();
            return this.Identifier;
        }
    }
}
