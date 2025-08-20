using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Atmos;
using Content.Shared.Containers.ItemSlots;

namespace Content.Goobstation.Server.Nutrition.Components // Bongs are very nutritious :godo:
{
    [RegisterComponent, Access(typeof(SmokingSystem))]
    public sealed partial class BongComponent : Component
    {
        [DataField("delay")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Delay { get; set; } = 5;

        [DataField("userDelay")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float UserDelay { get; set; } = 2;

        /// <summary>
        /// Solution volume will be divided by this number and converted to the gas
        /// </summary>
        [DataField("reductionFactor")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float ReductionFactor { get; set; } = 300f;

        public const string BowlSlotId = "bowl_slot";

        [DataField("bowl_slot")]
        public ItemSlot BowlSlot = new();
    }
}
