using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Leap;
using Leap.Unity;


public static class EnumerableExtensions
{
    public static void Each<T>(this IEnumerable<T> @this, Action<T, int> action)
    {
        var i = 0;
        foreach (var v in @this)
            action(v, i++);
    }
}

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

    public float JointStrength = 100.0f;

    public RigidFinger PointerFinger;

    private CameraController _cameraController;
    private bool _mouseLock;
    private bool _leapLock;
    private bool _bridgeDoneLock;
    private bool _setupLock;
    

    private Vector3 _startCoord;
    private Vector3 _endCoord;
    private List<Vector3> _plankPoints;
    private List<Transform> _planks;
    private List<Transform> _planksList; 
    private List<HingeJoint> _hinges;
    private List<Transform> _points;
    public Transform CylinderObj;

    public bool LeapDebug = true;
    public LeapOutput LeapGestureManager;

    private float _width;
    private float _height;
    private const int GridN = 11;
    
    private Transform[] gridTransforms;
    private Vector3[] verticies;

    Vector3 CalculateGridAnchor()
    {
        _width = 10 * gameObject.transform.localScale.x;
        _height = 10 * gameObject.transform.localScale.y;
        return gameObject.transform.position - new Vector3(_width / 2f, _height / 2f, 0);
    }

    private Vector3 GetGridOrigin()
    {
        var pt = verticies[0];
        return pt;
    }

    private int EncodeToIndex(Vector3 pos)
    {
        if (verticies == null)
            throw new Exception("EncodeToIndex can only be used after a grid has been populated.");
        var gridPos = pos - GetGridOrigin(); // Normalized to grid size
        gridPos.x /= _width;
        gridPos.y /= _height;
        var x_offset = (_width/GridN)/2f;
        var y_offset = (_height / GridN) / 2f;

        var row = Math.Round( gridPos.y * GridN - y_offset);
        var col = Math.Round( gridPos.x * GridN + x_offset);
        return (int)Math.Floor((row*(GridN)) + col - 1);
    }

    private void GenerateGrid()
    {
        var anchor = CalculateGridAnchor();
        verticies = new Vector3[(GridN + 1)*(GridN + 1)];
        for (int  i = 0, y = 0; y < GridN; y++)
            for (var x = 0; x < GridN; x++, i++)
            {
                verticies[i] = new Vector3(anchor.x + x, anchor.y + y, anchor.z);
            }
        PaintGrid();
    }

    private void PaintGrid()
    {
        gridTransforms = new Transform[verticies.Length];
        verticies.Each((v, i) =>
        {
            var sphere = (Transform) Instantiate(Sphere, v, Quaternion.identity);
            sphere.GetComponent<MeshRenderer>().material.color = Color.cyan;
            gridTransforms[i] = sphere;
        });
    }

    void InitializeLocks()
    {
        _mouseLock = false;
        _leapLock = false;
        _bridgeDoneLock = false;
        _setupLock = true;
    }

	// Use this for initialization
	void Start ()
	{
        InitializeLocks();
        GenerateGrid();


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

        if (LeapDebug)
            CylinderObj.gameObject.SetActive(false);
	    //_input = FindObjectOfType<InputManager>();
	}

    void Update()
    {
        if (!LeapDebug)
        {
            if (_setupLock)
                _setupLock = GetSetupReady();

            if (!CylinderObj.gameObject.activeSelf)
                return;

            var pointingRay = PointerFinger.GetBoneDirection((int) PointerFinger.fingerType);
            var fingerPoint = PointerFinger.GetBoneCenter((int) PointerFinger.fingerType);

            var point = fingerPoint + CylinderObj.localScale.y*pointingRay.normalized;
            var rotation = Quaternion.LookRotation(pointingRay)*Quaternion.AngleAxis(90, new Vector3(1, 0, 0));
            CylinderObj.rotation = rotation;
            CylinderObj.position = point;
        }
        else
        {
            RaycastHit planeHit;
            if (!GetPlaneIntersection(out planeHit))
                return;
            var index = EncodeToIndex(planeHit.point);
            gridTransforms[index].GetComponent<MeshRenderer>().material.color = Color.yellow;
            gridTransforms.Each((t, i) =>
            {
                if (i != index) t.GetComponent<MeshRenderer>().material.color = Color.cyan;
            });

        }
    }

    private bool GetSetupReady()
    {
        return !Input.GetKeyDown(KeyCode.Z);
    }

    private Ray GetPointingRay()
    {
        if (LeapDebug)
            return Camera.main.ScreenPointToRay(Input.mousePosition);

        var pointingRay = PointerFinger.GetBoneDirection((int)PointerFinger.fingerType);
        var fingerPoint = PointerFinger.GetBoneCenter((int)PointerFinger.fingerType);
        return new Ray(fingerPoint, pointingRay);
    }


    private bool GetBridgeFinished()
    {
        return LeapDebug ? Input.GetButton("Fire2") : LeapGestureManager.BridgeGesture();
        //return false;
    }

    private bool GetPlacePoint()
    {
        return LeapDebug ? Input.GetButton("Fire1") : LeapGestureManager.PlacePointGesture();
    }

	// Update is called once per frame
	void FixedUpdate ()
	{
	    //_input.PlacePoint();
	    if (_setupLock && !LeapDebug) return;

        if (_bridgeDoneLock) return;

        if (GetBridgeFinished())
        {
            _bridgeDoneLock = true;
	        foreach (var p in _planksList)
	        {
	            UnfreezePlank(p); 
	        }
            FinishBridge();
            _cameraController.SetActiveCamera(CameraController.ChosenCamera.Player);

        }

	    if (GetPlacePoint())
	    {
	        if (_mouseLock && LeapDebug) return;
	        if (_leapLock && !LeapDebug) return;

	        _mouseLock = true;
	        _leapLock = true;

	        SpawnPoint();
	    }
	    else
	    {
	        _mouseLock = false;
	        _leapLock = false;

	    }
	}

    bool GetPlaneIntersection(out RaycastHit planeHit)
    {
        var ray = GetPointingRay();
        var hit = Physics.Raycast(ray, out planeHit, 100, _wallMask);
        return hit;
    }

    void SpawnPoint()
    {
        RaycastHit planeHit;
        if (!GetPlaneIntersection(out planeHit))
            return;


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

        if (!LeapDebug)
            CylinderObj.gameObject.SetActive(false);

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
