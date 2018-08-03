    using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MaterialGrid : MonoBehaviour {

    public int gridWidth = 5;

    public float spacing = 1.5f;

    public GameObject originalObj;

    public GameObject[] goArray;

    public string propName1 = "_Smoothness";
    public string propName2 = "_Metallic";
    public float minVal1 = 0.0f;
    public float maxVal1 = 1.0f;
    public float minVal2 = 0.0f;
    public float maxVal2 = 1.0f;

    protected Material mat;

    void Start()
    {
        SetUp();
    }

    protected void SetUp()
    {
        if (originalObj == null) originalObj = transform.GetChild(0).gameObject;

        int goCount = gridWidth * gridWidth;
        float gridWidthF = (float)gridWidth;

        goArray = new GameObject[goCount];

        for (int x = 0; x < goCount; x++)
        {
            goArray[x] = (GameObject)Instantiate(originalObj);

            // Get mat value
            float lerp1 = Mathf.Lerp(minVal1, maxVal1, (float)(x / gridWidth) / (gridWidthF - 1.0f));
            float lerp2 = Mathf.Lerp(minVal2, maxVal2, (float)(x % gridWidth) / (gridWidthF - 1.0f));
            SetValues(lerp1, lerp2);

            // Other parts of the game object
            goArray[x].name = originalObj.name+"_"+lerp1.ToString("F2")+"_"+lerp2.ToString("F2");
            float negSpace = -(spacing * (float)(gridWidth - 1)) / 2.0f;
            goArray[x].transform.parent = originalObj.transform.parent;
            goArray[x].transform.localPosition = originalObj.transform.localPosition + new Vector3(
                ((float)(x / gridWidth) * spacing) + negSpace,
                0.0f,
                ((float)(x % gridWidth) * spacing) + negSpace);

            // Set material
            mat = goArray[x].GetComponent<Renderer>().material;
            SetMaterial();
        }

        originalObj.SetActive(false);
    }

    protected void RemoveGrid()
    {
        originalObj.SetActive(true);

        foreach (GameObject go in goArray)
        {
            Destroy(go);
        }
    }

    // Allows updates to happen during run time.
    void Update()
    {
        if (Input.GetKeyDown("0"))
        {
            RemoveGrid();
            SetUp();
        }
    }

    protected abstract void SetValues(float l1, float l2);

    protected abstract void SetMaterial();

}
