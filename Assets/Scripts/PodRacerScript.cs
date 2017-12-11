﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PodRacerScript : MonoBehaviour {
    public GameObject hoverPoint;
    public LayerMask hoverMask;                                     // so we don't Raycast ourself or other pods
    public Text speedIndicator;
    public GameObject mesh;
    public CameraScript podCamera;

    #region Acceleration settings

    public AnimationCurve accelCurve;
    public float timeToFullAcceleration = 0.1f;
    private float accelFactor = 0f;

    #endregion

    #region Speed settings

    public AnimationCurve speedCurve;
    public float timeToFullspeed = 30.0f;
    private float speedFactor = 0f;
    private float speed = 0f;
    private Dictionary<int, float> speedCurveApproximation;         // key = speed in m/s , value = factor from 0f to 1f;
    private float speedCurveApproximationPrecision = 0.0000001f;    // lower =  more accurate speeds but slower loading times
    
    private Vector3 forwardVector;

    #endregion

    #region Brake settings
    
    public AnimationCurve brakeCurve;
    private float brakeFactor = 0f;

    #endregion

    #region Turn settings

    [Range(0, 1)]
    

    private float maxTurnSpeed = 4.0f;
    private float turnSpeedFactor = 2.0f;
    private float turnOppositeMultiplier = 6.0f;                    // can be used for smoothing left/right transition
    private float currentTurnSpeed = 0f;

    private float visualRotSpeed = 0.02f;
    private float maxRotAngle = 50f;
    private float currentRotAngle = 0f;
    

    #endregion

    #region Hover settings

    // hover settings
    public float hoverHeight = 0.5f;
    private Vector3 gravityVector = Vector3.zero;

    #endregion

    #region Controls

    private bool forward;
    private bool brake;
    private bool left;
    private bool right;

    #endregion

    private Rigidbody rb;

    /*
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("percuté : "+ collision.collider.gameObject.name);
    }
    */

    private void Start () {
        #region Optimizations

        rb = GetComponent<Rigidbody>();

        #endregion

        #region Speed Approximation

        speedCurveApproximation = new Dictionary<int, float>();

        int speed = 0;
        float lastFactor = 0f;
        while(speed <= (speedCurve.Evaluate(1f)*10))
        {
            float factor = GetFactorForSpeed(speed, ref lastFactor);
            lastFactor = factor;
            speedCurveApproximation.Add(speed, factor);
            speed += 1;
            //Debug.Log("<color=green>added "+ speed +" = "+ factor +"</color>");
        }
        //Debug.Log("speed 238 m/s (or 23.8 unity units/s) is achieved at ratio " + GetRatioForSpeed(238));

        #endregion
    }


    private void Update () {
        #region Input Keys

        forward = Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.UpArrow);
        brake   = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        left    = Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftArrow);
        right   = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        
        #endregion

        #region Acceleration + Speed control

        if (forward)
        {
            accelFactor += Time.deltaTime / timeToFullAcceleration;                             // timeToFullAcceleration to reach max acceleration
            accelFactor = Mathf.Clamp01(accelFactor);
            speedFactor += accelCurve.Evaluate(accelFactor) * Time.deltaTime / timeToFullspeed; // timeToFullspeed to reach max speed (at max acceleration)
            speedFactor = Mathf.Clamp01(speedFactor);
            speed = speedCurve.Evaluate(speedFactor);
        }
        if (brake)
        {
            accelFactor -= Time.deltaTime / timeToFullAcceleration;
            accelFactor = Mathf.Clamp01(accelFactor);
            speed *= 0.96f;                                                                     // this will do for now.
            speedFactor = GetRatioForSpeed((int)(speed * 10f));                                 // get the approximated speed factor
            speedFactor = Mathf.Clamp01(speedFactor);
        }
        if (!forward && !brake)
        {
            accelFactor -= Time.deltaTime / timeToFullAcceleration;                             // timeToFullAcceleration to loose acceleration. could be another number
            accelFactor = Mathf.Clamp01(accelFactor);
            speed *= 0.999f;                                                                    // this will do for now.
            speedFactor = GetRatioForSpeed((int)(speed * 10f));                                 // get the approximated speed factor
            speedFactor = Mathf.Clamp01(speedFactor);
        }
        #endregion

        #region Turn control
        
        // real turning effects

        if (left)
        {
            currentTurnSpeed -= (currentTurnSpeed > 0) ? Time.deltaTime * turnSpeedFactor * turnOppositeMultiplier : Time.deltaTime * turnSpeedFactor;
            currentTurnSpeed = Mathf.Clamp(currentTurnSpeed, -maxTurnSpeed, maxTurnSpeed);
        }
        if (right)
        {
            currentTurnSpeed += (currentTurnSpeed < 0) ? Time.deltaTime * turnSpeedFactor * turnOppositeMultiplier : Time.deltaTime * turnSpeedFactor;
            currentTurnSpeed = Mathf.Clamp(currentTurnSpeed, -maxTurnSpeed, maxTurnSpeed);
        }
        if (!left && !right)
        {
            currentTurnSpeed *= 1f-Time.deltaTime*2f;   // we gradually reduce turning speed;
        }

        // visual turning effects
        if (left)
        {
            currentRotAngle = Mathf.Lerp(currentRotAngle, maxRotAngle, visualRotSpeed);
            mesh.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, currentRotAngle));
        }
        if (right)
        {
            currentRotAngle = Mathf.Lerp(currentRotAngle, -maxRotAngle, visualRotSpeed);
            mesh.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, currentRotAngle));
        }
        if (!left && !right)
        {
            currentRotAngle = Mathf.Lerp(currentRotAngle, 0, visualRotSpeed);
            mesh.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, currentRotAngle));
        }

        // camera turning effects
        if (left)
        {
            podCamera.aimPoint.transform.localPosition = Vector3.Slerp(podCamera.aimPoint.transform.localPosition, new Vector3(-2f, 0, 10f), 0.02f);
        }
        if (right)
        {
            podCamera.aimPoint.transform.localPosition = Vector3.Slerp(podCamera.aimPoint.transform.localPosition, new Vector3(2f, 0, 10f), 0.02f);
        }
        if (!left && !right)
        {
            podCamera.aimPoint.transform.localPosition = Vector3.Slerp(podCamera.aimPoint.transform.localPosition, new Vector3(0, 0, 10f), 0.02f);
        }

        #endregion

        }
    
    private void FixedUpdate()
    {
        

        #region Terrain detection

        Vector3 terrainVector = Vector3.zero;
        
        RaycastHit hit;

        if (Physics.Raycast(hoverPoint.transform.position, -transform.up, out hit, hoverHeight, hoverMask.value)) // if we are close enough to the ground, apply anti-gravity effect
        {
            gravityVector = Vector3.zero;
            
            //Debug.Log("1f-(hit.distance/hoverHeight) = 1f - (" + hit.distance + " / " + hoverHeight + ")  + = " + ( 1f - (hit.distance / hoverHeight)));
            gravityVector += new Vector3(0f, 15f*(1f-(hit.distance/hoverHeight)), 0f); // that way the pod is floating over the ground
            
            RaycastHit hitFromNormal;
            if (Physics.Raycast(hit.point + hit.normal, Vector3.down, out hitFromNormal, Mathf.Infinity, hoverMask.value))
            {
                terrainVector = hitFromNormal.point - hit.point;
            }

            //Debug.Log("<color=green>touched "+hit.collider.gameObject.name +"</color>");
        }
        else // if we are not close to the ground, make that part of the pod fall
        {
            gravityVector += Physics.gravity * Time.fixedDeltaTime;
            //Debug.Log("<color=red>didnt touch anything</color>");
            //rb.rotation = Quaternion.Lerp(rb.rotation, rb.rotation + Quaternion.to, 0.5f);
        }

        #endregion

        #region Applying forward speed

        forwardVector = transform.forward * speed;

        #endregion

        //Debug.Log(forwardVector + " , " + gravityVector + " , " + terrainVector);
        rb.velocity = forwardVector + gravityVector + terrainVector;
        //rb.MovePosition(transform.position + forwardVector + gravityVector + terrainVector);
        
        #region Turning

        rb.angularVelocity += new Vector3(0f, currentTurnSpeed, 0f);

        #endregion
    }
    
    // Using this to update UI
    private void LateUpdate()
    {
        Vector3 speedS = rb.velocity;
        speedS.y = 0;
        speedIndicator.text = (int)(speedS.magnitude * 10f * 3.6f) + " km/h";
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(hoverPoint.transform.position, -transform.up * hoverHeight);
        
        RaycastHit hit;
        if (Physics.Raycast(hoverPoint.transform.position, -transform.up, out hit, hoverHeight, hoverMask.value))                // if we are close enough to the ground, draw the impact
        {
            Gizmos.DrawSphere(hit.point, 0.02f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(hit.point, hit.normal);
            Gizmos.DrawSphere(hit.point + hit.normal, 0.02f);
            
            RaycastHit hitFromNormal;
            if (Physics.Raycast(hit.point + hit.normal, Vector3.down, out hitFromNormal, Mathf.Infinity, hoverMask.value))
            {
                Gizmos.color = Color.blue;

                Gizmos.DrawSphere(hitFromNormal.point, 0.02f);
                Gizmos.DrawRay(hit.point + hit.normal, Vector3.down*hitFromNormal.distance);


                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(hit.point, hitFromNormal.point-hit.point);
            }
            
        }
    }

    #region Speed Approximation methods

    private float GetFactorForSpeed(int speed, ref float factor)
    {
        float speedFromCurve = speedCurve.Evaluate(factor) * 10;
        while (speed > speedFromCurve)
        {
            factor += speedCurveApproximationPrecision;
            speedFromCurve = speedCurve.Evaluate(factor) * 10;
        }
        return factor;
    }

    private float GetRatioForSpeed(int speed)
    {
        while (!speedCurveApproximation.ContainsKey(speed))
        {
            speed -= 1;
        }
        //Debug.Log("returning the closest lower speed ratio : " + speed);
        return speedCurveApproximation[speed];
    }

    #endregion
}
