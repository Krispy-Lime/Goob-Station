// SPDX-FileCopyrightText: 2023 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 brainfood1183 <113240905+brainfood1183@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Clothing.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

[RegisterComponent]
[NetworkedComponent]
[Access(typeof(SkatesSystem))]
public sealed partial class SkatesComponent : Component
{
    /// <summary>
    /// the levels of friction the wearer is subected to, higher the number the more friction.
    /// </summary>
    [DataField]
    public float Friction = 0.125f;

    /// <summary>
    /// Determines the turning ability of the wearer, Higher the number the less control of their turning ability.
    /// </summary>
    [DataField]
    public float FrictionNoInput = 0.125f;

    /// <summary>
    /// Sets the speed in which the wearer accelerates to full speed, higher the number the quicker the acceleration.
    /// </summary>
    [DataField]
    public float Acceleration = 0.25f;

    /// <summary>
    /// The minimum speed the wearer needs to be traveling to take damage from collision.
    /// </summary>
    [DataField]
    public float MinimumSpeed = 3f;

    /// <summary>
    /// The length of time the wearer is stunned for on collision.
    /// </summary>
    [DataField]
    public float StunSeconds = 3f;


    /// <summary>
    /// The time duration before another collision can take place.
    /// </summary>
    [DataField]
    public float DamageCooldown = 2f;

    /// <summary>
    /// The damage per increment of speed on collision.
    /// </summary>
    [DataField]
    public float SpeedDamage = 1f;


    /// <summary>
    /// Defaults for MinimumSpeed, StunSeconds, DamageCooldown and SpeedDamage.
    /// </summary>
    [ViewVariables]
    public float DefaultMinimumSpeed = 20f;

    [ViewVariables]
    public float DefaultStunSeconds = 1f;

    [ViewVariables]
    public float DefaultDamageCooldown = 2f;

    [ViewVariables]
    public float DefaultSpeedDamage = 0.5f;
}
