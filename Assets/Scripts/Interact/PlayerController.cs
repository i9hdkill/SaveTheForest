﻿using System.Linq;
using Enums;
using Interact;
using Interact.Actions;
using Manager;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Player Settings")] public float speed = 0;
    public float pickUpRange;

    [SerializeField] private Grabable _carryingObject;

    public bool CanCarry => _carryingObject.Type == InteractableEnum.Hands;

    [SerializeField] private Transform _grabableHook;

    [SerializeField] private Grabable _hands;

    [Header("CameraRotation")] [SerializeField]
    private float _lookSensitivity = 12f;

    [SerializeField] public float CameraRotationLimit = 85f;

    private Vector3 _rotation = Vector3.zero;
    private float _cameraRotationX;
    private float _currentCameraRotationX;
    private Rigidbody _rb;
    [SerializeField] private float _multiplier = 2f;
    [SerializeField] private float _runSpeed = 5f;

    private static PlayerController _instance;

    public static PlayerController Instance => _instance;

    private void Awake()
    {
        _instance = this;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            ShootRay();

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Interact();
        }

        var finalMulti = 1f;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            finalMulti = _multiplier;
        }
        if (Input.GetKey("w")) transform.Translate(Vector3.forward * (Time.deltaTime * _runSpeed * finalMulti));
        if (Input.GetKey("s")) transform.Translate(Vector3.back * (Time.deltaTime * _runSpeed * finalMulti));
        if (Input.GetKey("a")) transform.Translate(Vector3.left * (Time.deltaTime * _runSpeed * finalMulti));
        if (Input.GetKey("d")) transform.Translate(Vector3.right * (Time.deltaTime * _runSpeed * finalMulti));
        
        Rotate();

        PerformRotation();
    }

    private void Interact()
    {
        var hitColliders = Physics.OverlapSphere(transform.position, 3).ToList();
        foreach (var hitCollider in hitColliders)
        {
            var usableObject = hitCollider.GetComponent<UsableObject>();
            if (usableObject)
            {
                var returnedObj = usableObject.TryHelp(_carryingObject);
                if (returnedObj)
                {
                    if (returnedObj.UsableAction)
                    {
                        returnedObj.UsableAction.Use();
                    }
                    
                    if (_carryingObject.UsableAction)
                    {
                        _carryingObject.UsableAction.Use();
                    }
                    PickUp(returnedObj);
                }
                return;
            }
        }
    }

    private void Rotate()
    {
        float yRot = Input.GetAxisRaw("Mouse X");
        _rotation = new Vector3(0f, yRot, 0f) * _lookSensitivity;
        float xRot = Input.GetAxisRaw("Mouse Y");
        _cameraRotationX = xRot * _lookSensitivity;
    }

    public void PerformRotation()
    {
        _rb.MoveRotation(_rb.rotation * Quaternion.Euler(_rotation));
        _currentCameraRotationX -= _cameraRotationX;
        //Set and Clamp Camera Rotation
        _currentCameraRotationX = Mathf.Clamp(_currentCameraRotationX, -CameraRotationLimit, CameraRotationLimit);
        Camera.main.transform.localEulerAngles = new Vector3(_currentCameraRotationX, 0f, 0f);
    }


    private void ShootRay()
    {
        if (!CanCarry)
        {
            if (_carryingObject != _hands)
            {
                Drop();
                return;
            }
        }

        if (Physics.Raycast(transform.position, transform.forward, out var target, pickUpRange))
        {
            var treeFarm = target.transform.GetComponent<TreeFarm>();
            if (treeFarm)
            {
                var pickupable = treeFarm.GiveSapling();
                pickupable.CanBeGrabbedByPlayer = true;
                PickUp(pickupable);
                return;
            }

            var grabable = target.transform.GetComponent<Grabable>();
            if (grabable != null && grabable.CanBeGrabbedByPlayer)
            {
                PickUp(grabable);
                return;
            }

            var usableObject = target.transform.GetComponent<UsableObject>();

            if (usableObject)
            {
                Use(usableObject);
                return;
            }
        }
    }

    private void Use(UsableObject usableObject)
    {
        var returnObject = usableObject.TryHelp(_carryingObject);
        if (returnObject)
        {
            Debug.Log($"{_carryingObject.Type} tries to help {usableObject.Type}");
            PickUp(returnObject);
            _carryingObject.UsableAction.Help();
        }
    }

    public void PickUp(Grabable grabable)
    {
        _carryingObject = grabable;
        //Fuck off gravity
        grabable.GetComponent<Rigidbody>().isKinematic = true;
        grabable.GetComponent<BoxCollider>().enabled = false;
        grabable.gameObject.transform.SetParent(_grabableHook, false);
        grabable.gameObject.transform.position = _grabableHook.position;
        EventManager.Instance.GrabableSpawned(_carryingObject);
    }

    public Grabable Drop()
    {
        //Make sure gravity applies to this object
        _carryingObject.GetComponent<Rigidbody>().isKinematic = false;
        _carryingObject.GetComponent<BoxCollider>().enabled = true;
        //unparent object
        _carryingObject.transform.SetParent(null);
        var oldObject = _carryingObject;
        if (_carryingObject.UsableAction is KoalaAction k)
        {
            k.Sit();
        }
        if (_carryingObject.Type == InteractableEnum.EmptyWaterBucket || _carryingObject.Type == InteractableEnum.FullWaterBucket 
                                                                      || _carryingObject.Type == InteractableEnum.Sapling)
        {
            _carryingObject.gameObject.transform.rotation = new Quaternion(0, 0, 0, 0);
        }
        EventManager.Instance.GrabableSpawned(oldObject);
        _carryingObject = _hands;
        return oldObject;
    }

}