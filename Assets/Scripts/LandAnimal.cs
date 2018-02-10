﻿using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class LandAnimal : MonoBehaviour {
    AnimalSkeleton skeleton;
    private float ikSpeed = 10;
    private float ikTolerance = 0.1f;

    public const float roamDistance = 20;
    private Vector3 roamCenter;

    Vector3 heading = Vector3.zero;
    public float turnSpeed = 50f;
    public float speed = 5f;
    public float levelSpeed = 30f;
    public float groundOffsetFactor = 0.7f;

    float timer = 0;
    private const float walkSpeed = 0.2f;

    //private void Start() {
    //    Spawn(Vector3.up * 100);
    //    RaycastHit hit;
    //    if (Physics.Raycast(new Ray(transform.position, Vector3.down), out hit)) {
    //        transform.position = hit.point + Vector3.up * (skeleton.legLength) * groundOffsetFactor;
    //    } 
    //}

    // Update is called once per frame
    void Update() {
        if (skeleton != null) {
            move();
            levelSpine();
            //stayGrounded();
            walk();
            timer += Time.deltaTime;
        }
    }

    public void Spawn(Vector3 pos) {
        generate();
        transform.position = pos;
        roamCenter = pos;
        roamCenter.y = 0;

        heading = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        transform.LookAt(transform.position - heading);
    }

    private void generate() {
        foreach(Transform child in transform) {
            Destroy(child.gameObject);
        }
        transform.rotation = Quaternion.identity;
        skeleton = new AnimalSkeleton(transform);
        GetComponent<SkinnedMeshRenderer>().sharedMesh = skeleton.createMesh();
        GetComponent<SkinnedMeshRenderer>().rootBone = transform;

        List<Bone> skeletonBones = skeleton.getBones(BodyPart.ALL);
        Transform[] bones = new Transform[skeletonBones.Count];
        for (int i = 0; i < bones.Length; i++) {
            bones[i] = skeletonBones[i].bone;
        }
        GetComponent<SkinnedMeshRenderer>().bones = bones;
    }

    private void levelSpine() {
        Transform spine = skeleton.getBones(BodyPart.SPINE)[0].bone;
        RaycastHit hit1;
        RaycastHit hit2;
        Physics.Raycast(new Ray(spine.position + spine.forward * skeleton.spineLength / 2f, Vector3.down), out hit1);
        Physics.Raycast(new Ray(spine.position - spine.forward * skeleton.spineLength / 2f, Vector3.down), out hit2);
        Vector3 a = hit1.point - hit2.point;
        Vector3 b = spine.forward * skeleton.spineLength;

        float angle = Mathf.Acos(Vector3.Dot(a, b) / (a.magnitude * b.magnitude));
        Vector3 normal = Vector3.Cross(a, b);
        if (angle > 0.01f) {
            spine.rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg * levelSpeed * Time.deltaTime, -normal) * spine.rotation;
        }
    }

    private void move() {
        float dist = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), roamCenter);
        Vector3 toCenter = roamCenter - transform.position;
        toCenter.y = 0;
        if (dist > roamDistance && Vector3.Angle(toCenter, heading) > 90) {
            heading = -heading;
            heading = Quaternion.AngleAxis(80 * Random.Range(-1f, 1f), Vector3.up) * heading;
            transform.LookAt(transform.position - heading);
        }
        transform.position += heading * speed * Time.deltaTime;
        Debug.DrawLine(transform.position, transform.position + heading * 10, Color.blue);

        RaycastHit hit;
        if (Physics.Raycast(new Ray(transform.position, Vector3.down), out hit)) {
            transform.position = hit.point + Vector3.up * (skeleton.legLength) * groundOffsetFactor;
        } else {
            transform.position = new Vector3(0, -1000, 0);
        }        
    }

    private void stayGrounded() {
        List<Bone> rightLegs = skeleton.getBones(BodyPart.RIGHT_LEGS);
        List<Bone> leftLegs = skeleton.getBones(BodyPart.LEFT_LEGS);
        
        var right1 = rightLegs.GetRange(0, 3);
        var right2 = rightLegs.GetRange(3, 3);
        var left1 = leftLegs.GetRange(0, 3);
        var left2 = leftLegs.GetRange(3, 3);

        groundLeg(right1, -1);
        groundLeg(right2, -1);
        groundLeg(left1, 1);
        groundLeg(left2, 1);
    }

    private void walk() {
        List<Bone> rightLegs = skeleton.getBones(BodyPart.RIGHT_LEGS);
        List<Bone> leftLegs = skeleton.getBones(BodyPart.LEFT_LEGS);

        var right1 = rightLegs.GetRange(0, 3);
        var right2 = rightLegs.GetRange(3, 3);
        var left1 = leftLegs.GetRange(0, 3);
        var left2 = leftLegs.GetRange(3, 3);

        walkLeg(right1, -1, 0);
        walkLeg(right2, -1, Mathf.PI);
        walkLeg(left1, 1, Mathf.PI);
        walkLeg(left2, 1, 0);
    }

    private void groundLeg(List<Bone> leg, int sign) {
        Vector3 target = leg[0].bone.position + sign * transform.right * skeleton.legLength / 2f;

        RaycastHit hit;
        if (Physics.Raycast(new Ray(target, Vector3.down), out hit)) {
            ccd(leg, hit.point);
        }
    }

    private void walkLeg(List<Bone> leg, int sign, float radOffset) {
        Vector3 target = leg[0].bone.position + sign * transform.right * skeleton.legLength / 2f;
        target += heading * Mathf.Cos(timer + radOffset) * skeleton.legLength / 2f; 
        

        RaycastHit hit;
        if (Physics.Raycast(new Ray(target, Vector3.down), out hit)) {
            float heightOffset = (Mathf.Sin(timer + Mathf.PI + radOffset)) * skeleton.legLength / 2f;
            heightOffset = (heightOffset > 0) ? heightOffset : 0;

            target = hit.point;
            target.y += heightOffset;
            ccd(leg, target);
        }

    }

    /// <summary>
    /// Does one iteration of CCD
    /// </summary>
    /// <param name="limb">Limb to bend</param>
    /// <param name="target">Target to reach</param>
    /// <returns>Bool target reached</returns>
    private bool ccd(List<Bone> limb, Vector3 target) {
        Debug.DrawLine(target, target + Vector3.up * 10, Color.red);
        Bone[] arm = skeleton.getBones(BodyPart.RIGHT_LEGS).GetRange(0, 3).ToArray();
        Transform effector = limb[limb.Count - 1].bone;
        float dist = Vector3.Distance(effector.position, target);

        if (dist > ikTolerance) {
            for (int i = limb.Count - 1; i >= 0; i--) {
                Transform bone = limb[i].bone;

                Vector3 a = effector.position - bone.position;
                Vector3 b = target - bone.position;


                float angle = Mathf.Acos(Vector3.Dot(a, b) / (a.magnitude * b.magnitude));
                Vector3 normal = Vector3.Cross(a, b);
                if (angle > 0.01f) {
                    bone.rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg * ikSpeed * Time.deltaTime, normal) * bone.rotation;
                    if (!checkConstraints(limb[i])) {
                        bone.rotation = Quaternion.AngleAxis(-angle * Mathf.Rad2Deg * ikSpeed * Time.deltaTime, normal) * bone.rotation;
                    }
                }
            }
        } 
        
        return dist < ikTolerance;
    }

    private bool checkConstraints(Bone bone) {
        Vector3 rotation = bone.bone.localEulerAngles;
        rotation.x = (rotation.x > 180) ? rotation.x - 360 : rotation.x;
        rotation.y = (rotation.y > 180) ? rotation.y - 360 : rotation.y;
        rotation.z = (rotation.z > 180) ? rotation.z - 360 : rotation.z;

        //Debug.Log("Rot: " + rotation + "__Min: " + bone.minAngles + "__Max: " + bone.maxAngles);
        bool min = rotation.x > bone.minAngles.x && rotation.y > bone.minAngles.y && rotation.z > bone.minAngles.z;
        bool max = rotation.x < bone.maxAngles.x && rotation.y < bone.maxAngles.y && rotation.z < bone.maxAngles.z;
        return min && max;
    }
}
