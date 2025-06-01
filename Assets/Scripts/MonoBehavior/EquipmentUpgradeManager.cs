using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SynthUpgrade
{
    /// Oscillator
    SecondOscillator,
        SecondOscillatorSemiTune,
        SecondOscillatorFineTune,
    /// Filter
    Filter,
        FilterResonance,
        FilterEnveloppe,
    /// Unisson
    Unisson,
        UnissonSpread,
    /// Voices
    Voices,

}

public class EquipmentUpgradeManager : MonoBehaviour
{

    public static ushort numOfPossibleUpgrades;
    public short numOfAvailableUpgrades;

    public static SynthUpgrade[] BaseSynthUpgradeOption = new SynthUpgrade[]
    {
        SynthUpgrade.SecondOscillator,
        SynthUpgrade.Filter,
        SynthUpgrade.Unisson,
        SynthUpgrade.Voices,
    };

    public List<bool[]> synthsActivatedFeatures;

    public List<List<SynthUpgrade>> synthEquipmentsUpgradeOptions;
    /// to do
    ///public List<List<>> DrumMachineEquipmentsUpgradeOptions;

  


}
