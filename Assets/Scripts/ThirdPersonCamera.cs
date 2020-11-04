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
    private float distanceAway = 0.0f;
    [SerializeField]
    private float distanceUp = 0.0f;
    [SerializeField]
    private Transform followXForm;
    [SerializeField]
    private float firstPersonThreshold = 0.5f;
    [SerializeField]
    private CharacterControllerLogic follow;
    [SerializeField]
    private float firstPersonLookSpeed = 1.5f;
    [SerializeField]
    private Vector2 firstPersonXAxisClamp = new Vector2(-70.0f, 90.0f);
    [SerializeField]
    private float fPSRotationDegreePerSecond = 120f;

    //* smoothing and damping
    private Vector3 velocityCamSmooth = Vector3.zero;
    [SerializeField]
    private float camSmoothDampTime = 0.1f;
    private Vector3 velocityLookDir = Vector3.zero;
    [SerializeField]
    private float lookDirDampTime = 0.1f;

    //* private global only
    private Vector3 lookDir;
    private Vector3 targetPosition;
    private CamStates camState = CamStates.Behind;
    private CameraPosition firstPersonCamPos;
    private float xAxisRot = 0.0f;
    private float lookWeight;
    private const float TARGETING_THRESHOLD = 0.01f;
    private Vector3 curLookDir;

    #endregion

    #region Properties (poublic)
    public CamStates CamState { get => camState; set => camState = value; }
    public float LookWeight { get => lookWeight; set => lookWeight = value; }
    internal CameraPosition FirstPersonCamPos { get => firstPersonCamPos; set => firstPersonCamPos = value; }

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

        follow = GameObject.FindWithTag("Player").GetComponent<CharacterControllerLogic>();
        followXForm = GameObject.FindWithTag("Player").transform;
        lookDir = followXForm.forward;
        curLookDir = followXForm.forward;

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
        Vector3 lookAt = characterOffset;

        // determine camera state
        if (Input.GetAxis("Target") > TARGETING_THRESHOLD)
        {
            CamState = CamStates.Target;
        }
        // first person
        if (rightY > firstPersonThreshold & !follow.IsAnimatorState("Locomotion"))
        {
            // reset look before entering the first person mode
            xAxisRot = 0f;
            lookWeight = 0f;
            CamState = CamStates.FirstPerson;
        }

        // behind the back
        if ((CamState == CamStates.FirstPerson && Input.GetButton("ExitFPV")) ||
        (CamState == CamStates.Target && (Input.GetAxis("Target") <= TARGETING_THRESHOLD)))
        {
            StartCoroutine(BehindTransition());
        }


        // // set the Look At Weight - amount to use look at IK vs using the heads animation
        //! moved to onAnimatorIK()
        // follow.Animator.SetLookAtWeight(lookWeight);

        // execute camera state
        switch (CamState)
        {
            case CamStates.Behind:
                ResetCamera();
                // calculate direction from camera to player, kill y, and normalize to give a valid direction with unit magnitude
                // lookDir = characterOffset - this.transform.position;
                // lookDir.y = 0;
                // lookDir.Normalize();
                // Debug.DrawRay(this.transform.position, lookDir, Color.green);

                // setting the target position to be the correct offset from the target
                // targetPosition = characterOffset + followXForm.up * distanceUp - lookDir * distanceAway;
                // Debug.DrawLine(followXForm.position, targetPosition, Color.magenta);

                // only update camera look direction if moving
                if (follow.Speed > follow.LocomotionThreshold && follow.IsAnimatorState("Locomotion"))
                {
                    lookDir = Vector3.Lerp(followXForm.right * (leftX < 0 ? 1f : -1f), followXForm.forward * (leftY < 0 ? -1f : 1f), Mathf.Abs(Vector3.Dot(this.transform.forward, followXForm.forward)));
                    Debug.DrawRay(this.transform.position, lookDir, Color.white);

                    // calculate direction from camera to player, kill y, and normalize to give a valid direction with unit magnitude
                    curLookDir = Vector3.Normalize(characterOffset - this.transform.position);
                    curLookDir.y = 0;
                    Debug.DrawRay(this.transform.position, curLookDir, Color.green);

                    // damping makes it so we don't update targetPosition while pivoting: camera shouldn't rotate around player
                    curLookDir = Vector3.SmoothDamp(curLookDir, lookDir, ref velocityLookDir, lookDirDampTime);
                }

                targetPosition = characterOffset + followXForm.up * distanceUp - Vector3.Normalize(curLookDir) * distanceAway;
                Debug.DrawLine(followXForm.position, targetPosition, Color.magenta);

                break;
            case CamStates.Target:
                ResetCamera();
                lookDir = followXForm.forward;
                curLookDir = followXForm.forward;
                targetPosition = characterOffset + followXForm.up * distanceUp - lookDir * distanceAway;
                break;
            case CamStates.FirstPerson:
                // looking up and down
                // calculate the amount of rotation and apply to the firstPersonCamPos GameObject
                xAxisRot += (leftY * firstPersonLookSpeed);
                xAxisRot = Mathf.Clamp(xAxisRot, firstPersonXAxisClamp.x, firstPersonXAxisClamp.y);
                firstPersonCamPos.XForm.localRotation = Quaternion.Euler(xAxisRot, 0, 0);

                // superimpose firstPersonCamPos GameObect's rotation on camera
                Quaternion rotationShift = Quaternion.FromToRotation(this.transform.forward, firstPersonCamPos.XForm.forward);
                this.transform.rotation = rotationShift * this.transform.rotation;

                // move character model's head
                //! moved to onAnimatorIK()
                // follow.Animator.SetLookAtPosition(firstPersonCamPos.XForm.position + firstPersonCamPos.XForm.forward);
                lookWeight = Mathf.Lerp(lookWeight, 1.0f, Time.deltaTime * firstPersonLookSpeed);

                // look left and right
                // similarly to how character is rotated while in locomotion, use Quaternion + add rotation to character
                Vector3 rotationAmount = Vector3.Lerp(Vector3.zero, new Vector3(0f, fPSRotationDegreePerSecond * (leftX < 0f ? -1f : 1f), 0f), Mathf.Abs(leftX));
                Quaternion deltaRotation = Quaternion.Euler(rotationAmount * Time.deltaTime);
                follow.transform.rotation = (follow.transform.rotation * deltaRotation);

                // move camera to firstPersonCamPos
                targetPosition = firstPersonCamPos.XForm.position;

                // smoothly transition look direction towards firstPersonCamPos when entering first person mode
                lookAt = Vector3.Lerp(targetPosition + followXForm.forward, this.transform.position + this.transform.forward, camSmoothDampTime * Time.deltaTime);
                Debug.DrawRay(Vector3.zero, lookAt, Color.black);
                Debug.DrawRay(Vector3.zero, targetPosition + followXForm.forward, Color.white);
                Debug.DrawRay(Vector3.zero, firstPersonCamPos.XForm.position + firstPersonCamPos.XForm.forward, Color.cyan);

                // choose lookat target based on distance
                lookAt = Vector3.Lerp(this.transform.position + this.transform.forward, lookAt, Vector3.Distance(this.transform.position, firstPersonCamPos.XForm.position));
                break;
        }



        CompensateForWalls(characterOffset, ref targetPosition);

        smoothPosition(this.transform.position, targetPosition);

        // make sure the camera is looking the right way
        transform.LookAt(lookAt);
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

    private void ResetCamera()
    {
        lookWeight = Mathf.Lerp(lookWeight, 0.0f, Time.deltaTime * firstPersonLookSpeed);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.identity, Time.deltaTime);
    }

    IEnumerator BehindTransition()
    {
        CamState = CamStates.Target;
        yield return new WaitForSeconds(0.1f);
        if (CamState == CamStates.Target)
        {
            yield return CamState = CamStates.Behind;
        }
    }


    #endregion
}
