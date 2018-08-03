using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialGridFloat : MaterialGrid {

    private float f1;
    private float f2;

    protected override void SetValues(float l1, float l2)
    {
        f1 = l1;
        f2 = l2;
    }

    protected override void SetMaterial()
    {
        mat.SetFloat(propName1, f1);
        mat.SetFloat(propName2, f2);
    }

}
