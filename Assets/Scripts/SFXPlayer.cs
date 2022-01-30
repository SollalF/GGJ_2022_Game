using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXPlayer : MonoBehaviour
{
    [SerializeField] AudioClip[] jumpSFX;
    [SerializeField] AudioClip[] dashSFX;
    [SerializeField] AudioClip[] fallSFX;
    [SerializeField] AudioClip[] dieSFX;
    [SerializeField] AudioClip[] spawnSFX;
    [SerializeField] AudioClip[] switchSFX;
    [SerializeField] AudioClip runSFX;

    // Cache
    AudioSource myAudioSource;

    private void Start()
    {
        myAudioSource = GetComponent<AudioSource>();
        myAudioSource.volume = GameState.masterVolume;
    }

    // TODO find a better way than this
    public void PlayJumpSFX()
    {
        if (jumpSFX.Length != 0)
        {
            myAudioSource.PlayOneShot(jumpSFX[Random.Range(0, jumpSFX.Length - 1)], GameState.masterVolume);
        }
    }
    public void PlayDashSFX()
    {
        if (dashSFX.Length != 0)
        {
            myAudioSource.PlayOneShot(dashSFX[Random.Range(0, dashSFX.Length - 1)], GameState.masterVolume);
        }
    }
    public void PlayFallSFX()
    {
        if (fallSFX.Length != 0)
        {
            myAudioSource.PlayOneShot(fallSFX[Random.Range(0, fallSFX.Length - 1)], GameState.masterVolume);
        }
    }
    public void PlayDieSFX()
    {
        if (dieSFX.Length != 0)
        {
            myAudioSource.PlayOneShot(dieSFX[Random.Range(0, dieSFX.Length - 1)], GameState.masterVolume);
        }
    }public void PlaySpawnSFX()
    {
        if (spawnSFX.Length != 0)
        {
            myAudioSource.PlayOneShot(spawnSFX[Random.Range(0, spawnSFX.Length - 1)], GameState.masterVolume);
        }
    }
    public void PlaySwitchSFX()
    {
        if (switchSFX.Length != 0)
        {
            myAudioSource.PlayOneShot(switchSFX[Random.Range(0, switchSFX.Length - 1)], GameState.masterVolume);
        }
    }
    public void PlayRunSFX()
    {
        myAudioSource.loop = true;
        myAudioSource.clip = runSFX;
        myAudioSource.Play();
    }
    public void StopRunSFX()
    {
        myAudioSource.clip = runSFX;
        myAudioSource.Stop();
    }
}
