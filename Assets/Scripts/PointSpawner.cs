using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PointSpawner : MonoBehaviour
{
    private int wallMask;
    public Transform sphere;
    public Transform plank;

    public Transform startCliff;
    public Transform endCliff;

    public Transform startPoint;
    public Transform endPoint;

    private bool mouseLock;
    private bool bridgeDoneLock;

    private Vector3 startCoord;
    private Vector3 endCoord;
    private List<Vector3> plankPoints;
    private List<Transform> planks;
    private List<Transform> planksList; 
    private List<HingeJoint> hinges; 

	// Use this for initialization
	void Start ()
	{
	    mouseLock = false;
	    bridgeDoneLock = false;


	    wallMask = LayerMask.GetMask("ClickingPlane");
        plankPoints = new List<Vector3>();
        planks = new List<Transform>();
        planksList = new List<Transform>();
        hinges = new List<HingeJoint>();

	    startCoord = startPoint.position;
	    endCoord = endPoint.position;

	    startCoord.z = 0;
	    endCoord.z = 0;

	    var everythingMask = LayerMask.GetMask("Default");
        Physics.IgnoreLayerCollision(gameObject.layer, everythingMask);
    }
	
	// Update is called once per frame
	void FixedUpdate ()
	{
        if (bridgeDoneLock) return;

        if (Input.GetButton("Fire2"))
        {
            bridgeDoneLock = true;
	        foreach (var p in planksList)
	        {
	            UnfreezePlank(p); 
	        }
            FinishBridge();
        }

	    if (Input.GetButton("Fire1"))
	    {
	        if (mouseLock) return;

	        mouseLock = true;
	        SpawnPoint();
	    }
	    else
	    {
	        mouseLock = false;
	    }
	}


    void SpawnPoint()
    {
        var camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit planeHit;

        if (!Physics.Raycast(camRay, out planeHit, 100, wallMask)) return;

        Instantiate(sphere, planeHit.point, Quaternion.identity);
        if (plankPoints.Count > 0)
        {
            var plank = DrawPlank(plankPoints.Last(), planeHit.point);

            if (planks.Count > 0)
            {
                var hinge = MakeHinge(plank, planks.Last());
                hinges.Add(hinge);
            }

            planks.Add(plank);
            planksList.Add(plank);
        }

        plankPoints.Add(planeHit.point);
    }

    Transform DrawPlank(Vector3 p1, Vector3 p2)
    {
        p1.z = 0;
        p2.z = 0;
        var obj = (Transform)Instantiate(plank, p1, Quaternion.identity);
        var directionalVector = p2 - p1;
        var distance = directionalVector.magnitude;

        obj.localScale = new Vector3(distance * 0.95f, obj.localScale.y, obj.localScale.z);

        obj.position = (p1 + (directionalVector / 2.0f));
        obj.rotation = Quaternion.AngleAxis(Mathf.Atan((p2.y-p1.y)/(p2.x - p1.x)) * Mathf.Rad2Deg, Vector3.forward);

        
        FreezePlank(obj);
        return obj;
    }

    void FinishBridge()
    {
        var firstPoint = plankPoints.First();
        var lastPoint = plankPoints.Last();

        var startingPlank = DrawPlank(startCoord, firstPoint);
        var endingPlank = DrawPlank(lastPoint, endCoord);

        var topPlank = planks.First();
        var backPlank = planks.Last();

        planks.Insert(0, startingPlank);
        planks.Add(endingPlank);

        var startHinge = MakeHinge(startingPlank, topPlank);
        var endHinge = MakeHinge(endingPlank, backPlank);

        hinges.Insert(0, startHinge);
        hinges.Add(endHinge);
    }

    HingeJoint MakeHinge(Component plank1, Component plank2) 
    {
        var tempHinge = plank1.gameObject.AddComponent<HingeJoint>();
        
        tempHinge.connectedBody = plank2.gameObject.GetComponent<Rigidbody>();
        tempHinge.autoConfigureConnectedAnchor = true;
        tempHinge.enablePreprocessing = false;
        tempHinge.axis = new Vector3(1, 0, 0);
        tempHinge.breakForce = 250;
        tempHinge.useSpring = true;

        //tempHinge.anchor = new Vector3(0.0f, 0.5f, 0.0f);
        //tempHinge.connectedAnchor = new Vector3(plank2.transform.localScale.x / -2.0f, 0.5f, 0.0f);


        return tempHinge;
    }

    void FreezePlank(Transform p)
    {
        var rb = p.gameObject.GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void UnfreezePlank(Transform p)
    {
        var rb = p.gameObject.GetComponent<Rigidbody>();
        rb.isKinematic = false;

    }
}
