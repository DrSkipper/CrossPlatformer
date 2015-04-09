using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    struct InputState
    {
        public const float MOVE_X_DEADZONE = 0.5f;
        public const float MOVE_Y_DEADZONE = 0.8f;
        public const float AIM_DEADZONE_SQ = 0.09f;
        public const float TRIGGER_THRESHOLD = 0.1f;

        /**
         * OLD:
         * [button]Check = Button is currently down.
         * [button]Pressed = Button went from Depressed to Pressed state this update.
         * 
         * NEW:
         * [button] = Button is currently down.
         * [button]Started = Button went from Depressed to Pressed state this update.
         */
        public bool Jump;
        public bool JumpStarted;
        public bool Shoot;
        public bool ShootStarted;
        public bool Dodge;
        public bool DodgeStarted;
        public bool AmmoSwitch;
        public bool AmmoSwitchStarted;
        public int MoveX;
        public int MoveY;
        public Vector2 AimAxis;

        public static InputState GetInputStateForPlayer(int playerIndex)
        {
            //TODO - Use playerIndex
            InputState state = new InputState();
            state.Jump = Input.GetButton("Jump");
            state.JumpStarted = Input.GetButtonDown("Jump");
            state.Shoot = Input.GetButton("Fire 1");
            state.ShootStarted = Input.GetButtonDown("Fire 1");
            state.Dodge = Input.GetButton("Fire 2");
            state.DodgeStarted = Input.GetButtonDown("Fire 2");
            state.AmmoSwitch = Input.GetButton("Fire 3");
            state.AmmoSwitchStarted = Input.GetButtonDown("Fire 3");
            state.MoveX = Math.Sign(Input.GetAxis("Horizontal"));
            state.MoveY = Math.Sign(Input.GetAxis("Vertical"));
            state.AimAxis = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            return state;
        }
    }
}
