using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="New Gun", menuName = "Gun")]
public class Gun : ScriptableObject
{
    public string name;
    public GameObject prefab;
    public GameObject display;
    public int damage;
    public int pellets = 1;
    public float firerate;
    public float aimSpeed;
    public int ammo;
    public int clipsize;
    public float reloadTime;
    public int burst; // 0 semi // 1 auto // 2+ burst
    public bool aimable = true;
    public bool recovery;
    public bool isSniper = false;
    //public Vector3 gunPoint;
    [Header("Recoil Settings")]
    public float bloom;
    public float bloomWhenAim = 1f;
    public float kickback;
    public float recoil;
    public float camRecoil = 0.5f;
    public float spreadCooldown = 0.3f;
    public float spreadRatePerShot = 0.2f;
    public bool fixedRecoil = false;
    
    [Header("Other Settings")]
    [Range(0, 1)] public float mainFOV;
    [Range(0, 1)] public float weaponFOV;

    public AudioClip gunShotSound;

    [Range(0, 1)] public float shotVolume = 1;
    public float pitchRandomization;

    private int stash;
    private int clip;

    public void Init()
    {
        stash = ammo;
        clip = clipsize;
    }

    public bool FireBullet()
    {
        if (clip > 0)
        {
            clip -= 1;
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool AmmoEmpty()
    {
        if (stash <= 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool ClipFull()
    {
        if (clip >= clipsize)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Reload()
    {
        stash += clip;
        clip = Mathf.Min(clipsize, stash);
        stash -= clip;
    }

    public int GetStash()
    {
        return stash;
    }

    public int GetClip()
    {
        return clip;
    }
}
