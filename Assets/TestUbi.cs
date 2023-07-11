using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestUbi : MonoBehaviour
{
    // Start is called before the first frame update
    void Start() {
        StartCoroutine(GetEnumerator());
    }

    IEnumerator GetEnumerator() {
        while (true) {
            Debug.Log(1);
            yield return 1;
            Debug.Log(2);
            yield return 2;
            Debug.Log(3);
            yield return 3;
            break;
        }
    }
}
