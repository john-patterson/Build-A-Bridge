using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    public Camera ObliqueCamera;
    public Camera PlayerCamera;
    public ChosenCamera StartingCamera;

    private AudioListener ObliqueCameraAL;
    private AudioListener PlayerCameraAL;

    private bool _testKeyLock = false;

    public enum ChosenCamera {
        Oblique,
        Player
    };

    public ChosenCamera CurrentCamera { get; private set; }

    void Start()
    {
        ObliqueCameraAL = ObliqueCamera.GetComponent<AudioListener>();
        PlayerCameraAL = PlayerCamera.GetComponent<AudioListener>();
        SetActiveCamera(StartingCamera);
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.C))
        {
            if (!_testKeyLock)
            {
                _testKeyLock = true;
                SetActiveCamera(CurrentCamera == ChosenCamera.Oblique ? ChosenCamera.Player : ChosenCamera.Oblique);
            }
        }
        else
        {
            _testKeyLock = false;
        }
    }

    public void SetActiveCamera(ChosenCamera newActiveCamera)
    {
        switch (newActiveCamera)
        {
            case ChosenCamera.Oblique:
                PlayerCamera.gameObject.SetActive(false);
                ObliqueCamera.gameObject.SetActive(true);
                CurrentCamera = ChosenCamera.Oblique;
                break;
            case ChosenCamera.Player:
                ObliqueCamera.gameObject.SetActive(false);
                PlayerCamera.gameObject.SetActive(true);
                CurrentCamera = ChosenCamera.Player;
                break;
        }
    }

}
