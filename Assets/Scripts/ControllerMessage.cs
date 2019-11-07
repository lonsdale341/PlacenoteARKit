using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ControllerMessage : MonoBehaviour
{
    public GameObject LabelMarker;
    public Text LabelText;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SetMessage(string messge)
    {
        LabelText.text = messge;
    }
}
