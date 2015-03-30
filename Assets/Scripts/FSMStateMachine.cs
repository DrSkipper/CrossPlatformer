using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    public delegate void StateEnteredCallback();
    public delegate void StateExitedCallback();
    public delegate string StateUpdateHandler();

    class FSMStateMachine
    {
        public string PreviousState
        {
            get
            {
                return _previousState.Identifier;
            }
        }

        public string CurrentState
        {
            get
            {
                return _currentState.Identifier;
            }
            set
            {
                if (_currentState.Identifier != value)
                {
                    _currentState.Exit();
                    _previousState = _currentState;
                    _currentState = _states[value];
                    _currentState.Enter();
                }
            }
        }

        public FSMStateMachine()
        {
            _states = new Dictionary<string, FSMState>();
        }

        public void AddState(string state, StateEnteredCallback enteredCallback = null, StateExitedCallback exitedCallback = null, StateUpdateHandler updateHandler = null)
        {
            _states[state] = new FSMState(state, enteredCallback, exitedCallback, updateHandler);
        }

        public void BeginWithInitialState(string initialState)
        {
            _currentState = _states[initialState];
        }

		public void Update()
		{
            this.CurrentState = _currentState.Update();
		}

        /**
         * Private
         */
        private FSMState _currentState;
        private FSMState _previousState;
        private Dictionary<string, FSMState> _states;
    }
}
