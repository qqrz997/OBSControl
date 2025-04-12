﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OBSControl.Wrappers
{
    public class GameplayModifiersWrapper : IGameplayModifiers
    {
        private GameplayModifiers _modifiers;
        public GameplayModifiersWrapper(GameplayModifiers modifiers)
        {
            _modifiers = modifiers;
        }
        public EnergyType EnergyType => _modifiers.energyType.ToEnergyType();
        public bool BatteryEnergy => _modifiers.energyType == GameplayModifiers.EnergyType.Battery;
        public bool NoFail => _modifiers.noFailOn0Energy;
        
        // TODO: look for a suitable replacement or remove
        public bool DemoNoFail => false;
        public bool InstaFail => _modifiers.instaFail;
        public bool FailOnSaberClash => _modifiers.failOnSaberClash;
        public EnabledObstacleType EnabledObstacleType => _modifiers.enabledObstacleType.ToEnabledObstacleType();
        public bool NoObstacles => _modifiers.enabledObstacleType == GameplayModifiers.EnabledObstacleType.NoObstacles;
        
        // TODO: look for a suitable replacement or remove
        public bool DemoNoObstacles => false;
        public bool FastNotes => _modifiers.fastNotes;
        public bool StrictAngles => _modifiers.strictAngles;
        public bool DisappearingArrows => _modifiers.disappearingArrows;
        public bool GhostNotes => _modifiers.ghostNotes;
        public bool NoBombs => _modifiers.noBombs;
        public SongSpeed SongSpeed => _modifiers.songSpeed.ToSongSpeed();
        public float SongSpeedMul => _modifiers.songSpeedMul;
        public bool NoArrows => _modifiers.noArrows;

        public override string ToString()
        {
            return this.ToModifierString();
        }
    }
}
