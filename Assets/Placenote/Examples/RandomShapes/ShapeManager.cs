using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.iOS;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

// Classes to hold shape information

[System.Serializable]
public class ShapeInfo
{
    public float px;
    public float py;
    public float pz;
    public float qx;
    public float qy;
    public float qz;
    public float qw;
    public int shapeType;
    public int colorType;
    public string Message;
}


[System.Serializable]
public class ShapeList
{
    public ShapeInfo[] shapes;
}



// Main Class for Managing Markers

public class ShapeManager : MonoBehaviour
{
    public GameObject PanelSetMessage;
    public GameObject modelPrefab; // 3 prefabs are attached in the inspector
    public List<ShapeInfo> shapeInfoList = new List<ShapeInfo>();
    public List<GameObject> shapeObjList = new List<GameObject>();
    public Material mShapeMaterial;
    public bool isAllowSetMarker;
     
    private Color[] colorTypeOptions = { Color.cyan, Color.red, Color.yellow };
    private ShapeInfo currentShapeInfo;
    GameObject currentShape;
    // Use this for initialization
    void Start()
    {
        isAllowSetMarker = false;
        PanelSetMessage.SetActive(false);
    }

    // The HitTest to Add a Marker

    bool HitTestWithResultType(ARPoint point, ARHitTestResultType resultTypes)
    {
        List<ARHitTestResult> hitResults = UnityARSessionNativeInterface.GetARSessionNativeInterface().HitTest(point, resultTypes);

        if (hitResults.Count > 0)
        {
            foreach (var hitResult in hitResults)
            {

                Debug.Log("Got hit!");

                Vector3 position = UnityARMatrixOps.GetPosition(hitResult.worldTransform);
                Quaternion rotation = UnityARMatrixOps.GetRotation(hitResult.worldTransform);

                //Transform to placenote frame of reference (planes are detected in ARKit frame of reference)
                Matrix4x4 worldTransform = Matrix4x4.TRS(position, rotation, Vector3.one);
                Matrix4x4? placenoteTransform = LibPlacenote.Instance.ProcessPose(worldTransform);

                Vector3 hitPosition = PNUtility.MatrixOps.GetPosition(placenoteTransform.Value);
                Quaternion hitRotation = PNUtility.MatrixOps.GetRotation(placenoteTransform.Value);
                PanelSetMessage.SetActive(true);
                isAllowSetMarker = false;
                ShapeInfo shapeInfo = new ShapeInfo();
                shapeInfo.px = hitPosition.x;
                shapeInfo.py = hitPosition.y;
                shapeInfo.pz = hitPosition.z;
                shapeInfo.qx = hitRotation.x;
                shapeInfo.qy = hitRotation.y;
                shapeInfo.qz = hitRotation.z;
                shapeInfo.qw = hitRotation.w;
                currentShapeInfo = shapeInfo;
                currentShape = Instantiate(modelPrefab);
                currentShape.transform.position = new Vector3(shapeInfo.px, shapeInfo.py, shapeInfo.pz);
                currentShape.transform.rotation = new Quaternion(shapeInfo.qx, shapeInfo.qy, shapeInfo.qz, shapeInfo.qw);
                // add shape
                //AddShape(hitPosition, hitRotation);


                return true;
            }
        }
        return false;
    }


    // Update function checks for hittest

    void Update()
    {

        // Check if the screen is touched
#if UNITY_EDITOR

        // for simulation in the editor

        if (Input.GetMouseButtonDown(0)&&isAllowSetMarker)
        {
            RaycastHit hit;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {

                if (EventSystem.current.currentSelectedGameObject == null)
                {
                    PanelSetMessage.SetActive(true);
                    Quaternion dropRotation = Camera.main.transform.rotation;

                    isAllowSetMarker = false;
                    ShapeInfo shapeInfo = new ShapeInfo();
                    shapeInfo.px = hit.point.x;
                    shapeInfo.py = hit.point.y;
                    shapeInfo.pz = hit.point.z;
                    shapeInfo.qx = dropRotation.x;
                    shapeInfo.qy = dropRotation.y;
                    shapeInfo.qz = dropRotation.z;
                    shapeInfo.qw = dropRotation.w;
                    currentShapeInfo = shapeInfo;
                    currentShape = Instantiate(modelPrefab);
                    currentShape.transform.position = new Vector3(shapeInfo.px, shapeInfo.py, shapeInfo.pz);
                    currentShape.transform.rotation = new Quaternion(shapeInfo.qx, shapeInfo.qy, shapeInfo.qz, shapeInfo.qw);
                    // shapeInfoList.Add(shapeInfo);

                    // GameObject shape = ShapeFromInfo(shapeInfo);
                    // shapeObjList.Add(shape);
                }

               
            }
        }

#else
        if (Input.touchCount > 0&&isAllowSetMarker)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (EventSystem.current.currentSelectedGameObject == null)
                {

                    Debug.Log("Not touching a UI button. Moving on.");

                    // add new shape
                    var screenPosition = Camera.main.ScreenToViewportPoint(touch.position);
                    ARPoint point = new ARPoint
                    {
                        x = screenPosition.x,
                        y = screenPosition.y
                    };

                    // prioritize reults types
                    ARHitTestResultType[] resultTypes = {
                        //ARHitTestResultType.ARHitTestResultTypeExistingPlaneUsingExtent,
                        //ARHitTestResultType.ARHitTestResultTypeExistingPlane,
                        //ARHitTestResultType.ARHitTestResultTypeEstimatedHorizontalPlane,
                        //ARHitTestResultType.ARHitTestResultTypeEstimatedVerticalPlane,
                        ARHitTestResultType.ARHitTestResultTypeFeaturePoint
                    };

                    foreach (ARHitTestResultType resultType in resultTypes)
                    {
                        if (HitTestWithResultType(point, resultType))
                        {
                            Debug.Log("Found a hit test result");
                            return;
                        }
                    }
                }
            }
        }
#endif
    }
    public void SetLabel(string message)
    {

        currentShapeInfo.Message = message;
        shapeInfoList.Add(currentShapeInfo);
        currentShape.GetComponent<ControllerMessage>().LabelMarker.SetActive(true);
        currentShape.GetComponent<ControllerMessage>().SetMessage(message);
        
        shapeObjList.Add(currentShape);
        PanelSetMessage.SetActive(false);
        isAllowSetMarker = true;


    }
    public void OnSimulatorDropShape()
    {
        Vector3 dropPosition = Camera.main.transform.position + Camera.main.transform.forward * 0.3f;
        Quaternion dropRotation = Camera.main.transform.rotation;

        AddShape(dropPosition, dropRotation);

    }


    // All shape management functions (add shapes, save shapes to metadata etc.

    public void AddShape(Vector3 shapePosition, Quaternion shapeRotation)
    {
        System.Random rnd = new System.Random();
        PrimitiveType type = (PrimitiveType)rnd.Next(0, 4);

        int colorType = rnd.Next(0, 3);

        ShapeInfo shapeInfo = new ShapeInfo();
        shapeInfo.px = shapePosition.x;
        shapeInfo.py = shapePosition.y;
        shapeInfo.pz = shapePosition.z;
        shapeInfo.qx = shapeRotation.x;
        shapeInfo.qy = shapeRotation.y;
        shapeInfo.qz = shapeRotation.z;
        shapeInfo.qw = shapeRotation.w;
        shapeInfo.shapeType = type.GetHashCode();
        shapeInfo.colorType = colorType;
        shapeInfoList.Add(shapeInfo);

        GameObject shape = ShapeFromInfo(shapeInfo);
        shapeObjList.Add(shape);
    }


    public GameObject ShapeFromInfo(ShapeInfo info)
    {
        //GameObject shape = GameObject.CreatePrimitive((PrimitiveType)info.shapeType);
        GameObject shape = Instantiate(modelPrefab);
        shape.transform.position = new Vector3(info.px, info.py, info.pz);
        shape.transform.rotation = new Quaternion(info.qx, info.qy, info.qz, info.qw);
        shape.GetComponent<ControllerMessage>().SetMessage(info.Message);
        //shape.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        //shape.GetComponent<MeshRenderer>().material = mShapeMaterial;
        //shape.GetComponent<MeshRenderer>().material.color = colorTypeOptions[info.colorType];
        return shape;
    }

    public void ClearShapes()
    {
        foreach (var obj in shapeObjList)
        {
            Destroy(obj);
        }
        shapeObjList.Clear();
        shapeInfoList.Clear();
    }


    public JObject Shapes2JSON()
    {
        ShapeList shapeList = new ShapeList();
        shapeList.shapes = new ShapeInfo[shapeInfoList.Count];
        for (int i = 0; i < shapeInfoList.Count; i++)
        {
            shapeList.shapes[i] = shapeInfoList[i];
        }

        return JObject.FromObject(shapeList);
    }

    public void LoadShapesJSON(JToken mapMetadata)
    {
        ClearShapes();
        if (mapMetadata is JObject && mapMetadata["shapeList"] is JObject)
        {
            ShapeList shapeList = mapMetadata["shapeList"].ToObject<ShapeList>();
            if (shapeList.shapes == null)
            {
                Debug.Log("no shapes dropped");
                return;
            }

            foreach (var shapeInfo in shapeList.shapes)
            {
                shapeInfoList.Add(shapeInfo);
                GameObject shape = ShapeFromInfo(shapeInfo);
                shapeObjList.Add(shape);
            }
        }
    }



}
