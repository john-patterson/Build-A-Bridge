using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    public RUISCamera ObliqueCamera;
    
    public ChosenCamera StartingCamera;
    public RUISDisplay MainDisplay;
    public RUISCamera CharacterCamera;
    public GameObject Character;
    public RUISInputManager InputManager;
    public PlayerSizer Sizer;

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

        if (playerSized && !playerMoved)
        {
            Sizer.MovePlayer();
            playerMoved = true;
        }

    }

    private bool playerMoved = false;
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
        FindObjectOfType<RUISDisplayManager>().UpdateDisplays();
    }

    private bool playerSized = false;
    private void ActivateKinect()
    {
        //CharacterCamera.SetActive(true);
        //InputManager.enableKinect2 = true;
        //var characterCamera = CharacterCamera.GetComponentInChildren<RUISCamera>();
//        characterCamera.gameObject.SetActive(true);
        CharacterCamera.gameObject.SetActive(true);
        MainDisplay.linkedCamera = CharacterCamera;
        Sizer.SizePlayer();
        playerSized = true;
        Character.gameObject.GetComponent<Rigidbody>().useGravity = true;
    }

    private void DeactivateKinect()
    {
        Character.gameObject.GetComponent<Rigidbody>().useGravity = false;

        //var characterCamera = CharacterCamera.GetComponentInChildren<RUISCamera>();
        //characterCamera.gameObject.SetActive(false);
        //CharacterCamera.SetActive(false);
        //InputManager.enableKinect2 = false;
    }

}
