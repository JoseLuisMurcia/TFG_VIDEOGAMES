using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class Pedestrian : MonoBehaviour
{

    public Camera cam;
    public NavMeshAgent agent;
    float turnSpeed = 0.1f;

	[Header("Player")]
	[Tooltip("Move speed of the character in m/s")]
	public float MoveSpeed = 2.0f;
	[Tooltip("Sprint speed of the character in m/s")]
	public float SprintSpeed = 5.335f;
	[Tooltip("How fast the character turns to face movement direction")]
	[Range(0.0f, 0.3f)]
	public float RotationSmoothTime = 0.12f;
	[Tooltip("Acceleration and deceleration")]
	public float SpeedChangeRate = 10.0f;

	// cinemachine
	private float _cinemachineTargetYaw;
	private float _cinemachineTargetPitch;

	// player
	private float _speed;
	private float _animationBlend;
	private float _targetRotation = 0.0f;
	private float rotationVelocity = 2f;
	private float _terminalVelocity = 53.0f;

	// timeout deltatime
	private float _jumpTimeoutDelta;
	private float _fallTimeoutDelta;

	private Animator animator;

	private const float _threshold = 0.01f;

	private bool _hasAnimator;

	// Start is called before the first frame update
	void Start()
    {
        cam = Camera.main;
        agent = GetComponent<NavMeshAgent>();
		animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (UnityEngine.Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(UnityEngine.Input.mousePosition);
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit))
            {
				agent.SetDestination(hit.point);
			}
        }

        if (agent.remainingDistance > agent.stoppingDistance)
        {
            animator.SetBool("IsMoving", true);
        }
        else
        {
            animator.SetBool("IsMoving", false);
        }

    }

    //private void Move()
    //{
    //    // set target speed based on move speed, sprint speed and if sprint is pressed
    //    float targetSpeed = 3f;

    //    // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

    //    // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
    //    // if there is no input, set the target speed to 0
    //    if (_input.move == Vector2.zero) targetSpeed = 0.0f;

    //    // a reference to the players current horizontal velocity
    //    float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

    //    float speedOffset = 0.1f;
    //    float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

    //    // accelerate or decelerate to target speed
    //    if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
    //    {
    //        // creates curved result rather than a linear one giving a more organic speed change
    //        // note T in Lerp is clamped, so we don't need to clamp our speed
    //        _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

    //        // round speed to 3 decimal places
    //        _speed = Mathf.Round(_speed * 1000f) / 1000f;
    //    }
    //    else
    //    {
    //        _speed = targetSpeed;
    //    }
    //    _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);

    //    // normalise input direction
    //    Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

    //    // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
    //    // if there is a move input rotate player when the player is moving
    //    if (_input.move != Vector2.zero)
    //    {
    //        _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + cam.transform.eulerAngles.y;
    //        float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

    //        // rotate to face input direction relative to camera position
    //        transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
    //    }


    //    Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

    //    // move the player
    //    controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

    //    // update animator if using character
    //    if (_hasAnimator)
    //    {
    //        _animator.SetFloat(_animIDSpeed, _animationBlend);
    //        _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
    //    }
    //}
}
