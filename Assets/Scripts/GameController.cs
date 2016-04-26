using UnityEngine;
using System.Collections;
using System.Linq;

public class GameController : MonoBehaviour
{
    const string VictoryTag = "VictoryZone";
    const string DeathTag = "DeathZone";

    public Transform BlankCube;
    public AudioSource Ambient;
    public AudioSource[] Feet;


    bool RestartInput()
    {
        return Input.GetKeyDown(KeyCode.Space);
    }

    void Start()
    {
        var mesh = BlankCube.GetComponent<MeshFilter>().mesh;
        mesh.triangles = mesh.triangles.Reverse().ToArray();
    }

    void Update()
    {
        if (RestartInput())
            GameRestart();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == VictoryTag)
            GameWin();

        if (other.tag == DeathTag)
            GameLost();
    }

    private void GameWin()
    {
        BlockOutsideStimuli();
    }

    private void GameLost()
    {
        BlockOutsideStimuli();
    }

    private void BlockOutsideStimuli()
    {
        Ambient.Stop();
        Ambient.mute = true;
        Feet.Each(f =>
        {
            f.Stop();
            f.mute = true;
        });
    }

    private void GameRestart()
    {
        Application.LoadLevel(Application.loadedLevel);
    }
}
