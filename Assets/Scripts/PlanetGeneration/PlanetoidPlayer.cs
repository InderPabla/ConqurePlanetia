using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetoidPlayer : MonoBehaviour
{
    // Start is called before the first frame update

    private Rigidbody _RigPlayer;
    private WorldManager Manager;

    private bool Shift;
    private bool ShiftPre;

    private bool Z;

    private bool Up;
    private bool Down;
    private bool Left;
    private bool Right;
    private bool BankLeft;
    private bool BankRight;

    float ShiftDownCount = 0f;
    public AnimationCurve PowerControlCruve;
    void Start()
    {
        _RigPlayer = GetComponent<Rigidbody>();
        Manager = FindObjectOfType<WorldManager>();
    }

    void Update()
    {
        
    }

    void FixedUpdate()
    {
        CheckKeyPressed();

        Vector3 ForwardForce = transform.forward * 30f;
        Vector3 ViewForce = transform.forward * 5f;

        if (Shift)
        {
            //_RigPlayer.AddForceAtPosition(ForwardForce, transform.position, ForceMode.Force);
            if(!ShiftPre)
            {
                ShiftDownCount = 0;
                //_RigPlayer.velocity = Vector3.zero;
                //_RigPlayer.angularVelocity = Vector3.zero;
            }

            float div = ShiftDownCount / 2f;
           _RigPlayer.velocity += ViewForce * PowerControlCruve.Evaluate(div);
            ShiftDownCount += Time.deltaTime;
            // _RigPlayer.AddForceAtPosition(ForwardForce, transform.position, ForceMode.Force);

            Debug.DrawLine(transform.position, ((-ViewForce) + transform.position), Color.yellow);
        }
        else if(Z)
        {
            _RigPlayer.velocity *= 0.95f;
        }

        if (Up || Down || Left || Right || BankLeft || BankRight)
        {
            if (Up)
            {
                _RigPlayer.angularVelocity = transform.right;
                Debug.DrawLine(transform.position, ((transform.right) + transform.position), Color.yellow);
            }
            else if (Down)
            {
                _RigPlayer.angularVelocity = -transform.right;
                Debug.DrawLine(transform.position, ((-transform.right) + transform.position), Color.yellow);
            }

            if (BankRight)
            {
                _RigPlayer.angularVelocity = transform.up;
                Debug.DrawLine(transform.position, ((transform.up) + transform.position), Color.yellow);
            }
            else if (BankLeft)
            {
                _RigPlayer.angularVelocity = -transform.up;
                Debug.DrawLine(transform.position, ((-transform.up) + transform.position), Color.yellow);
            }

            if (Right)
            {
                _RigPlayer.angularVelocity = -transform.forward;
                Debug.DrawLine(transform.position, ((-transform.forward) + transform.position), Color.yellow);
            }
            else if (Left)
            {
                _RigPlayer.angularVelocity = transform.forward;
                Debug.DrawLine(transform.position, ((transform.forward) + transform.position), Color.yellow);
            }
        }
        else
        {
           _RigPlayer.angularVelocity = Vector3.zero;
        }
    }

    public bool ApplyGravity
    {
        get {
            return !Shift;
        }
    }

    private void CheckKeyPressed()
    {
        ShiftPre = Shift;

        if (Input.GetKey(KeyCode.LeftShift)) Shift = true;
        else Shift = false;

        if (Input.GetKey(KeyCode.W)) Up = true;
        else Up = false;

        if (Input.GetKey(KeyCode.S)) Down = true;
        else Down = false;

        if (Input.GetKey(KeyCode.A)) Left = true;
        else Left = false;

        if (Input.GetKey(KeyCode.D)) Right = true;
        else Right = false;

        if (Input.GetKey(KeyCode.Q)) BankLeft = true;
        else BankLeft = false;

        if (Input.GetKey(KeyCode.E)) BankRight = true;
        else BankRight = false;

        if (Input.GetKey(KeyCode.Z)) Z = true;
        else Z = false;
    }

    public Rigidbody RigPlayer
    {
        get {
            return _RigPlayer;
        }
    }
}
