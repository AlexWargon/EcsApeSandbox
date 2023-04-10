using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AsyncTest : MonoBehaviour {

    // Start is called before the first frame update
    void Start() {
        Move();
    }

    async Task Move() {
        var dt = Time.deltaTime;
        await Task.Delay((int)(dt * 1000));
        transform.position += Vector3.right * dt * 20f;
        await Move();
    }
}
