using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IKnobController
{
    UIManager uiManager { get; }
    string UIknobChange(KnobChangeType knobChangeType, float newRot);


}
