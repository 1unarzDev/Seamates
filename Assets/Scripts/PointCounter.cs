using System.Collections;
using UnityEngine;

public class PointCounter : MonoBehaviour {
    [SerializeField] PointHUD pointHUD;

    private void Start () {
        StartCoroutine (CountPoints ());
    }

    private IEnumerator CountPoints () {
        while (true) {
            pointHUD.Points += 1;

            yield return new WaitForSeconds (1);
        }
    }
}