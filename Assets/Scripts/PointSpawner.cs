using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Leap;
using Leap.Unity;

public class PointSpawner : MonoBehaviour
{
   // private InputManager _input;

    private int _wallMask;
    public Transform Sphere;
    public Transform Plank;

    public Transform StartCliff;
    public Transform EndCliff;

    public Transform StartPoint;
    public Transform EndPoint;

    public bool DebugEnabled = true;
    public float JointStrength = 100.0f;

    public RigidFinger PointerFinger;

    private CameraController _cameraController;
    private bool _mouseLock;
    private bool _bridgeDoneLock;
    

    private Vector3 _startCoord;
    private Vector3 _endCoord;
    private List<Vector3> _plankPoints;
    private List<Transform> _planks;
    private List<Transform> _planksList; 
    private List<HingeJoint> _hinges;
    private List<Transform> _points;
    public Transform CylinderObj;

	// Use this for initialization
	void Start ()
	{
	    _mouseLock = false;
	    _bridgeDoneLock = false;
	    _bridgeDoneLock = !DebugEnabled;


	    _wallMask = LayerMask.GetMask("ClickingPlane");
        _plankPoints = new List<Vector3>();
        _planks = new List<Transform>();
        _planksList = new List<Transform>();
        _hinges = new List<HingeJoint>();
        _points = new List<Transform>() {StartPoint, EndPoint};

	    _startCoord = StartPoint.position;
	    _endCoord = EndPoint.position;

	    _startCoord.z = 0;
	    _endCoord.z = 0;

	    var everythingMask = LayerMask.GetMask("Default");
        Physics.IgnoreLayerCollision(gameObject.layer, everythingMask);

	    _cameraController = GameObject.Find("CameraManager").GetComponent<CameraController>();
	    //_input = FindObjectOfType<InputManager>();
	}

    void Update()
    {
        if (!CylinderObj.gameObject.activeSelf)
            return;

        var pointingRay = PointerFinger.GetBoneDirection((int)PointerFinger.fingerType);
        var fingerPoint = PointerFinger.GetBoneCenter((int)PointerFinger.fingerType);

        var point = fingerPoint + CylinderObj.localScale.y * pointingRay.normalized;
        var rotation = Quaternion.LookRotation(pointingRay) * Quaternion.AngleAxis(90, new Vector3(1, 0, 0));
        CylinderObj.rotation = rotation;
        CylinderObj.position = point;
    }
	
	// Update is called once per frame
	void FixedUpdate ()
	{
	    //_input.PlacePoint();

        if (_bridgeDoneLock) return;

        if (Input.GetButton("Fire2"))
        {
            _bridgeDoneLock = true;
	        foreach (var p in _planksList)
	        {
	            UnfreezePlank(p); 
	        }
            FinishBridge();
            _cameraController.SetActiveCamera(CameraController.ChosenCamera.Player);

        }

	    if (Input.GetButton("Fire1"))
	    {
	        if (_mouseLock) return;

	        _mouseLock = true;
	        SpawnPoint();
	    }
	    else
	    {
	        _mouseLock = false;
	    }
	}


    void SpawnPoint()
    {
        var camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit planeHit;

        if (!Physics.Raycast(camRay, out planeHit, 100, _wallMask)) return;

        _points.Add((Transform)Instantiate(Sphere, planeHit.point, Quaternion.identity));
        if (_plankPoints.Count > 0)
        {
            var plank = DrawPlank(_plankPoints.Last(), planeHit.point);

            if (_planks.Count > 0)
            {
                var hinge = MakeHinge(plank, _planks.Last());
                _hinges.Add(hinge);
            }

            _planks.Add(plank);
            _planksList.Add(plank);
        }

        _plankPoints.Add(planeHit.point);
    }

    Transform DrawPlank(Vector3 p1, Vector3 p2)
    {
        p1.z = 0;
        p2.z = 0;
        var obj = (Transform)Instantiate(Plank, p1, Quaternion.identity);
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
        var firstPoint = _plankPoints.First();
        var lastPoint = _plankPoints.Last();

        var startingPlank = DrawPlank(_startCoord, firstPoint);
        var endingPlank = DrawPlank(lastPoint, _endCoord);

        var topPlank = _planks.First();
        var backPlank = _planks.Last();

        _planks.Insert(0, startingPlank);
        _planks.Add(endingPlank);

        var startHinge = MakeHinge(startingPlank, topPlank);
        var endHinge = MakeHinge(endingPlank, backPlank);

        _hinges.Insert(0, startHinge);
        _hinges.Add(endHinge);

        foreach (var point in _points)
        {
            point.gameObject.SetActive(false);
        }

    }

    HingeJoint MakeHinge(Component plank1, Component plank2) 
    {
        var tempHinge = plank1.gameObject.AddComponent<HingeJoint>();
        
        tempHinge.connectedBody = plank2.gameObject.GetComponent<Rigidbody>();
        tempHinge.autoConfigureConnectedAnchor = true;
        tempHinge.enablePreprocessing = false;
        tempHinge.axis = new Vector3(1, 0, 0);
        tempHinge.breakForce = JointStrength;
        //tempHinge.useSpring = true;

        //tempHinge.anchor = new Vector3(0.0f, 0.5f, 0.0f);
        //tempHinge.connectedAnchor = new Vector3(plank2.transform.localScale.x / -2.0f, 0.5f, 0.0f);


        return tempHinge;
    }

    void FreezePlank(Component p)
    {
        var rb = p.gameObject.GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void UnfreezePlank(Component p)
    {
        var rb = p.gameObject.GetComponent<Rigidbody>();
        rb.isKinematic = false;

    }
}
