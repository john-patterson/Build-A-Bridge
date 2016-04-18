using UnityEngine;
using System.Collections;

public class PlayerSizer : MonoBehaviour {
    public Transform Player;
    public PointSpawner Spawner;
    public Transform CharacterRoot;
    public Transform StartingPoint;


	public void SizePlayer()
    {
        var size = Spawner.GetBridgeLength()/2f;
        Player.transform.localScale = new Vector3(size, size, size);
    }

    public void MovePlayer()
    {
        var startingPoint = StartingPoint.position;
        var currentPosition = CharacterRoot.position;
        var localPosition = CharacterRoot.localPosition;

        var diff = -CharacterRoot.position + startingPoint;
        Player.position += diff;
        Player.position += new Vector3(0,Player.localScale.x + 2,0);
    }
}
