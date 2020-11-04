using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{

    //* inspector serialized
    #region Variables (private)
    [SerializeField]
    private float distanceAway;
    [SerializeField]
    private float distanceUp;
    [SerializeField]
    private Transform followXForm;
    //TODO for widescreen effect
    // [SerializeField]
    // private float widescreen = 0.2f;
    // [SerializeField]
    // private float targetingTime = 0.5f;


    //* smoothing and damping
    private Vector3 velocityCamSmooth = Vector3.zero;
    [SerializeField]
    private float camSmoothDampTime = 0.1f;

    //* private global only
    private Vector3 lookDir;
    private Vector3 targetPosition;
    //TODO create a widescreen effect
    // private BarsEffect barEffect;
    private camStates camState = camStates.Behind;

    #endregion

    #region Properties (poublic)

    public enum camStates
    {
        Behind,
        FirstPerson,
        Target,
        Free
    }

    #endregion

    #region Unity event functions

    void Start()
    {
        followXForm = GameObject.FindWithTag("Player").transform;
    }

    void LateUpdate()
    {
        Vector3 characterOffset = followXForm.position + new Vector3(0f, distanceUp, 0f);

        // determine camera state
        if (Input.GetAxis("Target") > 0.01f)
        {
            camState = camStates.Target;
        }
        else
        {
            camState = camStates.Behind;
        }

        // execute camera state
        switch (camState)
        {
            case camStates.Behind:
                // calculate direction from camera to player, kill y, and normalize to give a valid direction with unit magnitude
                lookDir = characterOffset - this.transform.position;
                lookDir.y = 0;
                lookDir.Normalize();
                Debug.DrawRay(this.transform.position, lookDir, Color.green);

                // setting the target position to be the correct offset from the target
                targetPosition = characterOffset + followXForm.up * distanceUp - lookDir * distanceAway;
                Debug.DrawLine(followXForm.position, targetPosition, Color.magenta);
                break;
            case camStates.Target:
                lookDir = followXForm.forward;


                break;
        }

        targetPosition = characterOffset + followXForm.up * distanceUp - lookDir * distanceAway;


        CompensateForWalls(characterOffset, ref targetPosition);

        smoothPosition(this.transform.position, targetPosition);

        // make sure the camera is looking the right way
        transform.LookAt(followXForm);
    }

    #endregion

    #region Methods

    private void smoothPosition(Vector3 fromPos, Vector3 toPos)
    {
        // making a smooth transition between camera's current position and the position it wants to be in
        this.transform.position = Vector3.SmoothDamp(fromPos, toPos, ref velocityCamSmooth, camSmoothDampTime);
    }

    private void CompensateForWalls(Vector3 fromObject, ref Vector3 toTarget)
    {
        Debug.DrawLine(fromObject, toTarget, Color.cyan);
        // compensate for walls between camera
        RaycastHit wallHit = new RaycastHit();
        if (Physics.Linecast(fromObject, toTarget, out wallHit))
        {
            Debug.DrawRay(wallHit.point, Vector3.left, Color.red);
            toTarget = new Vector3(wallHit.point.x, toTarget.y, wallHit.point.z);
        }
    }

    #endregion
}
