using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterControllerLogic : MonoBehaviour
{
    #region Variables (private)

    //* inspector serialized
    [SerializeField]
    private Animator animator;
    [SerializeField]
    private float directionDampTime = .25f;
    [SerializeField]
    private ThirdPersonCamera gamecam = null;
    [SerializeField]
    private float directionSpeed = 3.0f;
    [SerializeField]
    private float rotationDegreePerSecond = 120f;

    //* private global only
    private float speed = 0.0f;
    private float direction = 0.0f;
    private float horizontal = 0.0f;
    private float vertical = 0.0f;

    #endregion

    #region Properties (public)

    public Animator Animator
    {
        get
        {
            return this.animator;
        }
    }

    public float Speed { get => speed; set => speed = value; }

    public float LocomotionThreshold { get { return 0.2f; } }


    #endregion

    #region Unity event functions

    void Start()
    {
        animator = GetComponent<Animator>();

        if (animator.layerCount >= 2)
        {
            animator.SetLayerWeight(1, 1);
        }
    }

    void Update()
    {
        if (animator && gamecam.CamState != ThirdPersonCamera.CamStates.FirstPerson)
        {
            // pull values from controller/keyboard
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");

            speed = new Vector2(horizontal, vertical).sqrMagnitude;

            StickToWorldspace(this.transform, gamecam.transform, ref direction, ref speed);

            animator.SetFloat("Speed", speed);
            animator.SetFloat("Direction", direction, directionDampTime, Time.deltaTime);

            // Debug.Log("speed = " + "[" + speed + "] " + "horizontal = " + "[" + horizontal + "]");

        }
    }

    void FixedUpdate()
    {
        // rotate character model if stick is tilted right or left, but only if character is moving in that direction
        if (IsAnimatorState("Locomotion") && ((direction >= 0 && horizontal >= 0) || (direction < 0 && horizontal < 0)))
        {
            Vector3 rotationAmount = Vector3.Lerp(Vector3.zero, new Vector3(0f, rotationDegreePerSecond * (horizontal < 0f ? -1f : 1f), 0f), Mathf.Abs(horizontal));
            Quaternion deltaRotation = Quaternion.Euler(rotationAmount * Time.deltaTime);
            this.transform.rotation = (this.transform.rotation * deltaRotation);
        }

        // Debug.Log(IsAnimatorState("Locomotion"));
    }

    void OnAnimatorIK()
    {
        Animator.SetLookAtWeight(gamecam.LookWeight);
        Animator.SetLookAtPosition(gamecam.FirstPersonCamPos.XForm.position + gamecam.FirstPersonCamPos.XForm.forward);
    }


    #endregion

    #region Methods

    public void StickToWorldspace(Transform root, Transform camera, ref float directionOut, ref float speedOut)
    {
        Vector3 rootDirection = root.forward;

        Vector3 stickDirection = new Vector3(horizontal, 0, vertical);

        speedOut = stickDirection.sqrMagnitude;

        // Get camera rotation
        Vector3 CameraDirection = camera.forward;
        CameraDirection.y = 0.0f; // kill y
        Quaternion referentialShift = Quaternion.FromToRotation(Vector3.forward, CameraDirection);

        // convert joystick input in Worldspace coordinates
        Vector3 moveDirection = referentialShift * stickDirection;
        Vector3 axisSign = Vector3.Cross(moveDirection, rootDirection);

        Debug.DrawRay(new Vector3(root.position.x, root.position.y + 2f, root.position.z), moveDirection, Color.green);
        Debug.DrawRay(new Vector3(root.position.x, root.position.y + 2f, root.position.z), rootDirection, Color.magenta);
        Debug.DrawRay(new Vector3(root.position.x, root.position.y + 2f, root.position.z), axisSign, Color.red);
        // Debug.DrawRay(new Vector3(root.position.x, root.position.y + 2f, root.position.z), stickDirection, Color.blue);

        float angleRootToMove = Vector3.Angle(rootDirection, moveDirection) * (axisSign.y >= 0 ? -1f : 1f);

        angleRootToMove /= 180f;

        directionOut = angleRootToMove * directionSpeed;
    }

    public bool IsAnimatorState(string stateName)
    {
        return animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
    }

    #endregion
}
