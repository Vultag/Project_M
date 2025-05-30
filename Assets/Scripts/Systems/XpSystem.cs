
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

public struct XpData : IComponentData
{
    public float ThisFrameXP;
    public float currentXP;
    public float XPtillNextLVL;
    public ushort LVL;
}


[BurstCompile]
public partial struct XpSystem : ISystem
{

    private EntityQuery XpEntityQuery;
    public void OnCreate(ref SystemState state)
    {

        var xpDataEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData<XpData>(xpDataEntity, new XpData
        {
            LVL = 0,
            XPtillNextLVL = 100,
        });
        XpEntityQuery = state.EntityManager.CreateEntityQuery(typeof(XpData));


    }
    public void OnUpdate(ref SystemState state)
    {
        var xpDataE = XpEntityQuery.GetSingletonEntity();
        var newXpData = state.EntityManager.GetComponentData<XpData>(xpDataE);

        float cumulatedXP = newXpData.ThisFrameXP + newXpData.currentXP;
        for (int lvlPassed = 0; cumulatedXP > newXpData.XPtillNextLVL; lvlPassed++)
        {
            cumulatedXP -= newXpData.XPtillNextLVL;
            newXpData.XPtillNextLVL *= 1.25f;
            newXpData.LVL++;
            //Debug.Log("lvl up !");
            UIManager.Instance._UpdateXpPanel(cumulatedXP, newXpData.XPtillNextLVL, newXpData.LVL);
        }
        newXpData.currentXP = cumulatedXP;
        newXpData.ThisFrameXP = 0;

        state.EntityManager.SetComponentData<XpData>(xpDataE, newXpData);

        
    }

}
