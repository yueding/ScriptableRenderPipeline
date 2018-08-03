using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostProcessingSceneController : TestSceneController
{
    public GameObject[] postVolumes;

    private int currentIndex = 0;

    public override void ExtraFunctionality()
    {
        if (Input.GetKeyDown("y"))
        {
            currentIndex++;
            if (currentIndex >= postVolumes.Length) currentIndex = 0;

            for (int x = 0; x < postVolumes.Length; x++)
            {
                postVolumes[x].SetActive(x == currentIndex);
            }
        }
    }
}