using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using UnityEngine.Networking.Types;
using System;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class HummingbirdAgent : Agent
{
    [Tooltip("Force to apply when flying")] 
    public float _MoveForce = 2f;

    [Tooltip("Speed to look down and up")]
    public float _Pitch = 100f;

    [Tooltip("Yaw speed is rotate speed")]
    public float _Yaw = 100f;

    [Tooltip("Position at the tip of the beak")]
    public Transform _BeakTip;

    [Tooltip("Agents camera")]
    public Camera _AgentCamera;

    [Tooltip("Helps to destinguish some behaviour in train mode or game mode")]
    public bool _IsTrainMode;

    new Rigidbody rigidbody;

    FlowerArea flowerArea;

    Flower nearestFlower;

    [Tooltip("When agent rotate himself, it will be look so strange")]
    float smoothPitchChange = 0f;

    [Tooltip("When agent rotate himself, it will be look so strange")]
    float smoothYawChange = 0f;

    const float maxPitchAngle = 80f;

    [Tooltip("max radius from the beak tip to acces nectar")]
    const float beakTipRadius = 0.008f;

    [Tooltip("whether the agent is flying")]
    bool IsFrozen = false;

    /// <summary>
    /// _NectarObtained in this training episode
    /// </summary>
    public float _NectarObtained { get; private set; }


    public override void Initialize()
    {
        rigidbody = GetComponent<Rigidbody>();
        flowerArea = GetComponentInParent<FlowerArea>();

        // play forever, no max step
        if (!_IsTrainMode) MaxStep = 0;
    }

    /// <summary>
    /// Resets Agent
    /// </summary>
    public override void OnEpisodeBegin()
    {
        if (_IsTrainMode)
        {
            flowerArea.ResetFlowers();
        }

        _NectarObtained = 0;

        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        bool inFrontOfFlower = false;
        if (_IsTrainMode)
        {
            inFrontOfFlower = UnityEngine.Random.value > 0.2;
        }

        MoveToSaveRandomPosition(inFrontOfFlower);

        UpdateNearesFlower();
    }

    private void UpdateNearesFlower()
    {
        foreach (var flower in flowerArea._Flowers)
        {
            if (!nearestFlower && flower._HasNectar)
            {
                nearestFlower = flower;
            }
            else if (flower._HasNectar)
            {
                float distanceToFlower = Vector3.Distance(flower.transform.position, _BeakTip.position);
                float distanceToNearestFlower = Vector3.Distance(nearestFlower.transform.position, _BeakTip.position);

                if (!nearestFlower._HasNectar || distanceToFlower < distanceToNearestFlower)
                {
                    nearestFlower = flower;
                }
            }
        }
    }

    /// <summary>
    /// Moves agent to save position (i.e. does not collide with anything)
    /// </summary>
    /// <param name="inFrontOfFlower"></param>
    private void MoveToSaveRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100;
        Vector3 potentialPostion = Vector3.zero;
        Quaternion potentialRotating = new();

        while (!safePositionFound && attemptsRemaining > 0) 
        { 
            attemptsRemaining--;

            if (inFrontOfFlower)
            {
                var randomFlower = flowerArea._Flowers[UnityEngine.Random.Range(0, flowerArea._Flowers.Count)];

                float distanceFromFlower;
                distanceFromFlower = UnityEngine.Random.Range(.1f, .2f);

                potentialPostion = randomFlower.transform.position + randomFlower._FlowerUpVector * distanceFromFlower;

                Vector3 toFlower = randomFlower._FlowerCenterPosition - potentialPostion;
                potentialRotating = Quaternion.LookRotation(toFlower, Vector3.up);
            }
            else
            {
                float height = UnityEngine.Random.Range(1.2f, 2.5f);

                float raduis = UnityEngine.Random.Range(2f, 7f);

                Quaternion direction = Quaternion.Euler(0f, UnityEngine.Random.Range(-180, 180), 0f);

                potentialPostion = flowerArea.transform.position + Vector3.up*height + direction*Vector3.forward*raduis;

                float pitch = UnityEngine.Random.Range(-60f, 60f);
                float yaw   = UnityEngine.Random.Range(-180f, 180f);

                potentialRotating = Quaternion.Euler(pitch, yaw, 0f);
            }

            Collider[] colls = Physics.OverlapSphere(potentialPostion, 0.05f);

            safePositionFound = colls.Length == 0;
        }

        Debug.Assert(safePositionFound, "Couldnt find safe position for spawn agent");

        transform.SetPositionAndRotation(potentialPostion, potentialRotating);
    }

    /// <summary>
    /// [0]: x (1 = right,  -1 = left)
    /// [1]: y (1 = up,     -1 = down)
    /// [2]: z (1 = forward,-1 = backward)
    /// [3]: pitch angle (1 = up,   -1 = down)
    /// [4]: yaw         (1 = right,-1 = left)
    /// </summary>
    /// <param name="actions"></param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (IsFrozen) return;

        Vector3 move = new(actions.ContinuousActions[0], actions.ContinuousActions[1], actions.ContinuousActions[2]);

        rigidbody.AddForce(move * _MoveForce);

        Vector3 rotation = transform.rotation.eulerAngles;

        float pitchChange = actions.ContinuousActions[3];
        float yawChange = actions.ContinuousActions[4];

        smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);

        float pitch = rotation.x + smoothPitchChange * Time.fixedDeltaTime * _Pitch;
        if (pitch > 180f) pitch -= 360;
        pitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);

        float yaw = rotation.y + smoothYawChange * Time.fixedDeltaTime * _Yaw;

        transform.rotation = Quaternion.Euler(pitch, yaw, 0);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (!nearestFlower)
        {
            sensor.AddObservation(new float[10]);
            return;
        }

        sensor.AddObservation(transform.localRotation.normalized); // 4

        Vector3 toFlower = nearestFlower._FlowerCenterPosition - _BeakTip.position;

        sensor.AddObservation(toFlower.normalized); // 3

        sensor.AddObservation(Vector3.Dot(toFlower.normalized, -nearestFlower._FlowerUpVector.normalized)); // 1

        sensor.AddObservation(Vector3.Dot(_BeakTip.forward.normalized, -nearestFlower._FlowerUpVector.normalized)); // 1

        sensor.AddObservation(toFlower.magnitude / FlowerArea._AreaDiameter); // 1

        // 10
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Vector3 forward = Vector3.zero;
        Vector3 left    = Vector3.zero;
        Vector3 up      = Vector3.zero;
        float pitch = 0;
        float yaw   = 0;  

        if      (Input.GetKey(KeyCode.W)) forward = transform.forward;
        else if (Input.GetKey(KeyCode.S)) forward = -transform.forward;

        if      (Input.GetKey(KeyCode.A)) left = -transform.right;
        else if (Input.GetKey(KeyCode.D)) left = transform.right;

        if      (Input.GetKey(KeyCode.Q)) yaw = -1;
        else if (Input.GetKey(KeyCode.E)) yaw = 1;

        if      (Input.GetKey(KeyCode.UpArrow))   pitch = -1;
        else if (Input.GetKey(KeyCode.DownArrow)) pitch = 1;

        Vector3 combined = (forward + left + up).normalized;

        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = combined.x;
        continuousActionsOut[1] = combined.y;
        continuousActionsOut[2] = combined.z;
        continuousActionsOut[3] = pitch;
        continuousActionsOut[4] = yaw;
    }

    public void FreezeAgent()
    {
        Debug.Assert(_IsTrainMode == false, "Freeze/Unfreeze not supported in training");
        IsFrozen = true;
        rigidbody.Sleep();
    }

    public void UnfreezeAgent()
    {
        Debug.Assert(_IsTrainMode == false, "Freeze/Unfreeze not supported in training");
        IsFrozen = false;
        rigidbody.WakeUp();
    }

    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    private void TriggerEnterOrStay(Collider other)
    {
        if (other.CompareTag("nectar"))
        {
            Vector3 closestPointToBeakTip = other.ClosestPoint(_BeakTip.position);

            if (Vector3.Distance(_BeakTip.position, closestPointToBeakTip) < beakTipRadius)
            {
                var flower = flowerArea.GetFlowerFromNectar(other);

                float nectarReceived = flower.Feed(.01f);

                _NectarObtained += nectarReceived;

                if (_IsTrainMode)
                {
                    float bonus = 0.02f * Mathf.Clamp01(Vector3.Dot(transform.forward.normalized, -nearestFlower._FlowerUpVector.normalized));

                    AddReward(.01f + bonus);
                }

                if (!flower._HasNectar)
                {
                    UpdateNearesFlower();
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_IsTrainMode && collision.collider.CompareTag("boundary"))
        {
            AddReward(-.5f);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (_IsTrainMode && collision.collider.CompareTag("petal_collider"))
        {
            //AddReward(-.0000001f * Time.deltaTime);
        }
    }

    private void Update()
    {
        if (nearestFlower)
            Debug.DrawLine(_BeakTip.position, nearestFlower._FlowerCenterPosition, Color.green);

    }

    private void FixedUpdate()
    {
        if (nearestFlower && !nearestFlower._HasNectar) 
            UpdateNearesFlower();
    }
}
