using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class Player : MonoBehaviourPunCallbacks, IPunObservable
{
    #region var

    public float speed;
    public float sprintModifier;
    public float slideModifier;
    public float crouchModifier;
    public float jumpForce;
    public float jetForce;
    public float jetWait;
    public float jetRecovery;
    public bool canShoot;
    public Camera normalCam;
    public Camera weaponCam;
    public GameObject cameraParent;
    public int maxHealth;
    public int maxFuel;
    public float lengthOfSlide;

    public float slideAmount;
    public float crouchAmount;
    public GameObject standingCollider;
    public GameObject crouchCollider;
    public GameObject mesh;
    public GameObject deadFX;

    public Transform weaponParent;
    public Transform gunHolder;

    public Transform groundDetector;
    public LayerMask ground;

    public AudioClip footstepClip;
    public AudioClip jumpClip;
    public AudioClip landingClip;
    public AudioSource source;
    public bool walkSoundPlayed = false;

    [HideInInspector] public ProfileData playerProfile;
    [HideInInspector] public bool awayTeam;
    public TextMeshPro playerUsername;
    public Renderer[] teamIndicators;

    private Rigidbody rig;

    private Vector3 targetWeaponBobPosition;
    private Vector3 weaponParentOrigin;
    private Vector3 weaponParentCurrentPos;

    private float movementCounter;
    private float idleCounter;

    private float baseFOV;
    private float sprintFOVModifier = 1.2f;
    private Vector3 origin;

    private int currentHealth;
    private float currentFuel;
    private float currentRecovery;
    private Manager manager;
    private Transform ui_healthbar;
    private Transform ui_fuelbar;
    private TextMeshProUGUI ui_ammo;
    private TextMeshProUGUI ui_username;
    private TextMeshProUGUI ui_team;

    private Weapon weapon;
    Quaternion gunHolderOrigin;

    private bool crouched;
    private bool sliding;
    public bool isAiming;
    private bool canJet;
    private float slideTime;
    private Vector3 slideDirection;
    private float aimAngle;
    private Vector3 normalCamTarget;
    private Vector3 weaponCamTarget;
    private Animator anim;

    #endregion

    public void OnPhotonSerializeView(PhotonStream p_stream, PhotonMessageInfo p_message)
    {
        if (p_stream.IsWriting)
        {
            p_stream.SendNext((int)(weaponParent.transform.localEulerAngles.x * 100f));
        }
        else
        {
            aimAngle = (int)p_stream.ReceiveNext() / 100f;
        }
    }


    #region Mono Callbacks

    // Start is called before the first frame update
    void Start()
    {
        manager = GameObject.Find("Manager").GetComponent<Manager>();
        currentHealth = maxHealth;
        weapon = GetComponent<Weapon>();
        currentFuel = maxFuel;

        cameraParent.SetActive(photonView.IsMine);

        if (!photonView.IsMine)
        {
            gameObject.layer = 12;
            standingCollider.layer = 12;
            crouchCollider.layer = 12;
            ChangeLayerRecursively(mesh.transform, 12);
        }

        if(Camera.main) Camera.main.enabled = false;

        rig = GetComponent<Rigidbody>();
        baseFOV = normalCam.fieldOfView;
        origin = normalCam.transform.localPosition;
        weaponParentOrigin = weaponParent.localPosition;
        weaponParentCurrentPos = weaponParentOrigin;

        if (photonView.IsMine)
        {

            //sprint gun holder
            gunHolderOrigin = gunHolder.localRotation;

            ui_ammo = GameObject.Find("HUD/Ammo/Text (TMP)").GetComponent<TextMeshProUGUI>();
            ui_username = GameObject.Find("HUD/Username/Text (TMP)").GetComponent<TextMeshProUGUI>();
            
            //ui_fuelbar = GameObject.Find("HUD/Fuel/Bar").transform;
            ui_healthbar = GameObject.Find("HUD/Health/HP").transform;
            ui_team = GameObject.Find("HUD/Team/Text (TMP)").GetComponent<TextMeshProUGUI>();

            RefreshHealtbar();
            anim = GetComponent<Animator>();

            ui_username.text = Launcher.myProfile.username;
            photonView.RPC("SyncProfile",RpcTarget.All, Launcher.myProfile.username, Launcher.myProfile.level, Launcher.myProfile.xp);

            if(GameSettings.gameMode == GameMode.TDM)
            {
                photonView.RPC("SyncTeam", RpcTarget.All, GameSettings.IsAwayTeam);

                if (GameSettings.IsAwayTeam)
                {
                    ui_team.text = "RED TEAM";
                    ui_team.color = Color.red;
                }
                else
                {
                    ui_team.text = "BLUE TEAM";
                    ui_team.color = Color.blue;
                }
            }
            else
            {
                ui_team.gameObject.SetActive(false);
            }

            anim = GetComponent<Animator>();
        }
    }

    public void ColorTeamIndicators(Color p_color)
    {
        foreach (Renderer renderer in teamIndicators)
        {
            renderer.enabled = true;
            renderer.material.color = p_color;
        }
    }

    private void ChangeLayerRecursively(Transform p_trans, int p_layer)
    {
        p_trans.gameObject.layer = p_layer;
        foreach (Transform t in p_trans) ChangeLayerRecursively(t, p_layer);
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            RefreshMultiplayerState();
            return;
        }

        //Axes
        float t_hmove = Input.GetAxisRaw("Horizontal");
        float t_vmove = Input.GetAxisRaw("Vertical");

        //Controls
        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool jump = Input.GetKeyDown(KeyCode.Space);
        bool crouch = Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl);
        bool pause = Input.GetKeyDown(KeyCode.Escape);

        //states 
        bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.15f, ground);
        bool isSprinting = sprint && t_vmove > 0;
        bool isJumping = jump && isGrounded;
        bool isCrouching = crouch && !isSprinting && !isJumping && isGrounded;

        //jump and landing sound
        
        bool airborne = false;
        if (isJumping && !airborne)
        {
            source.clip = jumpClip;
            source.pitch = 1 - 0.05f + Random.Range(-0.05f, 0.05f);
            source.volume = 0.3f;
            source.PlayOneShot(jumpClip);
            airborne = true;
        }

        //if (isGrounded && airborne)
        //{
        //    source.clip = landingClip;
        //    source.pitch = 1 - 0.05f + Random.Range(-0.05f, 0.05f);
        //    source.volume = 0.3f;
        //    source.PlayOneShot(landingClip);
        //    airborne = false;
        //}

        //Pause
        if (pause)
        {
            GameObject.Find("Pause").GetComponent<Pause>().TogglePause();
        }

        if (Pause.paused)
        {
            t_hmove = 0f;
            t_vmove = 0f;
            sprint = false;
            jump = false;
            crouch = false;
            pause = false;
            isGrounded = false;
            isJumping = false;
            isSprinting = false;
            isCrouching = false;
        }

        //crouch
        if (isCrouching)
        {
            photonView.RPC("SetCrouch", RpcTarget.All, !crouched);
        }

        //Jumping
        if (isJumping)
        {
            if (crouched)
            {
                photonView.RPC("SetCrouch", RpcTarget.All, false);
            }
            rig.AddForce(Vector3.up * jumpForce);
            currentRecovery = 0f;
        }

        //self damage
        if (Input.GetKey(KeyCode.U))
        {
            TakeDamage(100,-1);
        }

        //Headbob
        if (!isGrounded)
        {
            StopCoroutine(Footsteps(0.25f));

            //airborne
            HeadBob(idleCounter, 0.01f, 0.01f);
            idleCounter += 0;
            weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 2f * .2f);
        }
        else if (sliding) {

            StopCoroutine(Footsteps(0.25f));

            //sliding
            HeadBob(movementCounter, 0.15f, 0.075f);
            weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 10f * .2f);
        }
        else if(t_hmove == 0 && t_vmove == 0)
        {
            StopCoroutine(Footsteps(0.25f));

            //idling
            HeadBob(idleCounter, 0.008f, 0.008f);
            idleCounter += Time.deltaTime;
            weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 1.5f * .2f);
        }
        else if(!isSprinting && !crouched)
        {
            //walking sound footstep
            if (!walkSoundPlayed)
            {
                StartCoroutine(Footsteps(0.35f));
            }

            //walking
            HeadBob(movementCounter, 0.035f, 0.035f);
            movementCounter += Time.deltaTime * 6f;
            weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f * .2f);
        }
        else if(crouched)
        {
            //crouching
            HeadBob(movementCounter, 0.02f, 0.02f);
            movementCounter += Time.deltaTime * 3f;
            weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f * .2f);
        }
        else
        {
            //run sound footstep
            if (!walkSoundPlayed)
            {
                StartCoroutine(Footsteps(0.2f));
            }

            //sprinting
            HeadBob(movementCounter, 0.15f, 0.055f);
            movementCounter += Time.deltaTime * 9f;
            weaponParent.localPosition = Vector3.MoveTowards(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 10f * .2f);
        }

        //UI
        RefreshHealtbar();
        weapon.RefreshAmmo(ui_ammo);
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine) return;


        //Axes
        float t_hmove = Input.GetAxisRaw("Horizontal");
        float t_vmove = Input.GetAxisRaw("Vertical");

        //Controls
        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool jump = Input.GetKeyDown(KeyCode.Space);
        bool slide = Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl);
        bool aim = Input.GetMouseButton(1); // && !weapon.isReloading; // my fix
        //bool jet = Input.GetKey(KeyCode.Space);

        //states 
        bool isGrounded = Physics.Raycast(groundDetector.position, Vector3.down, 0.1f, ground);
        bool isSprinting = sprint && t_vmove > 0 && isGrounded;
        bool isJumping = jump && isGrounded;
        bool isSliding = isSprinting && slide &&!sliding;
        isAiming = aim && !isSliding && !isSprinting;
        
        //Pause
        if (Pause.paused)
        {
            t_hmove = 0f;
            t_vmove = 0f;
            sprint = false;
            jump = false;
            isGrounded = false;
            isJumping = false;
            isSprinting = false;
            isSliding = false;
            isAiming = false;
        }

        //sprint anim
        Quaternion sprintHolder = weaponParent.transform.Find("WeaponHolderRun").transform.localRotation;
        
        if (isSprinting)
        {
            canShoot = false;

            if (weapon.isReloading)
            {
                gunHolder.localRotation = Quaternion.Lerp(gunHolder.localRotation, gunHolderOrigin, Time.deltaTime * 17.5f);
            }
            else
            {
                gunHolder.localRotation = Quaternion.Lerp(gunHolder.localRotation, sprintHolder, Time.deltaTime * 10f);
            }
        }
        else
        {
            gunHolder.localRotation = Quaternion.Lerp(gunHolder.localRotation, gunHolderOrigin, Time.deltaTime * 17.5f);
            if(gunHolder.localRotation == gunHolderOrigin) canShoot = true;
        }

        //Movement
        Vector3 t_direction = Vector3.zero;
        float t_adjustedSpeed = speed;

        if (!sliding)
        {
            t_direction = new Vector3(t_hmove, 0, t_vmove);
            t_direction.Normalize();
            t_direction = transform.TransformDirection(t_direction);

            if (isSprinting)
            {
                if (crouched)
                {
                    photonView.RPC("SetCrouch", RpcTarget.All, false);
                }
                t_adjustedSpeed *= sprintModifier;
            }
            else if (crouched)
            {
                t_adjustedSpeed *= crouchModifier;
            }

        }
        else
        {
            t_direction = slideDirection;
            t_adjustedSpeed *= slideModifier;
            slideTime -= Time.deltaTime;

            if (slideTime <= 0)
            {
                sliding = false;
                weaponParentCurrentPos -= Vector3.down * (slideAmount - crouchAmount);
            }
        }

        Vector3 t_targetVelocity = t_direction * t_adjustedSpeed * Time.deltaTime;
        t_targetVelocity.y = rig.velocity.y;
        rig.velocity = t_targetVelocity;
        
        //sliding
        if (isSliding)
        {
            sliding = true;
            slideDirection = (t_direction);
            slideTime = lengthOfSlide;

            //adjust camera
            weaponParentCurrentPos += Vector3.down * (slideAmount - crouchAmount);

            if (!crouched)
            {
                photonView.RPC("SetCrouch", RpcTarget.All, true);
            }
        }

        //Jetting
        //if(jump && !isGrounded)
        //{
        //    canJet = true;
        //}

        //if (isGrounded)
        //{
        //    canJet = false;
        //}

        //if (canJet && jet && currentFuel > 0)
        //{
        //    rig.AddForce(Vector3.up * jetForce * Time.fixedDeltaTime, ForceMode.Acceleration);
        //    currentFuel = Mathf.Max(0, currentFuel - Time.fixedDeltaTime);
        //}

        //if (isGrounded)
        //{
        //    if(currentRecovery  < jetWait){
        //        currentRecovery = Mathf.Min(jetWait, currentRecovery + Time.fixedDeltaTime);
        //    }
        //    else
        //    {
        //        currentFuel = Mathf.Min(maxFuel, currentFuel + Time.fixedDeltaTime * jetRecovery);
        //    }
        //}

        //ui_fuelbar.localScale = new Vector3(currentFuel / maxFuel, 1, 1);
        
        //Aiming
        if (weapon.currentGunData != null)
        {
            if (weapon.currentGunData.isSniper)
            {
                isAiming = weapon.Aim(isAiming) && weapon.currentCooldown <= 0;
            }
            else
            {
                isAiming = weapon.Aim(isAiming);
            }
        }

        //if (isAiming && weapon.currentCooldown <= 0)
        //{
        //    weapon.Invoke("SniperScope", .2f);
        //}
        //else
        //{
        //    isAiming = false;
        //    weapon.SniperScopeQuit();
        //}

        //FOV
        if (sliding)
        {
            normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier * 1.25f, Time.deltaTime * 8f);
            //normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin + Vector3.down * slideAmount, Time.deltaTime * 6f);
            weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV * sprintFOVModifier * 1.25f, Time.deltaTime * 8f);
            //weaponCam.transform.localPosition = Vector3.Lerp(weaponCam.transform.localPosition, origin + Vector3.down * slideAmount, Time.deltaTime * 6f);

            normalCamTarget = Vector3.MoveTowards(normalCam.transform.localPosition, origin + Vector3.down * slideAmount, Time.deltaTime);
            weaponCamTarget = Vector3.MoveTowards(weaponCam.transform.localPosition, origin + Vector3.down * slideAmount, Time.deltaTime);
        }
        else
        {
            if (isSprinting)
            {
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * sprintFOVModifier, Time.deltaTime * 8f);
                weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV * sprintFOVModifier, Time.deltaTime * 8f);
            }
            else if (isAiming)
            {
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV * weapon.currentGunData.mainFOV, Time.deltaTime * 12f);
                weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV * weapon.currentGunData.weaponFOV, Time.deltaTime * 12f);
            }
            else
            {
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV, Time.deltaTime * 8f);
                weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, baseFOV, Time.deltaTime * 8f);
            }


            if (crouched)
            {
                normalCamTarget = Vector3.MoveTowards(normalCam.transform.localPosition, origin + Vector3.down * crouchAmount, Time.deltaTime * 2f);
                weaponCamTarget = Vector3.MoveTowards(weaponCam.transform.localPosition, origin + Vector3.down * crouchAmount, Time.deltaTime * 2f);
            }
            else
            {
                normalCamTarget = Vector3.MoveTowards(normalCam.transform.localPosition, origin, Time.deltaTime * 2f);
                weaponCamTarget = Vector3.MoveTowards(weaponCam.transform.localPosition, origin, Time.deltaTime * 2f);
            }
        }

        //animation
        float t_anim_horizontal = 0f;
        float t_anim_vertical = 0f;

        if (isGrounded)
        {
            t_anim_horizontal = t_hmove;
            t_anim_vertical = t_vmove;
        }

        anim.SetFloat("horizontal", t_anim_horizontal);
        anim.SetFloat("vertical", t_anim_vertical);

    }

    private void LateUpdate()
    {
        normalCam.transform.localPosition = normalCamTarget;
        weaponCam.transform.localPosition = weaponCamTarget;
    }

    #endregion

    void RefreshMultiplayerState()
    {
        float cacheEulY = weaponParent.localEulerAngles.y;

        Quaternion targetRotation = Quaternion.identity * Quaternion.AngleAxis(aimAngle, Vector3.right);
        weaponParent.rotation = Quaternion.Slerp(weaponParent.rotation, targetRotation, Time.deltaTime * 8f);

        Vector3 finalRotation = weaponParent.localEulerAngles;
        finalRotation.y = cacheEulY;

        weaponParent.localEulerAngles = finalRotation;
    }

    void HeadBob(float p_z, float p_x_intensity, float p_y_intensity)
    {
        float t_aim_adjust = 1f;
        if (isAiming)
        {
            t_aim_adjust = 0.05f;
        }
        targetWeaponBobPosition = weaponParentCurrentPos + new Vector3(Mathf.Cos(p_z) * p_y_intensity*t_aim_adjust, Mathf.Sin(p_z*2)*p_y_intensity * t_aim_adjust, 0);
    }

    void RefreshHealtbar()
    {
        float t_health_ratio = (float)currentHealth / (float)maxHealth;
        ui_healthbar.localScale = Vector3.Lerp(ui_healthbar.localScale, new Vector3(t_health_ratio,1,1),Time.deltaTime*6f);

    }

    public void TrySync()
    {
        if (!photonView.IsMine) return;

        photonView.RPC("SyncProfile", RpcTarget.All, Launcher.myProfile.username, Launcher.myProfile.level, Launcher.myProfile.xp);

        if(GameSettings.gameMode == GameMode.TDM)
        {
            photonView.RPC("SyncTeam", RpcTarget.All, GameSettings.IsAwayTeam);
        }
    }

    [PunRPC]
    private void ShowDeadFX()
    {
        Instantiate(deadFX, normalCam.transform.position, Quaternion.identity);
    }

    [PunRPC]
    private void SyncProfile(string p_username, int p_level, int p_xp)
    {
        playerProfile = new ProfileData(p_username, p_level, p_xp);
        playerUsername.text = playerProfile.username;
    }

    [PunRPC]
    private void SyncTeam(bool p_awayTeam)
    {
        awayTeam = p_awayTeam;

        if (awayTeam){
            ColorTeamIndicators(Color.red);
        }
        else
        {
            ColorTeamIndicators(Color.blue);
        }
    }

    [PunRPC]
    void SetCrouch(bool p_state)
    {
        if (crouched == p_state) return;

        crouched = p_state;

        if (crouched)
        {
            standingCollider.SetActive(false);
            crouchCollider.SetActive(true);
            weaponParentCurrentPos += Vector3.down * crouchAmount;
            //weaponParentCurrentPos = Vector3.MoveTowards(weaponParentCurrentPos, weaponParentCurrentPos + Vector3.down * crouchAmount, Time.deltaTime * 99);
        }
        else
        {
            standingCollider.SetActive(true);
            crouchCollider.SetActive(false);
            weaponParentCurrentPos -= Vector3.down * crouchAmount;
            //weaponParentCurrentPos = Vector3.MoveTowards(weaponParentCurrentPos, weaponParentCurrentPos - Vector3.down * crouchAmount, Time.deltaTime * 99);
        }
    }

    public void TakeDamage(int p_damage, int p_actor)
    {
        if (photonView.IsMine)
        {
            currentHealth -= p_damage;
            RefreshHealtbar();


            if(currentHealth <= 0)
            {
                photonView.RPC("ShowDeadFX", RpcTarget.All);
                manager.Spawn();
                manager.ChangeStat_S(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);

                if(p_actor >= 0)
                {
                    manager.ChangeStat_S(p_actor, 0, 1);
                }
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Destroyer")
        {
            TakeDamage(101, -1);
        }
    }
    
    IEnumerator Footsteps(float delay)
    {
        walkSoundPlayed = true;
        source.clip = footstepClip;
        source.pitch = 1 - 0.05f + Random.Range(-0.05f, 0.05f);
        source.volume = 0.3f;
        source.PlayOneShot(footstepClip);
        yield return new WaitForSeconds(delay);

        walkSoundPlayed = false;
        StopCoroutine(Footsteps(delay));
    }
}
