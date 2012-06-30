#region File Description
//-----------------------------------------------------------------------------
// SmokePlumeParticleSystem.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace FlightSim_AMS
{
    /// <summary>
    /// Custom particle system for creating a giant plume of long lasting smoke.
    /// </summary>
    class SmokePlumeParticleSystem : ParticleSystem
    {
        public SmokePlumeParticleSystem(Game game, ContentManager content)
            : base(game, content)
        { }


        protected override void InitializeSettings(ParticleSettings settings)
        {
            settings.TextureName = "Textures/smoke";

            settings.MaxParticles = 600;

            settings.Duration = TimeSpan.FromSeconds(10);

            settings.MinHorizontalVelocity = 0;
            settings.MaxHorizontalVelocity = 0.15f;

            settings.MinVerticalVelocity = 0.1f;
            settings.MaxVerticalVelocity = 0.2f;

            // Create a wind effect by tilting the gravity vector sideways.
            settings.Gravity = new Vector3(-0.2f, -0.05f, 0);

            settings.EndVelocity = 0.75f;

            settings.MinRotateSpeed = -1;
            settings.MaxRotateSpeed = 1;

            settings.MinStartSize = 0.05f;
            settings.MaxStartSize = 0.01f;

            settings.MinEndSize = 0.5f;
            settings.MaxEndSize = 1;
        }
    }
}
