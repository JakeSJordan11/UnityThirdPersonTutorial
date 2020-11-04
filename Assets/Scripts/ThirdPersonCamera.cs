using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct CameraPosition
{
    // position to align camera to, probably somewhere behind the character
    // or position to point camera at, probably sonwhere along character's axis
    private Vector3 position;
    // transform used for any rotation
    private Transform xForm;

    public Vector3 Position
    {
        get { return position; }
        set { position = value; }
    }
    public Transform XForm
    {
        get { return xForm; }
        set { xForm = value; }
    }

    public void Init(string camName, Vector3 pos, Transform transform, Transform parent)
    {
        position = pos;
        xForm = transform;
        xForm.name = camName;
        xForm.parent = parent;
        xForm.localPosition = Vector3.zero;
        xForm.localPosition = position;

    }
}

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
    [SerializeField]
    private float firstPersonThreshold = 0.5f;
    [SerializeField]
    private CharacterControllerLogic follow;


    //* smoothing and damping
    private Vector3 velocityCamSmooth = Vector3.zero;
    [SerializeField]
    private float camSmoothDampTime = 0.1f;

    //* private global only
    private Vector3 lookDir;
    private Vector3 targetPosition;
    private CamStates camState = CamStates.Behind;
    private CameraPosition firstPersonCamPos;
    private float xAxisRot = 0.0f;
    private float lookWeight;
    private const float TARGETING_THRESHOLD = 0.01f;

    public CharacterControllerLogic Follow { get => follow; set => follow = value; }
    public CharacterControllerLogic Follow1 { get => follow; set => follow = value; }

    #endregion

    #region Properties (poublic)

    public enum CamStates
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

        Follow = GameObject.FindWithTag("Player").GetComponent<CharacterControllerLogic>();

        followXForm = GameObject.FindWithTag("Player").transform;

        // position and parent a GameObject where firt person view should be
        firstPersonCamPos = new CameraPosition();
        firstPersonCamPos.Init("First Person Camera", new Vector3(0.0f, 1.6f, 0.2f), new GameObject().transform, followXForm);

    }

    void LateUpdate()
    {
        // pull values from controller/keyboard
        float rightX = Input.GetAxis("RightStickX");
        float rightY = Input.GetAxis("RightStickY");
        float leftX = Input.GetAxis("Horizontal");
        float leftY = Input.GetAxis("Vertical");
        Vector3 characterOffset = followXForm.position + new Vector3(0f, distanceUp, 0f);

        // determine camera state
        if (Input.GetAxis("Target") > TARGETING_THRESHOLD)
        {
            camState = CamStates.Target;
        }
        else
        {
            // first person
            if (rightY > firstPersonThreshold & !follow.IsAnimatorState("Locomotion"))
            {
                // reset look before entering the first person mode
                xAxisRot = 0;
                lookWeight = 0f;
                camState = CamStates.FirstPerson;
            }

            // behind the back
            if ((camState == CamStates.FirstPerson && Input.GetButton("ExitFPV")) ||
            (camState == CamStates.Target && (Input.GetAxis("Target") <= TARGETING_THRESHOLD)))
            {
                camState = CamStates.Behind;
            }

        }

        // execute camera state
        switch (camState)
        {
            case CamStates.Behind:
                // calculate direction from camera to player, kill y, and normalize to give a valid direction with unit magnitude
                lookDir = characterOffset - this.transform.position;
                lookDir.y = 0;
                lookDir.Normalize();
                Debug.DrawRay(this.transform.position, lookDir, Color.green);

                // setting the target position to be the correct offset from the target
                targetPosition = characterOffset + followXForm.up * distanceUp - lookDir * distanceAway;
                Debug.DrawLine(followXForm.position, targetPosition, Color.magenta);
                break;
            case CamStates.Target:
                lookDir = followXForm.forward;

                break;
            case CamStates.FirstPerson:
                Debug.Log("in first person", this);
                break;
        }

        targetPosition = characterOffset + followXForm.up * distanceUp - lookDir * distanceAway;


        CompensateForWalls(characterOffset, ref targetPosition);

        smoothPosition(this.transform.position, targetPosition);

        // make sure the camera is looking the right way
        transform.LookAt(followXForm);

        Debug.Log(camState);
        // Debug.Log(rightY);
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
