using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySounds : MonoBehaviour
{
    AudioSource source;
    public AudioClip[] clips;
    [Range(0, 1)] public float clipVolume = 1f;

    // Start is called before the first frame update
    void Start()
    {
        source = gameObject.AddComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlaySound(int index)
    {
        source.clip = clips[index];
        source.pitch = 1 - 0.05f + Random.Range(-0.05f, 0.05f);
        source.volume = clipVolume;
        source.PlayOneShot(source.clip);
    }
}
