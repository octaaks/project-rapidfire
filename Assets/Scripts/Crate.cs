using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Crate : MonoBehaviourPunCallbacks
{
    public Gun[] weapon;
    int i;

    private void FixedUpdate()
    {

    }

    [PunRPC]
    public void DestroyCrate()
    {
        gameObject.SetActive(false);
        //PhotonNetwork.Destroy(gameObject);
        //Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            i = Random.Range(0, weapon.Length);
            Weapon weaponController = collision.gameObject.GetComponent<Weapon>();
            weaponController.photonView.RPC("PickupWeapon", RpcTarget.All, weapon[i].name);
            if (photonView.IsMine)
            {
                photonView.RPC("DestroyCrate", RpcTarget.All);
            }
            weapon[i].Init();
        }
    }
}
