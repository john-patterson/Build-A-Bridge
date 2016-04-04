using UnityEngine;
using System.Collections;

public class RUISOculusFollow : MonoBehaviour 
{
	RUISCoordinateSystem coordinateSystem;
	
	void Start() 
	{
		coordinateSystem = MonoBehaviour.FindObjectOfType(typeof(RUISCoordinateSystem)) as RUISCoordinateSystem;
	}
	
	void Update () 
	{
		if(RUISOVRManager.ovrHmd != null)
		{
			Vector3 tempSample = Vector3.zero;
			
			Ovr.Posef headpose = RUISOVRManager.ovrHmd.GetTrackingState().HeadPose.ThePose;
			float px =  headpose.Position.x;
			float py =  headpose.Position.y;
			float pz = -headpose.Position.z; // This needs to be negated TODO: might change with future OVR version
			
			tempSample = new Vector3(px, py, pz);
			
			tempSample = coordinateSystem.ConvertRawOculusDK2Location(tempSample);
			Vector3 convertedLocation = coordinateSystem.ConvertLocation(tempSample, RUISDevice.Oculus_DK2); 
			this.transform.localPosition = convertedLocation;

			if(OVRManager.capiHmd != null)
			{
				try
				{
					this.transform.localRotation = OVRManager.capiHmd.GetTrackingState().HeadPose.ThePose.Orientation.ToQuaternion();
				}
				catch(System.Exception e)
				{
					Debug.LogError(e.Message);
				}
			}
		}
	}
}
