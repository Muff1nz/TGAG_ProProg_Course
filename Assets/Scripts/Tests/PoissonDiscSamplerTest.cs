﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


class PoissonDiscSamplerTest : MonoBehaviour {
    public GameObject testObject = null;
    PoissonDiscSampler sampler;


    private void Start() {
        sampler = new PoissonDiscSampler(5, 1000, 1000, true);
        StartCoroutine(run());
    }

    public IEnumerator run() {
        yield return new WaitForSeconds(1);
        int count = 0;
        StopWatch sw = new StopWatch();
        sw.start();

        foreach (Vector2 sample in sampler.sample()) {
            GameObject sampleObject = Instantiate(testObject);
            sampleObject.transform.position = new Vector3(sample.x, 0, sample.y);
            count++;
            //yield return new WaitForSeconds(0.001f);
        }

        sw.done("w=100, h=100, r=5; result:" + count + " spheres.");
    }


    /*
     * Results from testing:
     * w=1000, h=1000, r=5: 27663 points in 3600ms
     * 
     * 
     */

}
