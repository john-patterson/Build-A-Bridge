using UnityEngine;

public class FootSound : MonoBehaviour
{
    public AudioSource WoodSound;
    public string WoodTag = "Wood";
    public AudioSource StoneSound;
    public string StoneTag = "Stone";

    private AudioSource CurrentSound;

    public bool CutoffOnExit = false;
    public bool RepeatInside = false;

    void Awake()
    {
        CurrentSound = StoneSound;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == WoodTag)
            PlayWood();
        else if (other.tag == StoneTag)
            PlayStone();    
    }

    void OnTriggerExit(Collider other)
    {
        if (other.tag != WoodTag && other.tag != StoneTag)
            return;
        if (CutoffOnExit)
            CurrentSound.Stop();
    }

    void OnTriggerStay(Collider other)
    {
        if (other.tag != WoodTag && other.tag != StoneTag)
            return;
        if (RepeatInside && !CurrentSound.isPlaying)
            CurrentSound.Play();
    }


    void PlayWood()
    {
        if (CurrentSound == WoodSound && CurrentSound.isPlaying)
            return;
        CurrentSound.Stop();
        CurrentSound = WoodSound;
        CurrentSound.Play();
    }

    void PlayStone()
    {
        if (CurrentSound == StoneSound && CurrentSound.isPlaying)
            return;
        CurrentSound.Stop();
        CurrentSound = StoneSound;
        CurrentSound.Play(); ;
    }

}
