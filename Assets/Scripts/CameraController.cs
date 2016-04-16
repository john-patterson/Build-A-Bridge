using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    public RUISCamera ObliqueCamera;
    
    public ChosenCamera StartingCamera;
    public RUISDisplay MainDisplay;
    public GameObject Character;
    public RUISInputManager InputManager;

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
                MainDisplay.linkedCamera = ObliqueCamera;
                CurrentCamera = ChosenCamera.Oblique;
                DeactivateKinect();
                break;
            case ChosenCamera.Player:
                ObliqueCamera.gameObject.SetActive(false);
                CurrentCamera = ChosenCamera.Player;
                ActivateKinect();
                break;
        }
        //FindObjectOfType<RUISDisplayManager>().UpdateDisplays();
    }

    private void ActivateKinect()
    {
        //Character.SetActive(true);
        //InputManager.enableKinect2 = true;
        var characterCamera = Character.GetComponentInChildren<RUISCamera>();
//        characterCamera.gameObject.SetActive(true);
        MainDisplay.linkedCamera = characterCamera;
    }

    private void DeactivateKinect()
    {
        //var characterCamera = Character.GetComponentInChildren<RUISCamera>();
        //characterCamera.gameObject.SetActive(false);
        //Character.SetActive(false);
        //InputManager.enableKinect2 = false;
    }

}
