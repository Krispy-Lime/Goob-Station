using Content.Goobstation.Server.Nutrition.Components;
using Content.Server.Body.Components;
using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;


/// <summary>
/// System for bongs
/// </summary>
namespace Content.Server.Nutrition.EntitySystems
{
    public sealed partial class SmokingSystem
    {
        private void InitializeBongs()
        {
            SubscribeLocalEvent<BongComponent, AfterInteractEvent>(OnBongInteraction);
            SubscribeLocalEvent<BongComponent, VapeDoAfterEvent>(OnBongDoAfter); // Reusing VapeDoAfterEvent
            SubscribeLocalEvent<BongComponent, ComponentInit>(OnComponentInit);
        }

        public void OnComponentInit(Entity<BongComponent> entity, ref ComponentInit args)
        {
            _itemSlotsSystem.AddItemSlot(entity, BongComponent.BowlSlotId, entity.Comp.BowlSlot);
        }


        private void OnBongInteraction(Entity<BongComponent> entity, ref AfterInteractEvent args)
        {
            var delay = entity.Comp.Delay;
            var forced = true;

            if (!args.CanReach
                || !_solutionContainerSystem.TryGetRefillableSolution(entity.Owner, out _, out var solution)
                || !HasComp<BloodstreamComponent>(args.Target)
                || _foodSystem.IsMouthBlocked(args.Target.Value, args.User)
                )
            {
                return;
            }

            if (solution.Contents.Count == 0)
            {
                _popupSystem.PopupEntity("The chamber has no liquid", args.Target.Value,
                    args.User); // Replace when done
                return;
            }

            if (!EntityManager.TryGetComponent(entity, out SmokableComponent? smokable)
                || entity.Comp.BowlSlot.Item == null)
            {
                _popupSystem.PopupEntity("The bowl is empty", args.Target.Value,
                    args.User); // Replace when done
                return;
            }

            if (args.Target == args.User)
            {
                delay = entity.Comp.UserDelay;
                forced = false;
            }

            if (forced)
            {
                var targetName = Identity.Entity(args.Target.Value, EntityManager);
                var userName = Identity.Entity(args.User, EntityManager);

                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-try-use-vape-forced", ("user", userName)), args.Target.Value,
                    args.Target.Value);

                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-try-use-vape-forced-user", ("target", targetName)), args.User,
                    args.User);
            }
            else
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-try-use-vape"), args.User,
                    args.User);
            }

            var bongDoAfterEvent = new VapeDoAfterEvent(solution, forced);
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, delay, bongDoAfterEvent, entity.Owner, target: args.Target, used: entity.Owner)
            {
                BreakOnMove = false,
                BreakOnDamage = true,
                MultiplyDelay = false, // Goobstation
            });

            args.Handled = true;
        }

        private void OnBongDoAfter(Entity<BongComponent> entity, ref VapeDoAfterEvent args)
        {
            if (args.Cancelled || args.Handled || args.Args.Target == null)
                return;

            if (EntityManager.TryGetComponent(entity, out SmokableComponent? smokable))
                return;

            var environment = _atmos.GetContainingMixture(args.Args.Target.Value, true, true);
            if (environment == null || smokable == null)
            {
                return;
            }

            if (TryTransferReagents(entity, (entity.Owner, smokable)) == false)
                return;

            if (args.Forced)
            {
                var targetName = Identity.Entity(args.Args.Target.Value, EntityManager);
                var userName = Identity.Entity(args.Args.User, EntityManager);

                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-vape-success-forced", ("user", userName)), args.Args.Target.Value,
                    args.Args.Target.Value);

                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-vape-success-user-forced", ("target", targetName)), args.Args.User,
                    args.Args.Target.Value);
            }
            else
            {
                _popupSystem.PopupEntity(
                    Loc.GetString("vape-component-vape-success"), args.Args.Target.Value,
                    args.Args.Target.Value);
            }

        }

        // Convert smokable item into reagents to be smoked (Ripped off from SmokingPipeSystem)
        private bool TryTransferReagents(Entity<BongComponent> entity, Entity<SmokableComponent> smokable)
        {
            if (entity.Comp.BowlSlot.Item == null)
            {
                return false;
            }

            EntityUid contents = entity.Comp.BowlSlot.Item.Value;

            if (!TryComp<SolutionContainerManagerComponent>(contents, out var reagents) ||
                !_solutionContainerSystem.TryGetSolution(smokable.Owner, smokable.Comp.Solution, out var bongSolution, out _))
                return false;

            foreach (var (_, soln) in _solutionContainerSystem.EnumerateSolutions((contents, reagents)))
            {
                var reagentSolution = soln.Comp.Solution;
                _solutionContainerSystem.TryAddSolution(bongSolution.Value, reagentSolution);
            }

            EntityManager.DeleteEntity(contents);

            _itemSlotsSystem.SetLock(entity.Owner, entity.Comp.BowlSlot, true); //no inserting more until current runs out

            return true;
        }
    }
}
