using System;
using System.Collections;
using System.Collections.Generic;
using MusicNamespace;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

public class ToKeyboardShader : MonoBehaviour
{

    [HideInInspector]
    private Material KeyboardMaterial;
    [SerializeField]
    private TextMeshProUGUI modeText;
    private EntityManager entityManager;

    public EntityQuery Controlled_Weapon_query;
    private EntityQuery CentralizedInputDataQuery;

    private float pressInertia;

    private float[] ModeKeysArray = new float[12];


    void Start()
    {

        //temp -> put somewhere else
        WeaponSystem.mode = MusicUtils.MusicalMode.Dorian;

        KeyboardMaterial = GetComponent<Renderer>().material; // Get the Renderer component
        var world = World.DefaultGameObjectInjectionWorld;
        entityManager = world.EntityManager;
        Controlled_Weapon_query = entityManager.CreateEntityQuery(typeof(ControledWeaponTag));
        CentralizedInputDataQuery = entityManager.CreateEntityQuery(typeof(CentralizedInputData));

        KeyboardChangeMode();

    }

    public void KeyboardChangeMode()
    {

        int randomModeIdx = Random.Range(0,Enum.GetValues(typeof(MusicUtils.MusicalMode)).Length);

        for (int i = 0; i < 12; i++)
        {
            ModeKeysArray[i] = MusicUtils.Modes[randomModeIdx][i] ? 1.0f : 0.0f;
            //Debug.Log(ModeKeysArray[i]);
        }

        WeaponSystem.mode = ((MusicUtils.MusicalMode)randomModeIdx);
        modeText.text = ((MusicUtils.MusicalMode)randomModeIdx).ToString();
        KeyboardMaterial.SetFloatArray("_ModeKeysArray", ModeKeysArray);
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
        Quaternion playerRotation = math.mul(playerTrans.Rotation(), Quaternion.Euler(0, 0, 0));

        Vector2 mouseDir = new Vector2(mousepos.x - weaponPos.x, mousepos.y - weaponPos.y);
        mouseDir = math.mul(Quaternion.Inverse(playerRotation), new float3(mouseDir.x, mouseDir.y,0)).xy;

        float mouseNormRadian = Mathf.Atan2(mouseDir.x,mouseDir.y)/Mathf.PI;

        pressInertia = inputs.shootJustPressed? 1: Mathf.Lerp(pressInertia, pressInertia*0.25f, Time.deltaTime*4);

        ///UNT0022 OPTI
        this.transform.transform.rotation = playerRotation;
        this.transform.position = weaponTrans.Translation();
        //Debug.Log(Quaternion.ToEulerAngles(playerTrans.Rotation()));

        KeyboardMaterial.SetVector("_MouseInfo", new Vector4(Mathf.Abs(mouseNormRadian), Mathf.Sign(mouseNormRadian), pressInertia, 0));
    }
}
