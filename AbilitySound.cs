using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AbilitySound
{
	public enum SoundSelectMode
	{
		Sequential,
		Random
	}

	public float volume = 1f;

	public float pitch = 1f;

	public bool loop;

	public List<AudioClip> sounds;

	public AudioSource audioSource;

	private int nextSound = -1;

	public SoundSelectMode soundSelectMode;

	public bool IsValid()
	{
		if (sounds != null)
		{
			return sounds.Count > 0;
		}
		return false;
	}

	private void UpdateNextSound()
	{
		switch (soundSelectMode)
		{
		case SoundSelectMode.Sequential:
			nextSound = (nextSound + 1) % sounds.Count;
			break;
		case SoundSelectMode.Random:
			nextSound = UnityEngine.Random.Range(0, sounds.Count);
			break;
		}
	}

	public void Play(AudioSource audioSourceIn)
	{
		AudioSource audioSource = ((audioSourceIn != null) ? audioSourceIn : this.audioSource);
		if (sounds == null || !(audioSource != null))
		{
			return;
		}
		AudioClip clip = null;
		if (sounds.Count > 0)
		{
			if (nextSound < 0)
			{
				UpdateNextSound();
			}
			clip = sounds[nextSound];
			UpdateNextSound();
		}
		audioSource.clip = clip;
		audioSource.volume = volume;
		audioSource.pitch = pitch;
		audioSource.loop = loop;
		audioSource.Play();
	}
}
