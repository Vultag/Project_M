using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class ToDrumPadShader : MonoBehaviour
{

    [HideInInspector]
    private Material DrumPadMaterial;

    private EntityManager entityManager;

    public EntityQuery Controlled_Weapon_query;
    private EntityQuery CentralizedInputDataQuery;

    private float pressInertia;


    void Start()
    {
        DrumPadMaterial = GetComponent<Renderer>().material; // Get the Renderer component
        var world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
        Controlled_Weapon_query = entityManager.CreateEntityQuery(typeof(ControledWeaponTag));
        CentralizedInputDataQuery = entityManager.CreateEntityQuery(typeof(CentralizedInputData));
        UpdateInstrumentCount(1);
    }
    private void LateUpdate()
    {
        /// Disable GB instead
        if (Controlled_Weapon_query.IsEmpty)
        {
            return;
        }
        var inputs = entityManager.GetComponentData<CentralizedInputData>(CentralizedInputDataQuery.GetSingletonEntity());

        //redondant ?
        Entity weapon_entity = Controlled_Weapon_query.GetSingletonEntity();
        var weaponTrans = entityManager.GetComponentData<LocalToWorld>(weapon_entity).Value;
        var playerTrans = entityManager.GetComponentData<LocalToWorld>(AudioManager.Instance.Player_query.GetSingletonEntity()).Value;

        Vector3 mousepos = Camera.main.ScreenToWorldPoint(InputManager.mousePos);
        /// 0.4f offset from center
        float2 weaponPos = new float2(weaponTrans.Translation().x, weaponTrans.Translation().y);
        Quaternion playerRotation = playerTrans.Rotation();

        Vector2 mouseDir = new Vector2(mousepos.x - weaponPos.x, mousepos.y - weaponPos.y);
        ///mouseDir = math.mul(Quaternion.Inverse(playerRotation), new float3(mouseDir.x, mouseDir.y, 0)).xy;

        float mouseNormRadian = Mathf.Atan2(mouseDir.x, mouseDir.y) / Mathf.PI;

        pressInertia = inputs.shootJustPressed ? 1 : Mathf.Lerp(pressInertia, pressInertia * 0.25f, Time.deltaTime * 4);

        ///UNT0022 OPTI
        ///this.transform.parent.rotation = playerRotation;
        this.transform.parent.position = weaponTrans.Translation();

        //Debug.Log((mouseNormRadian + 1) * 0.5f);

        DrumPadMaterial.SetVector("_MouseInfo", new Vector4((mouseNormRadian+1)*0.5f, Mathf.Sign(mouseNormRadian), pressInertia, 0));


    }

    public void UpdateInstrumentCount(int instrumentnumber)
    {
        DrumPadMaterial.SetFloat("_InstrumentNumber", instrumentnumber);
    }
}
