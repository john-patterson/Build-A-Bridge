using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Leap.Unity;

public static class EnumerableExtensions
{
    public static void Each<T>(this IEnumerable<T> @this, Action<T, int> action)
    {
        var i = 0;
        foreach (var v in @this)
            action(v, i++);
    }

    public static void Each<T>(this IEnumerable<T> @this, Action<T> action)
    {
        foreach (var v in @this)
            action(v);
    }

    public static void SetActive(this IEnumerable<Transform> @this, bool active)
    {
        @this.Each(t => t.gameObject.SetActive(active));
    }
}

public static class TransformExtensions
{
    private static readonly Quaternion OrientingRotation = Quaternion.AngleAxis(90, new Vector3(1, 0, 0));

    public static void OrientCylinder(this Transform @this, Vector3 origin, Vector3 ray)
    {
        @this.position = origin + @this.localScale.y*ray.normalized;
        @this.rotation = Quaternion.LookRotation(ray)*OrientingRotation;
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
    private int PointerFingerType { get { return (int) PointerFinger.fingerType; } }

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
    public Transform SnappingObj;

    public bool LeapDebug = true;
    public LeapOutput LeapGestureManager;

    private float _width;
    private float _height;
    private const int GridN = 7;
    
    private Transform[] _gridTransforms;
    private Vector3[] _verticies;


    #region Grid Calculation
    Vector3 CalculateGridAnchor()
    {
        _width = 10 * gameObject.transform.localScale.x;
        _height = 10 * gameObject.transform.localScale.y;
        return gameObject.transform.position - new Vector3(_width / 2f, _height / 2f, 0);
    }

    private Vector3 GetGridOrigin()
    {
        RequiresVerticies("GetGridOrigin");
        var pt = _verticies[0];
        return pt;
    }

    private void RequiresVerticies(string method)
    {
        if (_verticies == null)
            throw new Exception(string.Format("{0} can only be used after a grid has been populated.", method));
    }
    private int EncodeToIndex(Vector3 pos)
    {
        RequiresVerticies("EncodeToIndex");
        var gridPos = pos - GetGridOrigin(); // Normalized to grid size
        gridPos.x /= _width;
        gridPos.y /= _height;
        var x_offset = (_width/GridN)/2f;
        var y_offset = (_height / GridN) / 2f;

        var row = Math.Round( gridPos.y * GridN - y_offset);
        var col = Math.Round( gridPos.x * GridN + x_offset);
        return (int)Math.Floor((row*(GridN)) + col - 1);
    }

    private double MaximumSpaceBetwenGridPoints
    {
        get {
            var delta_x = _width/GridN;
            var delta_y = _height/GridN;
            return Math.Sqrt(delta_x*delta_x + delta_y*delta_y);
        }
    }

    private void GenerateGrid()
    {
        var anchor = CalculateGridAnchor();
        _verticies = new Vector3[(GridN + 1)*(GridN + 1)];
        var delta_x = _width/GridN;
        var delta_y = _height/GridN;
        for (int i = 0, y = 0; y < GridN; y++)
        {
            for (var x = 0; x < GridN; x++, i++)
            {
                _verticies[i] = new Vector3(anchor.x + x*delta_x, anchor.y + y*delta_y, anchor.z);
            }
        }
        //for (int  i = 0, y = 0; y < GridN; y++)
        //    for (var x = 0; x < GridN; x++, i++)
        //    {
        //        _verticies[i] = new Vector3(anchor.x + x, anchor.y + y, anchor.z);
        //    }
        PaintGrid();
    }

    private void PaintGrid()
    {
        _gridTransforms = new Transform[_verticies.Length];
        _verticies.Each((v, i) =>
        {
            var sphere = (Transform) Instantiate(Sphere, v, Quaternion.identity);
            sphere.GetComponent<MeshRenderer>().material.color = Color.cyan;
            _gridTransforms[i] = sphere;
        });
    }
    #endregion

    #region Ray Resources
    private Ray GetPointingRay()
    {
        return
            GetRayToPoint(LeapDebug
                ? Input.mousePosition
                : PointerFinger.GetBoneDirection(PointerFingerType));
    }

    private Ray GetRayToPoint(Vector3 pt)
    {
        if (LeapDebug)
            return _cameraController.ObliqueCamera.leftCamera.ScreenPointToRay(pt);//Camera.main.ScreenPointToRay(pt);

        var fingerLocation = PointerFinger.GetBoneCenter(PointerFingerType);
        return new Ray(fingerLocation, pt);
    }

    bool GetPlaneIntersection(out RaycastHit planeHit)
    {
        var ray = GetPointingRay();
        var hit = Physics.Raycast(ray, out planeHit, 100, _wallMask);
        return hit;
    }
    #endregion

    #region Input Resources
    private bool GetSetupReady()
    {
        return !Input.GetKeyDown(KeyCode.Z);
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
    #endregion

    #region Setup
    void InitializeLocks()
    {
        _mouseLock = false;
        _leapLock = false;
        _bridgeDoneLock = false;
        _setupLock = true;
    }

    bool CheckLocks(params bool[] list)
    {
        var value = false;
        list.Each(l => value = value || l);
        return value;
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

	    if (LeapDebug) return;
	    CylinderObj.gameObject.SetActive(true);
	    SnappingObj.gameObject.SetActive(true);
	}
    #endregion

    #region Game Loop
    void OrientPointingCylinder(Vector3 origin)
    {
        var pointingRay = PointerFinger.GetBoneDirection(PointerFingerType);
        CylinderObj.OrientCylinder(origin, pointingRay);
    }

    void OrientSnappingCylinder(Vector3 origin)
    {
        RaycastHit hitPt;
        if (!GetPlaneIntersection(out hitPt))
        {
            SnappingObj.gameObject.SetActive(false);
            return;
        };
        SnappingObj.gameObject.SetActive(true);

        var getSnappedVertex = _verticies[EncodeToIndex(hitPt.point)];
        var ray = getSnappedVertex - origin;
        
        SnappingObj.OrientCylinder(origin, ray);
    }

    void Update()
    {
        if (!LeapDebug)
        {
            if (_setupLock)
                _setupLock = GetSetupReady();

            if (!CylinderObj.gameObject.activeSelf) return;

            var fingerPoint = PointerFinger.GetBoneCenter(PointerFingerType);
            OrientPointingCylinder(fingerPoint);
            OrientSnappingCylinder(fingerPoint);

        }
        else
        {
            RaycastHit planeHit;
            if (!GetPlaneIntersection(out planeHit))
                return;
            var index = EncodeToIndex(planeHit.point);
            _gridTransforms[index].GetComponent<MeshRenderer>().material.color = Color.yellow;
            _gridTransforms.Each((t, i) =>
            {
                if (i != index) t.GetComponent<MeshRenderer>().material.color = Color.cyan;
            });

        }
    }

	// Update is called once per frame
	void FixedUpdate ()
	{
	    if (CheckLocks(_setupLock && !LeapDebug, _bridgeDoneLock)) return;     


        if (GetBridgeFinished())
        {
            _bridgeDoneLock = true;
            _planksList.Each(UnfreezePlank);
            FinishBridge();
            _cameraController.SetActiveCamera(CameraController.ChosenCamera.Player);

        }

	    if (GetPlacePoint())
	    {
	        if (CheckLocks(_mouseLock && LeapDebug, _leapLock && !LeapDebug)) return;

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
    #endregion Game Loop

    void InvalidPlankLength()
    {
        
    }

    void SpawnPoint()
    {
        RaycastHit planeHit;
        if (!GetPlaneIntersection(out planeHit))
            return;
        var vertex = _verticies[EncodeToIndex(planeHit.point)];

        var previousPoint = _plankPoints.Any() ? _plankPoints.Last() : StartPoint.position;
        if ((vertex - previousPoint).magnitude > MaximumSpaceBetwenGridPoints)
        {
            InvalidPlankLength();
            return;
        }


        _points.Add((Transform)Instantiate(Sphere, vertex, Quaternion.identity));
        if (_plankPoints.Any())
        {
            var plank = DrawPlank(_plankPoints.Last(), vertex);

            if (_planks.Any())
            {
                var hinge = MakeHinge(plank, _planks.Last());
                _hinges.Add(hinge);
            }

            _planks.Add(plank);
            _planksList.Add(plank);
        }

        _plankPoints.Add(vertex);
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

    public float GetBridgeLength()
    {
        return _planks.Sum(p => p.localScale.x) * (20f/19f);
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

        _points.SetActive(false);
        _gridTransforms.SetActive(false);

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
