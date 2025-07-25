// SPDX-FileCopyrightText: 2022 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 keronshb <54602815+keronshb@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 wrexbe <wrexbe@protonmail.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Vordenburg <114301317+Vordenburg@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 keronshb <keronshb@live.com>
// SPDX-FileCopyrightText: 2024 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <aviu00@protonmail.com>
// SPDX-FileCopyrightText: 2025 BombasterDS <deniskaporoshok@gmail.com>
// SPDX-FileCopyrightText: 2025 BombasterDS2 <shvalovdenis.workmail@gmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 pubbi <63283968+impubbi@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.DoAfter;
using Content.Shared._Goobstation.Wizard.Mutate;
using Content.Shared.Alert;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Strip.Components;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Ensnaring;

[Serializable, NetSerializable]
public sealed partial class EnsnareableDoAfterEvent : SimpleDoAfterEvent
{
}

public abstract class SharedEnsnareableSystem : EntitySystem
{
    [Dependency] private   readonly INetManager _net = default!; // Goobstation
    [Dependency] private   readonly SharedHulkSystem _hulk = default!; // Goobstation
    [Dependency] private   readonly AlertsSystem _alerts = default!;
    [Dependency] private   readonly MovementSpeedModifierSystem _speedModifier = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] private   readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] private   readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private   readonly SharedHandsSystem _hands = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private   readonly SharedStaminaSystem _stamina = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnsnareableComponent, ComponentInit>(OnEnsnareInit);
        SubscribeLocalEvent<EnsnareableComponent, RefreshMovementSpeedModifiersEvent>(MovementSpeedModify);
        SubscribeLocalEvent<EnsnareableComponent, EnsnareEvent>(OnEnsnare);
        SubscribeLocalEvent<EnsnareableComponent, EnsnareRemoveEvent>(OnEnsnareRemove);
        SubscribeLocalEvent<EnsnareableComponent, EnsnaredChangedEvent>(OnEnsnareChange);
        SubscribeLocalEvent<EnsnareableComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<EnsnareableComponent, StrippingEnsnareButtonPressed>(OnStripEnsnareMessage);
        SubscribeLocalEvent<EnsnareableComponent, RemoveEnsnareAlertEvent>(OnRemoveEnsnareAlert);
        SubscribeLocalEvent<EnsnareableComponent, EnsnareableDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<EnsnaringComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<EnsnaringComponent, StepTriggerAttemptEvent>(AttemptStepTrigger);
        SubscribeLocalEvent<EnsnaringComponent, StepTriggeredOffEvent>(OnStepTrigger);
        SubscribeLocalEvent<EnsnaringComponent, ThrowDoHitEvent>(OnThrowHit);
        SubscribeLocalEvent<EnsnaringComponent, ProjectileHitEvent>(OnProjectileHit); // Goobstation
    }

    protected virtual void OnEnsnareInit(Entity<EnsnareableComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Container = Container.EnsureContainer<Container>(ent.Owner, "ensnare");
    }

    private void OnHandleState(EntityUid uid, EnsnareableComponent component, ref AfterAutoHandleStateEvent args)
    {
        RaiseLocalEvent(uid, new EnsnaredChangedEvent(component.IsEnsnared));
    }

    private void OnDoAfter(EntityUid uid, EnsnareableComponent component, DoAfterEvent args)
    {
        if (args.Args.Target == null)
            return;

        if (args.Handled || !TryComp<EnsnaringComponent>(args.Args.Used, out var ensnaring))
            return;

        if (args.Cancelled || !Container.Remove(args.Args.Used.Value, component.Container))
        {
            if (args.User == args.Target)
                Popup.PopupPredicted(Loc.GetString("ensnare-component-try-free-fail", ("ensnare", args.Args.Used)), uid, args.User, PopupType.MediumCaution);
            else if (args.Target != null)
                Popup.PopupPredicted(Loc.GetString("ensnare-component-try-free-fail-other", ("ensnare", args.Args.Used), ("user", args.Target)), uid, args.User, PopupType.MediumCaution);

            return;
        }

        component.IsEnsnared = component.Container.ContainedEntities.Count > 0;
        Dirty(uid, component);
        ensnaring.Ensnared = null;

        // Goobstation start
        if (ensnaring.DestroyOnRemove)
        {
            if (_net.IsServer)
                QueueDel(args.Args.Used.Value);
        }
        else
            _hands.PickupOrDrop(args.Args.User, args.Args.Used.Value);

        if (args.User == args.Target && TryComp(args.User, out HulkComponent? hulk))
            _hulk.Roar((args.User, hulk));
        // Goobstation end

        if (args.User == args.Target)
            Popup.PopupPredicted(Loc.GetString("ensnare-component-try-free-complete", ("ensnare", args.Args.Used)), uid, args.User, PopupType.Medium);
        else if (args.Target != null)
            Popup.PopupPredicted(Loc.GetString("ensnare-component-try-free-complete-other", ("ensnare", args.Args.Used), ("user", args.Target)), uid, args.User, PopupType.Medium);

        UpdateAlert(args.Args.Target.Value, component);
        var ev = new EnsnareRemoveEvent(ensnaring.WalkSpeed, ensnaring.SprintSpeed);
        RaiseLocalEvent(uid, ev);

        args.Handled = true;
    }

    private void OnEnsnare(EntityUid uid, EnsnareableComponent component, EnsnareEvent args)
    {
        component.WalkSpeed *= args.WalkSpeed;
        component.SprintSpeed *= args.SprintSpeed;

        _speedModifier.RefreshMovementSpeedModifiers(uid);

        var ev = new EnsnaredChangedEvent(component.IsEnsnared);
        RaiseLocalEvent(uid, ev);
    }

    private void OnEnsnareRemove(EntityUid uid, EnsnareableComponent component, EnsnareRemoveEvent args)
    {
        component.WalkSpeed /= args.WalkSpeed;
        component.SprintSpeed /= args.SprintSpeed;

        _speedModifier.RefreshMovementSpeedModifiers(uid);

        var ev = new EnsnaredChangedEvent(component.IsEnsnared);
        RaiseLocalEvent(uid, ev);
    }

    private void OnEnsnareChange(EntityUid uid, EnsnareableComponent component, EnsnaredChangedEvent args)
    {
        UpdateAppearance(uid, component);
    }

    private void UpdateAppearance(EntityUid uid, EnsnareableComponent component, AppearanceComponent? appearance = null)
    {
        Appearance.SetData(uid, EnsnareableVisuals.IsEnsnared, component.IsEnsnared, appearance);
    }

    private void MovementSpeedModify(EntityUid uid, EnsnareableComponent component,
        RefreshMovementSpeedModifiersEvent args)
    {
        if (!component.IsEnsnared)
            return;

        args.ModifySpeed(component.WalkSpeed, component.SprintSpeed);
    }

    /// <summary>
    /// Used where you want to try to free an entity with the <see cref="EnsnareableComponent"/>
    /// </summary>
    /// <param name="target">The entity that will be freed</param>
    /// <param name="user">The entity that is freeing the target</param>
    /// <param name="ensnare">The entity used to ensnare</param>
    /// <param name="component">The ensnaring component</param>
    public void TryFree(EntityUid target, EntityUid user, EntityUid ensnare, EnsnaringComponent component)
    {
        // Don't do anything if they don't have the ensnareable component.
        if (!HasComp<EnsnareableComponent>(target))
            return;

        var freeTime = user == target ? component.BreakoutTime : component.FreeTime;
        var breakOnMove = !component.CanMoveBreakout;

        if (user == target && HasComp<HulkComponent>(user)) // Goobstation
            freeTime = 0f;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, freeTime, new EnsnareableDoAfterEvent(), target, target: target, used: ensnare)
        {
            BreakOnMove = breakOnMove,
            BreakOnDamage = false,
            NeedHand = true,
            BreakOnDropItem = false,
        };

        if (!_doAfter.TryStartDoAfter(doAfterEventArgs))
            return;

        if (freeTime == 0f) // Goobstation
            return;

        if (user == target)
            Popup.PopupPredicted(Loc.GetString("ensnare-component-try-free", ("ensnare", ensnare)), target, target);
        else
            Popup.PopupPredicted(Loc.GetString("ensnare-component-try-free-other", ("ensnare", ensnare), ("user", Identity.Entity(target, EntityManager))), user, user);
    }

    private void OnStripEnsnareMessage(EntityUid uid, EnsnareableComponent component, StrippingEnsnareButtonPressed args)
    {
        foreach (var entity in component.Container.ContainedEntities)
        {
            if (!TryComp<EnsnaringComponent>(entity, out var ensnaring))
                continue;

            TryFree(uid, args.Actor, entity, ensnaring);
            return;
        }
    }

    private void OnRemoveEnsnareAlert(Entity<EnsnareableComponent> ent, ref RemoveEnsnareAlertEvent args)
    {
        if (args.Handled)
            return;

        foreach (var ensnare in ent.Comp.Container.ContainedEntities)
        {
            if (!TryComp<EnsnaringComponent>(ensnare, out var ensnaringComponent))
                continue;

            TryFree(ent, ent, ensnare, ensnaringComponent);

            args.Handled = true;
            // Only one snare at a time.
            break;
        }
    }

    private void OnComponentRemove(EntityUid uid, EnsnaringComponent component, ComponentRemove args)
    {
        if (!TryComp<EnsnareableComponent>(component.Ensnared, out var ensnared))
            return;

        if (ensnared.IsEnsnared)
            ForceFree(uid, component);
    }

    private void AttemptStepTrigger(EntityUid uid, EnsnaringComponent component, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;
    }

    private void OnStepTrigger(EntityUid uid, EnsnaringComponent component, ref StepTriggeredOffEvent args)
    {
        TryEnsnare(args.Tripper, uid, component);
    }

    private void OnThrowHit(EntityUid uid, EnsnaringComponent component, ThrowDoHitEvent args)
    {
        if (!component.CanThrowTrigger)
            return;

        if (TryEnsnare(args.Target, uid, component))
        {
            _audio.PlayPvs(component.EnsnareSound, uid);
        }
    }

    // Goobstation
    private void OnProjectileHit(EntityUid uid, EnsnaringComponent component, ProjectileHitEvent args)
    {
        if (!component.CanThrowTrigger)
            return;

        if (TryEnsnare(args.Target, uid, component))
        {
            _audio.PlayPvs(component.EnsnareSound, uid);
        }
    }

    /// <summary>
    /// Used where you want to try to ensnare an entity with the <see cref="EnsnareableComponent"/>
    /// </summary>
    /// <param name="target">The entity that will be ensnared</param>
    /// <paramref name="ensnare"> The entity that is used to ensnare</param>
    /// <param name="component">The ensnaring component</param>
    public bool TryEnsnare(EntityUid target, EntityUid ensnare, EnsnaringComponent component)
    {
        //Don't do anything if they don't have the ensnareable component.
        if (!TryComp<EnsnareableComponent>(target, out var ensnareable))
            return false;

        var numEnsnares = ensnareable.Container.ContainedEntities.Count;

        //Don't do anything if the maximum number of ensnares is applied.
        if (numEnsnares >= component.MaxEnsnares)
            return false;

        Container.Insert(ensnare, ensnareable.Container);

        // Apply stamina damage to target
        if (TryComp<StaminaComponent>(target, out var stamina))
        {
            _stamina.TakeStaminaDamage(target, component.StaminaDamage, with: ensnare, component: stamina);
        }

        component.Ensnared = target;
        ensnareable.IsEnsnared = true;
        Dirty(target, ensnareable);

        UpdateAlert(target, ensnareable);
        // Goobstation start
        var ensnaredEv = new EnsnaredEvent(target);
        RaiseLocalEvent(ensnare, ref ensnaredEv);
        // Goobstation end
        var ev = new EnsnareEvent(component.WalkSpeed, component.SprintSpeed);
        RaiseLocalEvent(target, ev);
        return true;
    }

    /// <summary>
    /// Used to force free someone for things like if the <see cref="EnsnaringComponent"/> is removed
    /// </summary>
    public void ForceFree(EntityUid ensnare, EnsnaringComponent component)
    {
        if (component.Ensnared == null)
            return;

        if (!TryComp<EnsnareableComponent>(component.Ensnared, out var ensnareable))
            return;

        var target = component.Ensnared.Value;

        Container.TryRemoveFromContainer(ensnare, force: true); // Goobstation - fix on ensnare entity remove
        ensnareable.IsEnsnared = ensnareable.Container.ContainedEntities.Count > 0;
        Dirty(component.Ensnared.Value, ensnareable);
        component.Ensnared = null;

        UpdateAlert(target, ensnareable);
        var ev = new EnsnareRemoveEvent(component.WalkSpeed, component.SprintSpeed);
        RaiseLocalEvent(target, ev);
    }

    /// <summary>
    /// Update the Ensnared alert for an entity.
    /// </summary>
    /// <param name="target">The entity that has been affected by a snare</param>
    public void UpdateAlert(EntityUid target, EnsnareableComponent component)
    {
        if (!component.IsEnsnared)
            _alerts.ClearAlert(target, component.EnsnaredAlert);
        else
            _alerts.ShowAlert(target, component.EnsnaredAlert);
    }
}
