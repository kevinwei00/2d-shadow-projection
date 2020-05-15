using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowDemoPart2b : MonoBehaviour {

    public Transform Pivot;
    public Transform DestinationRot;

    public bool Pause { get; private set; }

    private Animation anim;
    private bool flag;
    private Coroutine endCo;
    private Coroutine startCo;

    private void Awake() {
        anim = this.GetComponent<Animation>();
        anim.Play();
    }

    private void Update() {
        if (Input.GetKey(KeyCode.Space)) {
            if (!flag) {
                if (endCo == null) {
                    endCo = StartCoroutine(GoToEnd(DestinationRot, 2f));
                }
            }
            else {
                if (startCo == null) {
                    startCo = StartCoroutine(GoToStart(2f));
                }
            }
        }
    }

    IEnumerator GoToEnd(Transform end, float lerpTime) {
        anim["ShadowDemoPart2"].speed = 0f;
        Pause = true;

        Quaternion startRot = Pivot.rotation;
        for (float elapsedTime = 0f; elapsedTime < lerpTime; elapsedTime += Time.deltaTime) {
            Pivot.rotation = Quaternion.Slerp(startRot, end.rotation, elapsedTime / lerpTime);
            yield return null;
        }
        Pivot.rotation = end.rotation;

        flag = true;
        endCo = null;
    }

    IEnumerator GoToStart(float lerpTime) {
        Quaternion startRot = Pivot.rotation;
        for (float elapsedTime = 0f; elapsedTime < lerpTime; elapsedTime += Time.deltaTime) {
            Pivot.rotation = Quaternion.Slerp(startRot, Quaternion.identity, elapsedTime / lerpTime);
            yield return null;
        }
        Pivot.rotation = Quaternion.identity;

        flag = false;
        startCo = null;

        anim["ShadowDemoPart2"].speed = 1f;
        Pause = false;
    }
}
