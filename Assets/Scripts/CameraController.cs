using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    public RUISCamera ObliqueCamera;
    public RUISCamera PlayerCamera;
    public ChosenCamera StartingCamera;
    public RUISDisplay MainDisplay;

    private bool _testKeyLock = false;

    public enum ChosenCamera {
        Oblique,
        Player
    };

    public ChosenCamera CurrentCamera { get; private set; }

    void Start()
    {
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
                ObliqueCamera.gameObject.SetActive(true);
                PlayerCamera.gameObject.SetActive(false);
                MainDisplay.linkedCamera = ObliqueCamera;
                CurrentCamera = ChosenCamera.Oblique;
                break;
            case ChosenCamera.Player:
                ObliqueCamera.gameObject.SetActive(false);
                PlayerCamera.gameObject.SetActive(true);
                MainDisplay.linkedCamera = PlayerCamera;
                CurrentCamera = ChosenCamera.Player;
                break;
        }
        //FindObjectOfType<RUISDisplayManager>().UpdateDisplays();
    }

}
