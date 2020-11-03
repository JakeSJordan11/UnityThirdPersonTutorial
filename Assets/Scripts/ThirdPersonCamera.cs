using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    #region Variables (private)

    [SerializeField]
    private float distanceAway = 0;
    [SerializeField]
    private float distanceUp = 0;
    [SerializeField]
    private float smooth = 0;
    [SerializeField]
    private Transform follow;
    private Vector3 targetPosition;

    #endregion

    #region Unity event functions

    void Start()
    {
        follow = GameObject.FindWithTag("Player").transform;
    }

    void LateUpdate()
    {
        // setting the target position to be the correct offset from the target
        targetPosition = follow.position + follow.up * distanceUp - follow.forward * distanceAway;
        Debug.DrawRay(follow.position, Vector3.up * distanceUp, Color.red);
        Debug.DrawRay(follow.position, -1f * follow.forward * distanceAway, Color.blue);
        Debug.DrawLine(follow.position, targetPosition, Color.magenta);

        // making a smooth transition between it's current position and the position it wants to be in
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smooth);

        // make sure the camera is looking the right way
        transform.LookAt(follow);
    }

    #endregion
}
