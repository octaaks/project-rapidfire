using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class Weapon : MonoBehaviourPunCallbacks
{
    public List<Gun> loadout;
    [HideInInspector] public Gun currentGunData;
    public Transform weaponParent;
    public Transform gunPoint;
    public Transform gunHolder;
    public GameObject gunCam;
    public GameObject mainCam;
    public GameObject camHolder;

    public GameObject bulletholePrefab;
    public GameObject bloodPrefab;

    public LayerMask canBeShot;
    public LayerMask playerCanBeShot;
    public LayerMask pickupLayer;
    public bool isAiming = false;
    public AudioSource sfx;
    public AudioClip hitmarkerSound;

    public LineRenderer bulletTrail;
    public float pickupDistance;

    private TextMeshProUGUI equipText;
    private Image hitmarkerImage;
    private Image crosshairSpread;
    private Image sniperScope;
    public float currentCooldown;
    private float hitmarkerwait;

    public bool isReloading; // my fix
    private int currentIndex;
    private GameObject currentWeapon;
    private Color clearwhite = new Color(1, 1, 1, 0);
    private Transform weaponObject;

    private bool canGrab = false;
    private Quaternion camHolderOriginRotation;
    

    int equippedWeapon = 1;
    float delayAfterEquip = 0.6f;
    float spreadRate = 0f;

    private void Start()
    {
        foreach (Gun a in loadout) a.Init();
        hitmarkerImage = GameObject.Find("HUD/Hitmarker/Image").GetComponent<Image>();
        crosshairSpread = GameObject.Find("HUD/Crosshair/DotSpread").GetComponent<Image>();
        sniperScope = GameObject.Find("HUD/Crosshair/Sniper").GetComponent<Image>();
        equipText = GameObject.Find("HUD/InteractText/Equip").GetComponent<TextMeshProUGUI>();
        equipText.enabled = false;

        sniperScope.enabled = false;

        hitmarkerImage.color = clearwhite;

        Equip(0);

        if (photonView.IsMine)
        {
            camHolderOriginRotation = camHolder.transform.localRotation;
        }
    }

    void Update()
    {
        if (Pause.paused && photonView.IsMine) return;

        if (photonView.IsMine)
        {
            //Pickup weapon trigger
            Transform camCenter;
            camCenter = gunCam.transform;

            RaycastHit pickuphit = new RaycastHit();
            if (Physics.Raycast(camCenter.position, camCenter.transform.forward, out pickuphit, pickupDistance, pickupLayer))
            {
                equipText.enabled = true;
                Debug.DrawLine(camCenter.position, pickuphit.point, Color.green);
                Debug.Log("Weapon on sight!!!");
                if (Input.GetKeyDown(KeyCode.E))
                {
                    photonView.RPC("PickupWeapon", RpcTarget.All, pickuphit.transform.GetComponent<Pickup>().weapon.name);
                    pickuphit.transform.GetComponent<Pickup>().PickedUp();

                }
            }
            else
            {
                equipText.enabled = false;
            }
        }

        //reset cam recoil
        camHolder.transform.localRotation = Quaternion.Lerp(camHolder.transform.localRotation, camHolderOriginRotation, Time.deltaTime * 2f);
                
        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha1) && equippedWeapon!=1)//my fix
        {
            if(loadout[0] != null)
            {
                photonView.RPC("Equip", RpcTarget.All, 0);
                equippedWeapon = 1;
            }
        }

        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha2) && equippedWeapon != 2 && loadout.Count > 1)//my fix
        {
            if (loadout[1] != null)
            {
                photonView.RPC("Equip", RpcTarget.All, 1);
                equippedWeapon = 2;
            }
        }

        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Q) && loadout.Count > 1)
        {
            if (equippedWeapon == 1)
            {
                photonView.RPC("Equip", RpcTarget.All, 1);
                equippedWeapon = 2;
            }
            else
            {
                photonView.RPC("Equip", RpcTarget.All, 0);
                equippedWeapon = 1;
            }
        }

        if (currentWeapon != null)
        {
            if (photonView.IsMine)
            {
                //cant sprint while shoot
                Player playerScript = GetComponent<Player>();
                
                //burst
                if (loadout[currentIndex].burst != 1)
                {
                    if (Input.GetMouseButtonDown(0) && currentCooldown <= 0 && !isReloading && delayAfterEquip < 0 && playerScript.canShoot)
                    {

                        //checks if clip empty
                        if (loadout[currentIndex].FireBullet())
                        {
                            photonView.RPC("Shoot", RpcTarget.All);
                        }
                        else
                        {
                            //checks if all of the ammo empty
                            if (loadout[currentIndex].AmmoEmpty())
                            {
                                Debug.Log("ammo out");
                            }
                            else
                            {
                                photonView.RPC("ReloadRPC", RpcTarget.All);
                            }
                        }
                    }
                }
                else
                {
                    if (Input.GetMouseButton(0) && currentCooldown <= 0 && !isReloading && delayAfterEquip < 0 && playerScript.canShoot)
                    {
                        //checks if clip empty
                        if (loadout[currentIndex].FireBullet())
                        {
                            photonView.RPC("Shoot", RpcTarget.All);
                        }
                        else
                        {
                            //checks if all of the ammo empty
                            if (loadout[currentIndex].AmmoEmpty())
                            {
                                Debug.Log("ammo out");
                            }
                            else
                            {
                                photonView.RPC("ReloadRPC", RpcTarget.All);
                            }
                        }
                    }
                }

                //R to force reload
                if (Input.GetKeyDown(KeyCode.R) && !isReloading && !loadout[currentIndex].AmmoEmpty() && !loadout[currentIndex].ClipFull())
                {
                    photonView.RPC("ReloadRPC", RpcTarget.All);
                }

                //cooldown
                if (currentCooldown > 0)
                {
                    currentCooldown -= Time.deltaTime;
                }
            }

            //weapon position elasticity
            currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, Vector3.zero, Time.deltaTime * 4f);
        }

        if (photonView.IsMine)
        {
            if (hitmarkerwait > 0)
            {
                hitmarkerwait -= Time.deltaTime;
            }
            else if(hitmarkerImage.color.a > 0)
            {
                hitmarkerImage.color = Color.Lerp(hitmarkerImage.color, clearwhite, Time.deltaTime * 2f);
            }

            //crosshair

            if (loadout[currentIndex].isSniper)
            {
                GameObject.Find("HUD/Crosshair/Image").GetComponent<Image>().enabled = false;
                crosshairSpread.enabled = false;
            }
            else
            {
                GameObject.Find("HUD/Crosshair/Image").GetComponent<Image>().enabled = !isAiming;
                crosshairSpread.enabled = !isAiming;
            }

            // UI recoil
            if (loadout[currentIndex].fixedRecoil)
            {
                RectTransform rt = crosshairSpread.GetComponent(typeof(RectTransform)) as RectTransform;
                rt.sizeDelta = new Vector2(loadout[currentIndex].spreadRatePerShot * 75f, loadout[currentIndex].spreadRatePerShot * 75f);
            }
            else
            {
                RectTransform rt = crosshairSpread.GetComponent(typeof(RectTransform)) as RectTransform;
                rt.sizeDelta = new Vector2(spreadRate * 75f, spreadRate * 75f);
            }
        }
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine) return;
        delayAfterEquip -= Time.deltaTime;
        
        if (spreadRate > 0.01f)
        {
            spreadRate -= Time.deltaTime * loadout[currentIndex].spreadCooldown;
        }

        //UI HUD Weapon Icon

        GameObject primaryIconHolder = GameObject.Find("HUD/Weapon/Primary");
        GameObject secondaryIconHolder = GameObject.Find("HUD/Weapon/Secondary");

        if (loadout.Count >= 2)
        {
            primaryIconHolder.SetActive(true);
            secondaryIconHolder.SetActive(true);
        }
        else
        {
            primaryIconHolder.SetActive(true);
            secondaryIconHolder.SetActive(false);
        }
    }
    
    private void RefreshWeaponIcon(int index)
    {
        Transform primaryIconHolder = GameObject.Find("HUD/Weapon/Primary").transform;
        Transform secondaryIconHolder = GameObject.Find("HUD/Weapon/Secondary").transform;

        foreach (Transform icon in primaryIconHolder)
        {
            icon.gameObject.SetActive(false);
        }

        foreach (Transform icon in secondaryIconHolder)
        {
            icon.gameObject.SetActive(false);
        }
        
        GameObject.Find("HUD/Weapon/Primary/" + loadout[index].name).SetActive(true);
        if (loadout.Count >= 2)
        {
            for (int i = 0; i < 2; i++)
            {
                if (loadout[index].name != loadout[i].name)
                {
                    GameObject.Find("HUD/Weapon/Secondary/" + loadout[i].name).SetActive(true);
                }
            }
        }
    }

    [PunRPC]
    private void ReloadRPC()
    {
        StartCoroutine(Reload(loadout[currentIndex].reloadTime));
    }

    // sniper function
    public void SniperScope()
    {
        if (!loadout[currentIndex].isSniper) return;
        if (delayAfterEquip > 0) return;
        if (currentCooldown > 0) return;

        sniperScope.enabled = true;
        gunCam.SetActive(false);
    }

    public void SniperScopeQuit()
    {
        if (!loadout[currentIndex].isSniper) return;
        
        Player playerScript = GetComponent<Player>();
        playerScript.isAiming = false;
        sniperScope.enabled = false;
        gunCam.SetActive(true);
    }

    IEnumerator Reload(float p_wait)
    {
        SniperScopeQuit();
        isReloading = true;
        if (currentWeapon.GetComponent<Animator>())
        {
            currentWeapon.GetComponent<Animator>().Play("reload", 0, 0);
        }
        else
        {
            currentWeapon.SetActive(false);
        }
        //currentWeapon.SetActive(false);

        yield return new WaitForSeconds(p_wait);

        loadout[currentIndex].Reload();
        currentWeapon.SetActive(true);
        isReloading = false;
    }

    [PunRPC]
    void Equip(int p_ind)
    {
        if (photonView.IsMine)
        {
            RefreshWeaponIcon(p_ind);

            if (!loadout[currentIndex].isSniper)
            {
                GameObject.Find("HUD/Crosshair/Image").GetComponent<Image>().enabled = true;
            }

            Aim(false);
            SniperScopeQuit();
        }

        spreadRate = 0;
        delayAfterEquip = 0.6f;

        if(currentWeapon != null)
        {
            if (isReloading)
            {
                StopCoroutine("Reload");
            }

            Destroy(currentWeapon);
        }

        currentIndex = p_ind;

        GameObject t_newWeapon = Instantiate(loadout[p_ind].prefab, weaponParent.GetChild(0).position, weaponParent.GetChild(0).rotation, weaponParent.GetChild(0)) as GameObject;
        t_newWeapon.transform.localPosition = Vector3.zero;
        t_newWeapon.transform.localEulerAngles = Vector3.zero;
        t_newWeapon.GetComponent<Sway>().isMine = photonView.IsMine;

        if (photonView.IsMine)
        {
            ChangeLayerRecursively(t_newWeapon, 11);
        }
        else
        {
            ChangeLayerRecursively(t_newWeapon, 0);
        }

        t_newWeapon.GetComponent<Animator>().Play("equip", 0, 0);

        currentWeapon = t_newWeapon;
        currentGunData = loadout[p_ind];
    }

    [PunRPC]
    void PickupWeapon(string name)
    {
        //find weapon from library
        //add weapon to the loadout
        Gun newWeapon = GunLibrary.FindGun(name);

        if(loadout.Count >= 2)
        {
            if(loadout[0] != newWeapon && loadout[1] != newWeapon)
            {
                loadout[currentIndex] = newWeapon;
                loadout[currentIndex].Init();
                Equip(currentIndex);
                equippedWeapon = currentIndex + 1;
            }
            else
            {
                for (int i = 0; i < loadout.Count; i++)
                {
                    if (loadout[i] == newWeapon)
                    {
                        loadout[i].Init();
                        Equip(i);
                        equippedWeapon = i+1;
                    }
                }
            }
        }
        else
        {
            if (loadout[currentIndex] == newWeapon)
            {
                loadout[currentIndex].Init();
                Equip(currentIndex);
                equippedWeapon = 1;
            }
            else
            {
                loadout.Add(newWeapon);
                Equip(loadout.Count - 1);
                equippedWeapon = 2;
            }
        }
    }

    private void ChangeLayerRecursively(GameObject p_target, int p_layer)
    {
        p_target.layer = p_layer;
        foreach (Transform a in p_target.transform) ChangeLayerRecursively(a.gameObject, p_layer);
    }

    public bool Aim(bool p_isAiming)
    {
        if (!currentWeapon) return false;
        if (!loadout[currentIndex].aimable) return false;
        if (isReloading) p_isAiming = false;
        if (delayAfterEquip > 0) return false;
        if (loadout[currentIndex].isSniper && currentCooldown > 0) p_isAiming = false;

        isAiming = p_isAiming;
        Transform t_anchor = currentWeapon.transform.Find("Anchor");
        Transform t_state_ads = currentWeapon.transform.Find("States/ADS");
        Transform t_state_hip = currentWeapon.transform.Find("States/Hip");

        if (p_isAiming)
        {
            if (!loadout[currentIndex].isSniper)
            {
                SniperScopeQuit();
            }
            else
            {
                if (currentCooldown <= 0)
                {
                    Invoke("SniperScope", .2f);
                }
            }

            //aim ADS
            t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_ads.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
        }
        else
        {
            SniperScopeQuit();

            //hip 
            t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_hip.position, Time.deltaTime * loadout[currentIndex].aimSpeed);
        }

        return isAiming;
    }

    [PunRPC]
    void Shoot()
    {

        //spread rate
        if (spreadRate <= 1)
        {
            spreadRate += loadout[currentIndex].spreadRatePerShot;
        }

        Transform t_spawn;

        if (photonView.IsMine)
        {
             t_spawn = gunCam.transform;
        }
        else
        {
            t_spawn = gunHolder.GetChild(0).transform.Find("Anchor/Muzzle");
        }
        //cooldown
        currentCooldown = loadout[currentIndex].firerate;

        for (int i = 0; i < Mathf.Max(1, currentGunData.pellets); i++)
        {
            //bloom
            Vector3 t_bloom = t_spawn.position + t_spawn.forward * 1000f;

            if (isAiming)
            {
                t_bloom += Random.Range(0, loadout[currentIndex].bloomWhenAim * spreadRate) * t_spawn.up;
                t_bloom += Random.Range(-loadout[currentIndex].bloomWhenAim * spreadRate, loadout[currentIndex].bloomWhenAim * spreadRate) * t_spawn.right;
                t_bloom -= t_spawn.position;
                t_bloom.Normalize();
            }
            else
            {
                t_bloom += Random.Range(0, loadout[currentIndex].bloom * spreadRate) * t_spawn.up;
                t_bloom += Random.Range(-loadout[currentIndex].bloom * spreadRate, loadout[currentIndex].bloom * spreadRate) * t_spawn.right;
                t_bloom -= t_spawn.position;
                t_bloom.Normalize();
            }

            //cooldown
            currentCooldown = loadout[currentIndex].firerate;

            //raycast shoot player only
            RaycastHit t_hit_player = new RaycastHit();
            if (Physics.Raycast(t_spawn.position, t_bloom, out t_hit_player, 1000f, playerCanBeShot))
            {
                GameObject t_newBulletHole = Instantiate(bloodPrefab, t_hit_player.point + t_hit_player.normal * 0.001f, Quaternion.identity) as GameObject;
                t_newBulletHole.transform.LookAt(t_hit_player.point + t_hit_player.normal);
                Destroy(t_newBulletHole, 0.1f);

                if (photonView.IsMine)
                {
                    if(t_hit_player.collider.gameObject.layer == 12)
                    {
                        bool applyDamage = false;

                        if(GameSettings.gameMode == GameMode.FFA)
                        {
                            applyDamage = true;
                        }

                        if(GameSettings.gameMode == GameMode.TDM)
                        {
                            if(t_hit_player.collider.transform.root.gameObject.GetComponent<Player>().awayTeam != GameSettings.IsAwayTeam)
                            {
                                applyDamage = true;
                            }
                        }

                        if (applyDamage)
                        {
                            //give damage
                            t_hit_player.collider.transform.root.gameObject.GetPhotonView().RPC("TakeDamage", RpcTarget.All, loadout[currentIndex].damage, PhotonNetwork.LocalPlayer.ActorNumber);

                            //show hitmarker
                            hitmarkerImage.color = Color.white;
                            sfx.PlayOneShot(hitmarkerSound);
                            hitmarkerwait = 0.5f;
                        }
                    }
                }

                ////bullet trail FX

                weaponObject = gunHolder.transform.GetChild(0);
                Transform t_muzzlePosition = weaponObject.transform.Find("Anchor/Muzzle").transform;
                GameObject bulletTrailFX = Instantiate(bulletTrail.gameObject, t_muzzlePosition.position, Quaternion.identity);

                LineRenderer lineRenderer = bulletTrailFX.GetComponent<LineRenderer>();

                lineRenderer.SetPosition(0, t_muzzlePosition.position);
                lineRenderer.SetPosition(1, t_hit_player.point);

                Destroy(bulletTrailFX, 1f);
            }

            //raycast shoot other than player
            RaycastHit t_hit = new RaycastHit();
            if (Physics.Raycast(t_spawn.position, t_bloom, out t_hit, 1000f, canBeShot))
            {
                GameObject t_newBulletHole = Instantiate(bulletholePrefab, t_hit.point + t_hit.normal * 0.001f, Quaternion.identity) as GameObject;
                t_newBulletHole.transform.LookAt(t_hit.point + t_hit.normal);
                Destroy(t_newBulletHole, 15f);

                if (photonView.IsMine)
                {
                    //shooting target
                    if (t_hit.collider.transform.gameObject.layer == 13)
                    {
                        //show hitmarker
                        hitmarkerImage.color = Color.white;
                        sfx.PlayOneShot(hitmarkerSound);
                        hitmarkerwait = 0.5f;
                    }
                }

                ////bullet trail FX

                weaponObject = gunHolder.transform.GetChild(0);
                Transform t_muzzlePosition = weaponObject.transform.Find("Anchor/Muzzle").transform;
                GameObject bulletTrailFX = Instantiate(bulletTrail.gameObject, t_muzzlePosition.position, Quaternion.identity);

                LineRenderer lineRenderer = bulletTrailFX.GetComponent<LineRenderer>();

                lineRenderer.SetPosition(0, t_muzzlePosition.position);
                lineRenderer.SetPosition(1, t_hit.point);

                Destroy(bulletTrailFX, 1f);
            }

        }
        
        //sound
        sfx.clip = currentGunData.gunShotSound;
        sfx.pitch = 1 - currentGunData.pitchRandomization + Random.Range(-currentGunData.pitchRandomization, currentGunData.pitchRandomization);
        sfx.volume = currentGunData.shotVolume;
        sfx.PlayOneShot(sfx.clip);
                
        //gun fx
        weaponObject = gunHolder.transform.GetChild(0);
        weaponObject.transform.Find("Anchor/Muzzle").GetComponent<ParticleSystem>().Stop();
        weaponObject.transform.Find("Anchor/Muzzle").GetComponent<ParticleSystem>().Play();

        currentWeapon.transform.Rotate(-loadout[currentIndex].recoil, 0, 0);
        camHolder.transform.Rotate(-loadout[currentIndex].camRecoil, 0, 0);

        currentWeapon.transform.position -= currentWeapon.transform.forward*loadout[currentIndex].kickback;
        
        //sniper scope out
        if (loadout[currentIndex].isSniper)
        {
            SniperScopeQuit();
            isAiming = false;
        }

        if (currentGunData.recovery)
        {
            currentWeapon.GetComponent<Animator>().Play("recovery", 0, 0);
            Invoke("PlayRecoverySound", currentGunData.recoveryDelayTime);
        }
    }

    void PlayRecoverySound()
    {
        sfx.clip = currentGunData.recoverySound;
        sfx.pitch = 1 - currentGunData.pitchRandomization + Random.Range(-currentGunData.pitchRandomization, currentGunData.pitchRandomization);
        sfx.volume = currentGunData.shotVolume;
        sfx.PlayOneShot(sfx.clip);
    }
    
    [PunRPC]
    private void TakeDamage(int p_damage,int p_actor)
    {
        GetComponent<Player>().TakeDamage(p_damage, p_actor);
    }

    public void RefreshAmmo(TextMeshProUGUI p_text)
    {
        int t_clip = loadout[currentIndex].GetClip();
        int t_stash = loadout[currentIndex].GetStash();

        p_text.text = t_clip.ToString("D2") + " / " + t_stash.ToString("D2");
    }
}
