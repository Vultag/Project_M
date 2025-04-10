using Unity.Burst;
using Unity.Entities;
using UnityEngine;


public partial class GameSimulationSystemGroup : ComponentSystemGroup{}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class FixedStepGameSimulationSystemGroup : ComponentSystemGroup{}


#region FIXED STEP


[UpdateInGroup(typeof(FixedStepGameSimulationSystemGroup))]
public partial struct PhyResolutionSystem : ISystem { }

[UpdateInGroup(typeof(FixedStepGameSimulationSystemGroup), OrderLast = true)]
public partial struct ApplyPhysicsSystem : ISystem { }

#endregion

#region GAME


[UpdateInGroup(typeof(GameSimulationSystemGroup), OrderFirst = true)]
public partial struct TreeInsersionSystem : ISystem { }

[UpdateInGroup(typeof(GameSimulationSystemGroup))]
public partial struct ProjectileSystem : ISystem { }

[UpdateInGroup(typeof(GameSimulationSystemGroup))]
public partial class WeaponSystem : SystemBase { }

[UpdateInGroup(typeof(GameSimulationSystemGroup))]
[UpdateAfter(typeof(WeaponSystem))]
public partial class PlaybackRecordSystem : SystemBase { }

[UpdateInGroup(typeof(GameSimulationSystemGroup))]
[UpdateAfter(typeof(PlaybackRecordSystem))]
public partial struct HealthSystem : ISystem { }

[UpdateInGroup(typeof(GameSimulationSystemGroup), OrderLast = true)]
public partial struct PhysicsRenderingSystem : ISystem { }

#endregion




