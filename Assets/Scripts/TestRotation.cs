using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestRotation : MonoBehaviour
{
    public Transform target;
    // Start is called before the first frame update
    void Start()
    {
       // Quaternion dropRotation = Camera.main.transform.rotation;
       // Debug.Log(dropRotation);
       // target.rotation = new Quaternion(dropRotation.x, dropRotation.y, dropRotation.z, dropRotation.w);
        target.position = new Vector3(target.position.x, transform.position.y, target.position.z);
        Vector3 relativePos = target.position - transform.position;
        
        // the second argument, upwards, defaults to Vector3.up
        Quaternion rotation = Quaternion.LookRotation(relativePos, Vector3.up);
        transform.rotation = rotation;
    }

    // Update is called once per frame
    void Update()
    {
        // Quaternion dropRotation = Camera.main.transform.rotation;
        // Debug.Log(dropRotation);
        // target.rotation = new Quaternion(dropRotation.x, dropRotation.y, dropRotation.z, dropRotation.w);
        target.position = new Vector3(target.position.x, transform.position.y, target.position.z);
        Vector3 relativePos = target.position - transform.position;

        // the second argument, upwards, defaults to Vector3.up
        Quaternion rotation = Quaternion.LookRotation(relativePos, Vector3.up);
        transform.rotation = rotation;
    }
}
