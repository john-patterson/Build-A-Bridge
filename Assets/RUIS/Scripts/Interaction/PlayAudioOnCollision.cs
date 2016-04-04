/*****************************************************************************

Content    :   Attach to a gameobject with AudioSource script
Authors    :   Tuukka Takala
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class PlayAudioOnCollision : MonoBehaviour 
{
	
	public GameObject triggerGameObject;
	
	new AudioSource audio;
	public enum AudioRepeating
	{
	    PlayOnce = 0,
	    RepeatWhileInside = 1,
		PlayOnceAndNeverAgain = 2
	};
	public AudioRepeating repeatMode = AudioRepeating.PlayOnce;
	public bool cutOffUponExit = false;
	private bool playedOnce = false;
	
	// Use this for initialization
	void Start () 
	{
		audio = GetComponent<AudioSource>();
	}
	
	void OnTriggerEnter(Collider other)
	{
	
		if (other.gameObject == triggerGameObject)
		{
			if(!audio.isPlaying && !playedOnce)
		    	audio.Play();
		}
	}
	
	void OnTriggerExit(Collider other)
	{
		if(other.gameObject == triggerGameObject)
		{
			if (cutOffUponExit)
			    audio.Stop();
			
			if(repeatMode != AudioRepeating.PlayOnceAndNeverAgain)
				playedOnce = false;
			else
				playedOnce = true;
		}
	}
	
    void OnTriggerStay(Collider other) 
	{
		if(other.gameObject == triggerGameObject)
		{
			if(repeatMode == AudioRepeating.RepeatWhileInside && !audio.isPlaying)
		    	audio.Play();
		}
    }
}
