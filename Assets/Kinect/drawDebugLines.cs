using UnityEngine;
using System.Collections;

public class drawDebugLines : MonoBehaviour {
	
	private float lineLength = 1.2f;
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
	
	
	void OnDrawGizmos()
	{
		Color color;
		color = Color.green;
		// local up
		DrawHelperAtCenter(this.transform.up, color, lineLength);
		
		color.g -= 0.5f;
		// global up
		//DrawHelperAtCenter(Vector3.up, color, 0.5f);
		
		color = Color.blue;
		// local forward
		DrawHelperAtCenter(this.transform.forward, color, lineLength);
		
		color.b -= 0.5f;
		// global forward
		//DrawHelperAtCenter(Vector3.forward, color, 0.5f);
		
		color = Color.red;
		// local right
		DrawHelperAtCenter(this.transform.right, color, lineLength);
		
		color.r -= 0.5f;
		// global right
		//DrawHelperAtCenter(Vector3.right, color, 0.5f);
	}
	
	private void DrawHelperAtCenter(Vector3 direction, Color color, float scale)
	{
		Gizmos.color = color;
		Vector3 destination = transform.position + direction * scale;
		Gizmos.DrawLine(transform.position, destination);
	}
	
	
}
