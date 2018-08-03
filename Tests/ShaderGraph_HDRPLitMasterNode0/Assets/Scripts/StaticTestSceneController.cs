using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticTestSceneController : TestSceneController {

    public override void ExtraFunctionality()
    {

    }

    protected override GameObject GetRotationPointGameObj()
    {
        if (currentObj != null && currentObj.isStatic) 
        {
            return currentObj;
        }
        else return gameObject;
    }

}
