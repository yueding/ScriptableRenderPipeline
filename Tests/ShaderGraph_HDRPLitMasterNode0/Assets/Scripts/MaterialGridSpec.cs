using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialGridSpec : MaterialGrid {

    private float f1;

    public Color colMin;
    public Color colMax;
    private Color col;

    protected override void SetValues(float l1, float l2)
    {
        f1 = l1;
        col = Color.Lerp(colMin, colMax, l2);
    }

    protected override void SetMaterial()
    {
        mat.SetFloat(propName1, f1);
        mat.SetColor(propName2, col);
    }

}
