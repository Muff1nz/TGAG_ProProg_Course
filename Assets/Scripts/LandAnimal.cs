﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class LandAnimal : MonoBehaviour {
    AnimalSkeleton skeleton;
    private float ikSpeed = 10;
    private float ikTolerance = 0.2f;

    Vector3 heading = Vector3.forward;
    public float turnSpeed = 50f;
    public float speed = 5f;
    public float levelSpeed = 30f;
    public float groundOffsetFactor = 0.7f;

    // Use this for initialization
    void Start() {
        skeleton = new AnimalSkeleton(transform);
        GetComponent<SkinnedMeshRenderer>().sharedMesh = skeleton.createMesh();
        GetComponent<SkinnedMeshRenderer>().rootBone = transform;
        GetComponent<SkinnedMeshRenderer>().bones = skeleton.getBones(AnimalSkeleton.BodyPart.ALL).ToArray();

        //RaycastHit hit;
        //Physics.Raycast(new Ray(transform.position, Vector3.down), out hit);
        //transform.position = hit.point + Vector3.up * skeleton.legLength;
    }

    // Update is called once per frame
    void Update() {
        move();
        levelSpine();
        stayGrounded();
    }

    private void levelSpine() {
        Transform spine = skeleton.getBones(AnimalSkeleton.BodyPart.SPINE)[0];
        RaycastHit hit1;
        RaycastHit hit2;
        Physics.Raycast(new Ray(spine.position + spine.forward * skeleton.spineLength / 2f, Vector3.down), out hit1);
        Physics.Raycast(new Ray(spine.position - spine.forward * skeleton.spineLength / 2f, Vector3.down), out hit2);
        Vector3 a = hit1.point - hit2.point;
        Vector3 b = spine.forward * skeleton.spineLength;

        float angle = Mathf.Acos(Vector3.Dot(a, b) / (a.magnitude * b.magnitude));
        Debug.Log(angle);
        Vector3 normal = Vector3.Cross(a, b);
        if (angle > 0.01f) {
            spine.rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg * levelSpeed * Time.deltaTime, -normal) * spine.rotation;
        }
    }

    private void move() {
        if (Random.Range(0f, 1f) < 0.5f) {
            heading = Quaternion.AngleAxis(turnSpeed * Random.Range(-1f, 1f) * Time.deltaTime, Vector3.up) * heading;
            transform.LookAt(transform.position - heading);
        }
        transform.position += heading * speed * Time.deltaTime;

        RaycastHit hit;
        Physics.Raycast(new Ray(transform.position, Vector3.down), out hit);
        transform.position = hit.point + Vector3.up * (skeleton.legLength + skeleton.neckLength) * groundOffsetFactor;
    }

    private void stayGrounded() {
        List<Transform> rightLegs = skeleton.getBones(AnimalSkeleton.BodyPart.RIGHT_LEGS);
        List<Transform> leftLegs = skeleton.getBones(AnimalSkeleton.BodyPart.LEFT_LEGS);

        var right1 = rightLegs.GetRange(0, 3);
        var right2 = rightLegs.GetRange(3, 3);
        var left1 = leftLegs.GetRange(0, 3);
        var left2 = leftLegs.GetRange(3, 3);

        groundLeg(right1, -1);
        groundLeg(right2, -1);
        groundLeg(left1, 1);
        groundLeg(left2, 1);
    }

    private void groundLeg(List<Transform> leg, int sign) {
        RaycastHit hit;
        Physics.Raycast(new Ray(leg[0].position + sign * leg[0].right * skeleton.legLength / 2f, Vector3.down), out hit);
        ccd(leg, hit.point);
    }

    /// <summary>
    /// Does one iteration of CCD
    /// </summary>
    /// <param name="limb">Limb to bend</param>
    /// <param name="target">Target to reach</param>
    /// <returns>Bool target reached</returns>
    private bool ccd(List<Transform> limb, Vector3 target) {
        Transform[] arm = skeleton.getBones(AnimalSkeleton.BodyPart.RIGHT_LEGS).GetRange(0, 3).ToArray();
        Transform effector = limb[limb.Count - 1];
        float dist = Vector3.Distance(effector.position, target);

            if (dist > ikTolerance) {
                for (int i = 0; i < limb.Count - 1; i++) {
                    Transform bone = limb[i];

                    Vector3 a = effector.position - bone.position;
                    Vector3 b = target - bone.position;


                    float angle = Mathf.Acos(Vector3.Dot(a, b) / (a.magnitude * b.magnitude));
                    Vector3 normal = Vector3.Cross(a, b);
                    if (angle > 0.01f) {
                        bone.rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg * ikSpeed * Time.deltaTime, normal) * bone.rotation;
                    }
                }
            }
        
        return dist < ikTolerance;
    }
}
