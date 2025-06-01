using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

public class LevelUpUI : MonoBehaviour
{
    [SerializeField]
    private GameObject LevelingProgressUI;
    [SerializeField]
    private GameObject ChoicePanel;
    [HideInInspector]
    public short numOfLvlUp = 0;
    ushort totalLvlUpEffectNum = 0;

    List<LvlUpEffect> possibleEffectPool;

    short armedEffect0Idx;
    short armedEffect1Idx;
    short armedEffect2Idx;

    [SerializeField]
    TextMeshProUGUI tempPick0txt;
    [SerializeField]
    TextMeshProUGUI tempPick1txt;
    [SerializeField]
    TextMeshProUGUI tempPick2txt;

    public enum LvlUpEffect
    {
        Gain3EquipmentsUpgrade,
        Effect1,
        Effect2,
        Effect3,
        Effect4,
        Effect5,
    }

    void Start()
    {
        totalLvlUpEffectNum = (ushort)Enum.GetValues(typeof(LvlUpEffect)).Length;
        possibleEffectPool = new List<LvlUpEffect>(totalLvlUpEffectNum);
        for (int i = 0; i < totalLvlUpEffectNum; i++)
        {
            possibleEffectPool.Add((LvlUpEffect)i);
        }
    }
    public void OpenChocies()
    {
        if (ChoicePanel.activeSelf)
        {
            ChoicePanel.SetActive(false);
        }
        else
        {
            /// FOR TESTING -> MOVE AWAY
            InitiatePicks();
            ChoicePanel.SetActive(true);
        }
    }
    public void InitiatePicks()
    {
        Generate3UniqueRandomShorts(possibleEffectPool.Count, out short FirstIdx, out short SecondIdx, out short ThirdIdx);
        armedEffect0Idx = FirstIdx;
        armedEffect1Idx = SecondIdx;
        armedEffect2Idx = ThirdIdx;
        tempPick0txt.text = possibleEffectPool[FirstIdx].ToString();
        tempPick1txt.text = possibleEffectPool[SecondIdx].ToString();
        tempPick2txt.text = possibleEffectPool[ThirdIdx].ToString();
    }

    public void Pick0()
    {
        //ActivateLvlEffect(PopRandomUnordered(armedEffect0Idx));
        ActivateLvlEffect(possibleEffectPool[armedEffect0Idx]);
    }
    public void Pick1()
    {
        //ActivateLvlEffect(PopRandomUnordered(armedEffect1Idx));
        ActivateLvlEffect(possibleEffectPool[armedEffect1Idx]);
    }
    public void Pick2()
    {
        //ActivateLvlEffect(PopRandomUnordered(armedEffect2Idx));
        ActivateLvlEffect(possibleEffectPool[armedEffect2Idx]);
    }
    public void ActivateLvlEffect(LvlUpEffect effect)
    {
        switch (effect)
        {
            case LvlUpEffect.Gain3EquipmentsUpgrade:
                UIManager.Instance.equipmentToolBar.GetComponent<EquipmentUpgradeManager>().numOfAvailableUpgrades += 3;
                UIManager.Instance.equipmentToolBar.transform.GetChild(UIManager.Instance.activeEquipmentIdx).GetComponent<EquipmentUIelement>().upgradeButtonGB.SetActive(true);
                break;
            case LvlUpEffect.Effect1:
                Debug.Log("effect 1");
                break;
            case LvlUpEffect.Effect2:
                Debug.Log("effect 2");
                break;
            case LvlUpEffect.Effect3:
                Debug.Log("effect 3");
                break;
            case LvlUpEffect.Effect4:
                Debug.Log("effect 4");
                break;
            case LvlUpEffect.Effect5:
                Debug.Log("effect 5");
                break;
        }
        ChoicePanel.SetActive(false);
        numOfLvlUp--;
        if (numOfLvlUp < 1)
            this.gameObject.SetActive(false);
    }


    void Generate3UniqueRandomShorts(int maxExclusive, out short a, out short b, out short c)
    {

        a = (short)Random.Range(0,maxExclusive);

        // Generate b different from a
        do { b = (short)Random.Range(0, maxExclusive); } while (b == a);

        // Generate c different from a and b
        do { c = (short)Random.Range(0, maxExclusive); } while (c == a || c == b);
    }
    LvlUpEffect PopRandomUnordered(short idx)
    {
        LvlUpEffect value = possibleEffectPool[idx];
        int last = possibleEffectPool.Count - 1;
        possibleEffectPool[idx] = possibleEffectPool[last]; // move last element into the hole
        possibleEffectPool.RemoveAt(last);      // O(1) remove at end
        return value;
    }
}
