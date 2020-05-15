using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowDemoPart1 : MonoBehaviour {

    public Vector3 EndPos;
    public float Duration;

    private Vector3 startPos;
    private bool sunMoved;
    private Coroutine coroutine;

    private void Update() {
        if (Input.GetKey(KeyCode.Space)) {
            if (!sunMoved) {
                if (coroutine == null) {
                    coroutine = StartCoroutine(MoveSun());
                }
            }
            else {
                sunMoved = false;
                coroutine = null;
                transform.position = startPos;
            }
        }
    }

    IEnumerator MoveSun() {
        startPos = transform.position;
        for (float elapsedTime = 0f; elapsedTime < Duration; elapsedTime += Time.deltaTime) {
            transform.position = Vector3.Lerp(startPos, EndPos, elapsedTime / Duration);
            yield return null;
        }
        sunMoved = true;
    }
}
