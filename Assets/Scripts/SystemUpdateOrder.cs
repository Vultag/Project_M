using Unity.Entities;
using UnityEngine;


[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
public partial class GameSimulationSystemGroup : ComponentSystemGroup
{

}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class FixedStepGameSimulationSystemGroup : ComponentSystemGroup
{

}