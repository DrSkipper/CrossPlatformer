using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Assets.Scripts
{
    class TFPlayer : MonoBehaviour
    {
        public void Update()
        {
            // - Update spam shot counter
            
            // - Regenerate arrows if need be

            // - Regenerate shields if need be

            // - Update opacity based on visibility and character state

            // - Handle sprite scaling for non-dodging states
            
            // - Handle opacity for ducking

            // - Send collision ray(s) from our position in UnitY direction (down) to detect OnGround
            // - If so, store hit entity as lastPlatform. Check if this platform is slippery or hot coals and store bools for those as well.

            // - Get input state for this player

            // - Get aimDirection (circular) from joystick axis

            // - If we're frozen, just set Facing to appropriate direction and exit Update function

            // - Update jumpBufferCounter, and if input indicates Jump is pressed, set it to JUMP_BUFFER (6)

            // - If we're aiming, play aiming sound (?) and update lastAimDirection to AimDirection

            // - Check if we're set to auto-move, and if so, set our input axis x value to our autoMove value

            // - If we're on ground, do some stuff:
            /*
                this.jumpGraceCounter.SetMax(JUMP_GRACE); // JUMP_GRACE = 6
                this.wallStickMax = WALLSTICK_START;
                this.flapGravity = 1f;
                this.graceLedgeDir = 0;
             */

            // - Otherwise, update our jump grace counter (I believe this is for stored jumps)

            // - If there is a wing flap counter, update it or set it to zero if we have >= 0 y movement

            // - Set gliding to false

            //base.Update(); - this calls updates on the player components, including PlayerState, which results in one of those corresponding methods being called

            // - Check if we should pick up an arrow

            // - Check hat collisions

            // - If on fire and have been on fire for long enough to lose wings, lose them

            // - Update Animations
            
            // - Update hair/other animated accessory 
        }
    }
}
