/*****************************************************************************

Content    :   Attach to a gameobject with RUISSelectable and AudioSource scripts
Authors    :   Tuukka Takala
Copyright  :   Copyright 2013 Tuukka Takala, Mikael Matveinen. All Rights reserved.
Licensing  :   RUIS is distributed under the LGPL Version 3 license.

******************************************************************************/

using UnityEngine;
using System.Collections;

public class PlayAudioWhenSelected : MonoBehaviour {
	
	RUISSelectable selectable;
	new AudioSource audio;
	public enum AudioRepeating
	{
	    PlayOnce = 0,
	    RepeatWhileHolding = 1,
		PlayOnceAndNeverAgain = 2
	};
	public AudioRepeating repeatMode = AudioRepeating.PlayOnce;
	private bool playedOnce = false;
	
	// Use this for initialization
	void Start ()
	{
		selectable = GetComponent<RUISSelectable>();
		audio = GetComponent<AudioSource>();
	}
	
	// Update is called once per frame
	void Update () 
	{
		switch(repeatMode)
		{
			case AudioRepeating.PlayOnce:
				if(		selectable != null && selectable.isSelected 
					&&	audio != null && !audio.isPlaying && !playedOnce )
				{
					audio.Play();
					playedOnce = true;
				}
				break;
			case AudioRepeating.RepeatWhileHolding:
				if(		selectable != null && selectable.isSelected 
					&&	audio != null && !audio.isPlaying			)
				{
					audio.Play();
				}
				break;
			case AudioRepeating.PlayOnceAndNeverAgain:
				if(		selectable != null && selectable.isSelected 
					&&	audio != null && !audio.isPlaying && !playedOnce )
				{
					audio.Play();
					playedOnce = true;
				}
				break;
		}
		
		if(		repeatMode != AudioRepeating.PlayOnceAndNeverAgain 
			&&	selectable != null && !selectable.isSelected			)
			playedOnce = false;
	}
}
