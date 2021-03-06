﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Assets.Scripts.Extensions;

namespace Assets.Scripts
{
    class CPPlayerAbility : VoBehavior
    {
        public int Priority = 50;

        public CPPlayer Player { get { if (!_player) _player = this.GetComponent<CPPlayer>(); return _player; } }

        public virtual void ResetProperties()
        {
        }

        public virtual void ApplyPropertyModifiers()
        {
        }

        public virtual string UpdateAbility()
        {
            return null;
        }

        public virtual string PostUpdateAbility()
        {
            return null;
        }

        /**
         * Private
         */
        private CPPlayer _player;
    }
}
