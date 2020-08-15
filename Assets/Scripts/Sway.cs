using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Sway : MonoBehaviourPunCallbacks
{
    public float intensity;
    public float smooth;
    private Quaternion origin_rotation;
    private Transform player;
    Player playerScript;

    public bool isMine;
    float intensityOrigin;

    void Start()
    {
        origin_rotation = transform.localRotation;
    }
    
    void Update()
    {
        if (Pause.paused) return;
        
        UpdateSway();
    }

    private void UpdateSway()
    {
        //controls
        float t_x_mouse = Input.GetAxis("Mouse X");
        float t_y_mouse = Input.GetAxis("Mouse Y");

        if (!isMine)
        {
            t_x_mouse = 0;
            t_y_mouse = 0;
        }

        //calculate target rotation
        Quaternion t_x_adj = Quaternion.AngleAxis(-intensity * t_x_mouse, Vector3.up);
        Quaternion t_y_adj = Quaternion.AngleAxis(intensity * t_y_mouse, Vector3.right);
        Quaternion target_rotation = origin_rotation * t_x_adj * t_y_adj;

        //rotate towards target rotation
        transform.localRotation = Quaternion.Lerp(transform.localRotation, target_rotation, Time.deltaTime * smooth);
    }
}
