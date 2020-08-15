using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class CrateSpawner : MonoBehaviourPunCallbacks
{
    public float cooldown = 10f;

    public Transform[] spawnPoint;
    public GameObject cratePrefab;

    private float currentCooldown;
    // Start is called before the first frame update
    void Start()
    {
        currentCooldown = cooldown;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {
        currentCooldown -= Time.deltaTime;

        if (currentCooldown <= 0)
        {
            SpawnCrate();
        }
    }
    
    public void SpawnCrate()
    {
        GameObject crate1 = Instantiate(cratePrefab, spawnPoint[0].position, transform.rotation);
        GameObject crate2 = Instantiate(cratePrefab, spawnPoint[1].position, transform.rotation);
        Destroy(crate1, cooldown);
        Destroy(crate2, cooldown);
        currentCooldown = cooldown;
    }
}
