using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MetSpecSmoothController : MonoBehaviour {

    private Material mat;

    void Start()
    {
        mat = transform.GetComponent<Renderer>().material;
    }

    // public Texture t;

    void Update()
    {
        float f = (Mathf.Sin(Time.time) * 0.5f) + 0.5f;
        mat.SetFloat("_Smoothness", f);
        mat.SetFloat("_Metallic", f);

        // if (f > 0.5f) mat.SetTexture("_MainTex", t);
        // else mat.SetTexture("_MainTex", null);
    }

}
