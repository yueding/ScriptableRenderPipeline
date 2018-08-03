using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CanvasController : MonoBehaviour {

    public Text titleText;
    public Text viewModeText;
    public Text switchText;
    public Text lightRotText;
    public Text hideText;

    public Text[] extraOptions;

    void Start()
    {
        switchText.text = "<i>a/d</i>: Switch Subscenes";
        lightRotText.text = "<i>z</i>: Light Rotation";
        hideText.text = "<i>q</i>: Hide UI";
    }

    public void UpdateText(string name, int mode, int maxMode, bool shaderGraphObj)
    {
        if (name == null) 
        {
            titleText.text = "<b>"+SceneManager.GetActiveScene().name+"</b>";
            viewModeText.gameObject.SetActive(false);
            switchText.gameObject.SetActive(false);
            lightRotText.gameObject.SetActive(false);
            hideText.gameObject.SetActive(false);

            foreach (Text t in extraOptions) t.gameObject.SetActive(false);
        }
        else
        {
            titleText.text = "<b>("+mode+"/"+maxMode+"):</b> "+name;
            viewModeText.gameObject.SetActive(true);
            switchText.gameObject.SetActive(true);
            lightRotText.gameObject.SetActive(true);
            hideText.gameObject.SetActive(true);

            foreach (Text t in extraOptions) t.gameObject.SetActive(true);

            if (shaderGraphObj) viewModeText.text = "<i>Space:</i> <b>Shader Graph</b> / Pipeline Shader";
            else viewModeText.text = "<i>Space:</i> Shader Graph / <b>Pipeline Shader</b>";
        }
    }

}
